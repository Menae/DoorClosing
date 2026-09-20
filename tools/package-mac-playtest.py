"""Package a completed Unity macOS build with POSIX execute modes and static checks."""
import argparse
import hashlib
import json
import plistlib
import stat
import struct
import zipfile
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("build", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
app = args.build / "Homecoming.app"
report = json.loads((args.build / "build.json").read_text(encoding="utf-8"))
assert report["status"] == "Succeeded" and not report["development"]
plist = plistlib.loads((app / "Contents/Info.plist").read_bytes())
executable = app / "Contents/MacOS" / plist["CFBundleExecutable"]
assert executable.is_file()

def architectures(data):
    if data[:4] == b"\xca\xfe\xba\xbe":
        count = struct.unpack_from(">I", data, 4)[0]
        return [struct.unpack_from(">I", data, 8 + 20 * i)[0] for i in range(count)]
    if data[:4] == b"\xcf\xfa\xed\xfe":
        return [struct.unpack_from("<I", data, 4)[0]]
    return []

binaries = {}
for file in app.rglob("*"):
    if file.is_file():
        with file.open("rb") as stream:
            arch = architectures(stream.read(4096))
        if arch:
            assert set(arch) == {0x01000007, 0x0100000C}, (str(file), arch)
            binaries[file.relative_to(app).as_posix()] = ["x86_64", "arm64"]
assert "Contents/MacOS/" + plist["CFBundleExecutable"] in binaries
assemblies = json.loads((app / "Contents/Resources/Data/ScriptingAssemblies.json").read_text())
names = assemblies["names"]
assert not any("Test" in n or "Editor" in n for n in names)
# The installed package ships a runtime helper assembly (serialization, screenshots,
# object/physics compatibility). Its Editor-only server/transport must be absent.
assert not any("MCP" in n and n != "MCPForUnity.Runtime.dll" for n in names)
assert "Assembly-CSharp.dll" in names
assert not any("PerformanceTest" in f.name for f in app.rglob("*"))
assert (app / "Contents/Resources/Data/level0").is_file()

args.output.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(args.output, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for source in sorted(args.build.rglob("*")):
        rel = source.relative_to(args.build)
        if rel.parts[0] not in {"Homecoming.app", "README.txt", "licenses"}:
            continue
        entry = zipfile.ZipInfo("Homecoming-Mac/" + rel.as_posix() + ("/" if source.is_dir() else ""))
        entry.create_system = 3
        is_binary = source.is_file() and rel.parts[0] == "Homecoming.app" and Path(*rel.parts[1:]).as_posix() in binaries
        mode = (stat.S_IFDIR | 0o755) if source.is_dir() else (stat.S_IFREG | (0o755 if is_binary else 0o644))
        entry.external_attr = mode << 16
        if source.is_dir():
            entry.external_attr |= 0x10
        entry.compress_type = zipfile.ZIP_DEFLATED
        archive.writestr(entry, b"" if source.is_dir() else source.read_bytes())
with zipfile.ZipFile(args.output) as archive:
    assert archive.testzip() is None
    name = "Homecoming-Mac/Homecoming.app/Contents/MacOS/" + plist["CFBundleExecutable"]
    assert archive.getinfo(name).external_attr >> 16 & 0o111 == 0o111
    assert archive.read(name) == executable.read_bytes()
result = {"zip": str(args.output), "bytes": args.output.stat().st_size,
          "sha256": hashlib.sha256(args.output.read_bytes()).hexdigest(),
          "minimum_macos": plist["LSMinimumSystemVersion"], "native_binaries": binaries,
          "assembly_count": len(names), "mcp_runtime_helpers": "MCPForUnity.Runtime.dll" in names,
          "zip_crc": "pass", "posix_executable_modes": "pass",
          "mac_runtime_validation": "pending", "developer_id_notarization": "not performed"}
args.output.with_suffix(".validation.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(result, ensure_ascii=False, indent=2))
