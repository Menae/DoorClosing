"""WORK-007 PreToolUse guard: while Sol holds artifacts/orchestra/.sol.lock, the leader must not change the
checkout or drive Unity (ORCH-001). Denies repo edits outside briefs/ and reviews/, git commands that change
state, and Unity operations. Without the lock it allows everything immediately; with the lock any error denies.
Known gaps (documented in DEVELOPMENT): file writes through shell commands, and git spelled in ways the pattern
does not cover. Standard library only.
"""
import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LOCK = Path(os.environ.get("SOL_GUARD_LOCK") or ROOT / "artifacts" / "orchestra" / ".sol.lock")  # override for tests
LEADER_DIRS = (ROOT / "artifacts" / "orchestra" / "briefs", ROOT / "artifacts" / "orchestra" / "reviews")
VALUE = r"""(?:"[^"]*"|'[^']*'|\S+)"""
GIT_OPTION = (rf"-[cC]\s+{VALUE}|--(?:git-dir|work-tree|namespace|config-env)(?:=|\s+){VALUE}"
              r"|--[\w-]+(?:=\S+)?|-[A-Za-z]\b")
GIT = re.compile(rf"""(?<![\w.-])git(?:\.exe)?["']?((?:\s+(?:{GIT_OPTION}))*)\s+([a-z][a-z-]*)([^;&|\n]*)""", re.I)
GIT_WRITES = {"add", "commit", "push", "pull", "stash", "reset", "restore", "checkout", "switch", "merge", "rebase",
              "cherry-pick", "revert", "apply", "am", "rm", "mv", "clean", "tag", "worktree", "update-index",
              "update-ref", "notes", "submodule", "gc", "prune", "filter-branch", "replace", "bisect", "read-tree",
              "checkout-index", "fetch", "init", "clone", "sparse-checkout", "commit-tree", "write-tree", "symbolic-ref"}
UNITY = re.compile(r"UnityAgent\.ps1|unity-agent[\\/]+client\.py|\bUnity\.exe\b|(?:127\.0\.0\.1|localhost):8080", re.I)
MSYS_DRIVE = re.compile(r"^/([a-zA-Z])/")  # Git Bash spelling of C:\


def deny(reason):
    sys.stdout.write(json.dumps({"hookSpecificOutput": {
        "hookEventName": "PreToolUse", "permissionDecision": "deny",
        "permissionDecisionReason": f"Sol is running ({reason}). Wait for it, check `python tools/orchestra/sol.py status`, "
                                    "or end it with `sol.py stop`; only reading and briefs/reviews are allowed meanwhile."}},
        ensure_ascii=True) + "\n")  # ASCII JSON survives any console code page
    return 0


def inside(path, folder):
    try:
        return Path(os.path.normcase(str(path))).is_relative_to(Path(os.path.normcase(str(folder))))
    except (TypeError, ValueError):
        return False


def git_write(command):
    for match in GIT.finditer(command):
        sub, rest = match.group(2).lower(), match.group(3)
        if sub in GIT_WRITES:
            return f"git {sub}"
        if sub == "branch":
            if re.search(r"(?:^|\s)(?:-[dDmMcCfu]\b|--(?:delete|move|copy|force|set-upstream-to|unset-upstream|edit-description))", rest):
                return "git branch"
            names = [t for t in rest.split() if not t.startswith("-")]
            if names and not re.search(r"--list|(?:^|\s)-l\b|--contains|--merged|--no-merged|--points-at", rest):
                return "git branch"  # a bare name creates a branch
        if sub == "config" and not re.search(r"--get|--list|(?:^|\s)-l\b", rest):
            return "git config"
        if sub == "remote" and re.match(r"\s+(add|remove|rm|rename|set-url|set-head|set-branches|prune|update)\b", rest):
            return "git remote"
        if sub == "lfs" and not re.match(r"\s+(ls-files|status|env|version|logs)\b", rest):
            return "git lfs"
    return None


def check(payload):
    tool = payload.get("tool_name") or ""
    data = payload.get("tool_input") or {}
    if tool in ("Edit", "Write", "NotebookEdit"):
        raw = str(data.get("file_path") or data.get("notebook_path") or "")
        if raw.startswith(("\\\\?\\", "//?/")):
            raw = raw[4:]  # extended-length prefix
        raw = MSYS_DRIVE.sub(lambda m: f"{m.group(1)}:/", raw)
        target = Path(raw if os.path.isabs(raw) else ROOT / raw).resolve()
        if inside(target, ROOT) and not any(inside(target, d) for d in LEADER_DIRS):
            return deny(f"{tool} of {target.relative_to(ROOT).as_posix()}")
        return 0
    if tool in ("Bash", "PowerShell"):
        command = str(data.get("command") or "")
        found = git_write(command)
        if found:
            return deny(found)
        if UNITY.search(command):
            return deny("Unity operation")
        return 0
    if tool.startswith("mcp__UnityMCP__"):
        return deny(tool)
    return 0


def main():
    if not LOCK.exists():
        return 0
    try:
        payload = json.loads(sys.stdin.buffer.read().decode("utf-8", errors="replace") or "{}")
        return check(payload)
    except Exception as error:  # fail closed while Sol runs
        return deny(f"guard error {type(error).__name__}")


if __name__ == "__main__":
    raise SystemExit(main())
