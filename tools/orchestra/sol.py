"""WORK-007: run the Sol implementer (Codex CLI) non-interactively for the Opus leader.

Each run stores the exact prompt, JSONL events, structured report, git/Unity state, usage and a
machine-checked summary.json under artifacts/orchestra/<task>/<UTC stamp>/ so the leader and the
independent reviewer can audit it. `run` uses `codex exec`; `app` queues the brief into the user's
Codex desktop-app Sol thread, where Computer Use approvals work (CU-001). Standard library only.
"""
import argparse
import contextlib
import datetime as dt
import fnmatch
import glob
import hashlib
import json
import os
import re
import shutil
import sqlite3
import subprocess
import sys
import time
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
RUNS = ROOT / "artifacts" / "orchestra"
LOCK = RUNS / ".sol.lock"
LEADER_DIRS = ("briefs", "reviews")  # artifacts/orchestra/* the leader may write while Sol runs
DEFAULT_MODEL = "gpt-6.1-sol"
DEFAULT_EFFORT = "max"
CLI_TIMEOUT_MIN = 90
# Server-side saturation is transient: resume the same session instead of switching models.
CAPACITY_MARKERS = ("at capacity", "overloaded", "rate limit", "try again later")
# An account limit does not clear in minutes: stop and report instead of retrying.
USAGE_LIMIT_MARKERS = ("usage limit", "hit your limit", "quota")
# The user's Codex config enables a question tool; an unattended exec run has nobody to answer it.
NO_USER_INPUT = ["-c", "features.default_mode_request_user_input=false"]
RESUME_AFTER_CAPACITY = (
    "前のターンはモデルの容量不足（サーバー側）で中断した。作業状態とファイルを確認し直し、"
    "同じタスクを続けて完了させ、最後に指定スキーマのJSONだけで報告すること。")
UNITY_PY = ROOT / "tools" / "unity-agent" / ".venv" / "Scripts" / "python.exe"
UNITY_CLIENT = ROOT / "tools" / "unity-agent" / "client.py"
FULL_REVIEW_PREFIXES = ("Assets/", "ProjectSettings/", "Packages/")
CONTRACT_PROTECTED = ("docs/PROJECT_STATUS.md", "docs/DECISIONS.md")  # Sol must not edit these
RULE_PATHS = (".claude/", "tools/orchestra/", "AGENTS.md")  # the hook, contract and agent rules themselves
TEXT_SUFFIXES = (".cs", ".md", ".json", ".txt", ".py", ".ps1", ".shader", ".hlsl", ".cginc", ".asmdef", ".uss", ".uxml")
# codex reports config notices as error items; they are not failures of Sol's work.
BENIGN_NOTICES = ("Codex is ignoring", "Under-development features enabled")


def now_iso():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def codex_bin(name):
    # The desktop app installs its tools under hash folders that change on update.
    pattern = os.path.join(os.environ.get("LOCALAPPDATA", ""), "OpenAI", "Codex", "bin", "*", name)
    candidates = [Path(p) for p in glob.glob(pattern)]
    return max(candidates, key=lambda p: p.stat().st_mtime) if candidates else None


def find_codex():
    override = os.environ.get("CODEX_CLI_PATH")
    if override and Path(override).is_file():
        return Path(override)
    found = codex_bin("codex.exe")
    if not found:
        raise SystemExit("codex.exe not found. Install/update the Codex desktop app or set CODEX_CLI_PATH.")
    return found


def child_env():
    """Sol's shells inherit this env; the app ships rg.exe in a different bin folder than codex.exe."""
    env = os.environ.copy()
    rg = shutil.which("rg")
    if not rg:
        found = codex_bin("rg.exe")
        if found:
            env["PATH"] = str(found.parent) + os.pathsep + env.get("PATH", "")
            rg = str(found)
    return env, rg


def git(*args):
    result = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    return result.stdout.rstrip("\n")  # keep porcelain's leading status column


def status_entries():
    raw = subprocess.run(["git", "status", "--porcelain=v1", "-z", "-uall"], cwd=ROOT,
                         capture_output=True).stdout.decode("utf-8", errors="replace")
    parts, entries, i = raw.split("\0"), [], 0
    while i < len(parts):
        part = parts[i]
        i += 1
        if not part:
            continue
        entries.append((part[:2], part[3:]))
        if part[0] in "RC" or part[1] in "RC":
            if i < len(parts) and parts[i]:
                entries.append((part[:2], parts[i]))  # the rename/copy source changed too
            i += 1
    return entries


def file_hash(rel):
    path = ROOT / rel
    try:
        return hashlib.sha1(path.read_bytes()).hexdigest() if path.is_file() else None
    except OSError:
        return None


def git_state():
    entries = status_entries()
    return {
        "head": git("rev-parse", "HEAD"),
        "status": git("status", "--porcelain=v1", "-uall").splitlines(),
        "staged": git("diff", "--cached", "--name-only").splitlines(),
        # content hashes of already-dirty paths, so the summary can tell this run's edits apart
        "dirty": {path: file_hash(path) for _, path in entries},
    }


def require_clean(allow_dirty, what):
    entries = status_entries()
    if entries and not allow_dirty:
        listing = "\n".join(f"  {xy} {path}" for xy, path in entries[:20])
        raise SystemExit(
            f"The working tree has changes, so {what} could not be attributed to Sol alone:\n{listing}\n"
            "Commit or set them aside first (ask the user about edits that are not yours), or pass --allow-dirty "
            "for a follow-up on this task's own uncommitted changes.")


# ---- Unity Editor state (MCP via the pinned client) ----

def unity_client(*args, timeout=180):
    if not UNITY_PY.is_file():
        raise RuntimeError("tools/unity-agent/.venv is missing; run tools/UnityAgent.ps1 Setup.")
    result = subprocess.run([str(UNITY_PY), str(UNITY_CLIENT), *args], cwd=ROOT, capture_output=True,
                            text=True, encoding="utf-8", errors="replace", timeout=timeout)
    if result.returncode:
        raise RuntimeError((result.stderr or result.stdout).strip()[-400:] or f"client exit {result.returncode}")
    return json.loads(result.stdout)


def mcp_data(result):
    value = result.get("structuredContent")
    if value is None:
        for block in result.get("contents", result.get("content", [])):
            if "text" in block:
                value = json.loads(block["text"])
                break
    if isinstance(value, dict) and "result" in value:
        value = value["result"]
    if isinstance(value, dict) and value.get("success") is False:
        raise RuntimeError(json.dumps(value, ensure_ascii=False)[:400])
    return value.get("data", value) if isinstance(value, dict) else value


def unity_state(launch):
    """Editor facts the next task depends on; `error` is set when Unity could not be read."""
    state = {"checked_utc": now_iso()}
    try:
        if launch:
            started = subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
                                      str(ROOT / "tools" / "UnityAgent.ps1"), "Launch"], cwd=ROOT,
                                     capture_output=True, text=True, errors="replace", timeout=1000)
            state["launch"] = (started.stdout + started.stderr).strip()[-600:]
            if started.returncode:
                raise RuntimeError(f"UnityAgent.ps1 Launch failed: {state['launch'][-300:]}")
        instances = mcp_data(unity_client("resource", "mcpforunity://instances")) or {}
        instance = info = None
        for candidate in instances.get("instances", []):
            if candidate.get("name") != ROOT.name:
                continue
            data = mcp_data(unity_client("--instance", candidate["id"], "resource", "mcpforunity://project/info"))
            if Path(data["projectRoot"]).resolve() == ROOT.resolve():
                instance, info = candidate["id"], data
                break
        if not instance:
            raise RuntimeError("No MCP instance reports this projectRoot.")
        editor = mcp_data(unity_client("--instance", instance, "resource", "mcpforunity://editor/state"))
        scene = mcp_data(unity_client("--instance", instance, "call", "manage_scene",
                                      "--arguments", json.dumps({"action": "get_active"})))
        counts = {}
        for kind in ("error", "warning"):
            entries = mcp_data(unity_client("--instance", instance, "call", "read_console", "--arguments",
                                            json.dumps({"action": "get", "types": [kind], "count": 200,
                                                        "format": "plain"})))
            counts[kind] = len(entries) if isinstance(entries, list) else None
        state.update(
            instance=instance, build_target=info.get("platform"), unity_version=info.get("unityVersion"),
            is_playing=editor["editor"]["play_mode"]["is_playing"],
            is_compiling=editor["compilation"]["is_compiling"],
            ready_for_tools=editor["advice"]["ready_for_tools"], blocking=editor["advice"]["blocking_reasons"],
            state_age_ms=(editor.get("staleness") or {}).get("age_ms"),
            active_scene=scene.get("path"), scene_dirty=scene.get("isDirty"),
            console_errors=counts["error"], console_warnings=counts["warning"])
    except Exception as error:  # any unexpected reply means Unity could not be read
        state["error"] = f"{type(error).__name__}: {error}"[:400]
    return state


def unity_preflight():
    print("[sol] checking Unity (Launch + editor state)...", flush=True)
    state = unity_state(launch=True)
    problems = []
    if state.get("error"):
        problems.append(f"Unity could not be read: {state['error']}")
    else:
        if state["is_playing"]:
            problems.append("the Editor is in Play Mode")
        if state["scene_dirty"]:
            problems.append(f"{state['active_scene']} has unsaved changes (ask the user before saving or discarding)")
        # 'stale_status' only means the Editor has not ticked recently (idle in the background); tool calls
        # such as the scene and Console reads above still answer, so it is noted rather than refused.
        busy = [reason for reason in state["blocking"] if reason != "stale_status"]
        if state["is_compiling"] or busy:
            problems.append(f"the Editor is busy ({busy or 'compiling'}); retry shortly")
        elif not state["ready_for_tools"]:
            print(f"[sol] note: Unity reports {state['blocking']}; its scene and Console reads answered.", flush=True)
    if problems:
        raise SystemExit("Unity is not ready for Sol: " + "; ".join(problems) + ".")
    if state["console_errors"]:
        print(f"[sol] note: the Console already has {state['console_errors']} error(s) before the run.", flush=True)
    return state


# ---- locking ----

UNKNOWN = "unknown"  # the process table could not be read: never treat that as "not running"


def process_info(pid):
    """Name, command line and start time (unix seconds) of a live process; None if it is gone; UNKNOWN on failure."""
    script = ("$ErrorActionPreference='Stop'; [Console]::OutputEncoding=[Text.Encoding]::UTF8; "
              f"$p = Get-CimInstance Win32_Process -Filter 'ProcessId={int(pid)}'; if ($p) {{ "
              "[pscustomobject]@{name=$p.Name; cmd=$p.CommandLine; "
              "created=([DateTimeOffset]$p.CreationDate).ToUnixTimeSeconds()} | ConvertTo-Json -Compress }")
    try:
        result = subprocess.run(["powershell", "-NoProfile", "-Command", script], capture_output=True, text=True,
                                encoding="utf-8", errors="replace", timeout=60)
    except (OSError, subprocess.TimeoutExpired):
        return UNKNOWN
    out = result.stdout.strip()
    if result.returncode != 0 or (not out and result.stderr.strip()):
        return UNKNOWN
    try:
        return json.loads(out) if out else None
    except json.JSONDecodeError:
        return UNKNOWN


def is_run_process(pid, kind, since):
    """True only for the sol.py / codex exec process that wrote the lock (not a later process reusing the PID);
    UNKNOWN when the process table could not be read."""
    info = process_info(pid) if pid else None
    if info == UNKNOWN:
        return UNKNOWN
    if not info:
        return False
    if since and isinstance(info.get("created"), int) and info["created"] > since + 5:
        return False  # started after the lock was last written: the PID was reused
    name, cmd = (info.get("name") or "").lower(), (info.get("cmd") or "").lower()
    if kind == "python":
        return name.startswith("python") and "sol.py" in cmd
    return name.startswith("codex") and " exec" in cmd and "report.schema.json" in cmd


def lock_pids(holder):
    python = re.search(r"(?<![a-z_])pid=(\d+)", holder)
    codex = re.search(r"codex_pid=(\d+)", holder)
    return (int(python.group(1)) if python else None), (int(codex.group(1)) if codex else None)


def lock_mtime():
    try:
        return LOCK.stat().st_mtime
    except OSError:
        return None


def holder_alive(holder):
    python, codex = lock_pids(holder)
    since = lock_mtime()
    states = [is_run_process(codex, "codex", since), is_run_process(python, "python", since)]
    if True in states:
        return True
    return UNKNOWN if UNKNOWN in states else False


def acquire_lock(run_dir):
    RUNS.mkdir(parents=True, exist_ok=True)
    token = f"pid={os.getpid()} run={run_dir}"
    for _ in range(3):
        try:
            handle = os.open(LOCK, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            break
        except FileExistsError:
            try:
                holder = LOCK.read_text(encoding="utf-8").strip()
            except FileNotFoundError:
                continue  # released meanwhile
            if "app_pending" in holder:
                run = re.search(r"run=(.+?) app_pending", holder)
                raise SystemExit(f"A desktop-app Sol turn may still be queued or running ({holder}). Use "
                                 f"`python tools/orchestra/sol.py app-wait --run-dir \"{run.group(1) if run else '<run dir>'}\"`.")
            if not holder and time.time() - (lock_mtime() or 0) < 60:
                raise SystemExit(f"Another Sol run is taking {LOCK} right now; retry in a minute.")
            alive = holder_alive(holder) if holder else False
            if alive == UNKNOWN:
                raise SystemExit(f"{LOCK} is held ({holder}) and the process table could not be read; check "
                                 "those PIDs by hand before removing it.")
            if alive:
                raise SystemExit(f"Another Sol run holds {LOCK}: {holder}. Wait for it, or end it with "
                                 "`python tools/orchestra/sol.py stop`.")
            try:
                if LOCK.read_text(encoding="utf-8").strip() != holder:
                    continue  # someone replaced it while we checked; look again
            except FileNotFoundError:
                continue
            # Neither the sol.py nor the codex process of that run is alive: a crashed or killed run left it.
            print(f"[sol] removing a stale lock (no live sol.py/codex process of that run): {holder}", flush=True)
            LOCK.unlink(missing_ok=True)
    else:
        raise SystemExit(f"Could not take {LOCK}; another run started at the same moment.")
    with os.fdopen(handle, "w", encoding="utf-8") as lock:
        lock.write(token + "\n")
    return token


def note_codex_pid(token, pid):
    LOCK.write_text(f"{token} codex_pid={pid}\n", encoding="utf-8")


def release_lock(token):
    try:
        if LOCK.read_text(encoding="utf-8").startswith(token):
            LOCK.unlink()
    except FileNotFoundError:
        pass


def ignored_writes(since, run_dir):
    """Files under artifacts/ touched during the run; git status cannot see ignored paths."""
    root = ROOT / "artifacts"
    leader = {RUNS / name for name in LEADER_DIRS}
    touched = []
    for current, dirs, files in os.walk(root):
        here = Path(current)
        if here == root:
            # cache/ and server/ are written by uv and the leader's MCP server, not by Sol.
            dirs[:] = [d for d in dirs if d not in ("cache", "server")]
        if here == RUNS:
            dirs[:] = [d for d in dirs if RUNS / d not in leader]
        if here == run_dir or run_dir in here.parents:
            continue
        for name in files:
            path = here / name
            try:
                if path != LOCK and path.stat().st_mtime >= since:
                    touched.append(path.relative_to(ROOT).as_posix())
            except OSError:
                continue  # removed while scanning
    return sorted(touched)[:500]


# ---- run summary (mechanical checks the reviewer reads first) ----

def clip(text, limit):
    text = str(text or "")
    return text if len(text) <= limit else text[:limit] + "…"


def step_failures(log_path, app_route):
    failures = []
    if not log_path.exists():
        return failures
    for line in open(log_path, encoding="utf-8", errors="replace"):
        try:
            record = json.loads(line)
        except json.JSONDecodeError:
            continue
        if app_route:
            kind, item = record.get("item_type"), record.get("item") or {}
        elif record.get("type") == "item.completed":
            item = record.get("item") or {}
            kind = item.get("type")
        else:
            continue
        code = item.get("exitCode", item.get("exit_code"))
        if kind in ("command_execution", "commandExecution") and code not in (0, None):
            command = re.sub(r'^"?[^"]*powershell\.exe"?\s+-Command\s+', "", str(item.get("command") or ""))
            failures.append({"kind": "command", "exit": code, "command": clip(command, 200),
                             "output": clip(item.get("aggregated_output") or item.get("aggregatedOutput"), 300)})
        elif kind in ("mcp_tool_call", "mcpToolCall") and (item.get("error") or item.get("status") == "failed"):
            failures.append({"kind": "mcp", "tool": f"{item.get('server')}.{item.get('tool')}",
                             "error": clip(json.dumps(item.get("error"), ensure_ascii=False), 300)})
        elif kind == "error":
            if not str(item.get("message") or "").startswith(BENIGN_NOTICES):
                failures.append({"kind": "error", "message": clip(item.get("message"), 300)})
        elif item.get("status") == "failed":
            failures.append({"kind": kind, "status": "failed"})
    return failures[:40]


def cli_usage(events):
    turns, total = 0, {}
    if events.exists():
        for line in open(events, encoding="utf-8", errors="replace"):
            try:
                event = json.loads(line)
            except json.JSONDecodeError:
                continue
            if event.get("type") == "turn.completed" and isinstance(event.get("usage"), dict):
                turns += 1
                for key, value in event["usage"].items():
                    if isinstance(value, (int, float)):
                        total[key] = total.get(key, 0) + value
    return {"turns": turns, "total": total}


def normalize(path):
    text = str(path).replace("\\", "/")
    root = ROOT.as_posix().rstrip("/") + "/"
    if text.lower().startswith(root.lower()):
        return text[len(root):]
    while text.startswith("./"):  # a prefix, not a character set: keep .claude/, .gitignore and ../
        text = text[2:]
    return text


def guid_of(text):
    found = re.search(r"^guid:\s*(\w+)", text or "", re.M)
    return found.group(1) if found else None


def base_text(base, rel):
    old = subprocess.run(["git", "show", f"{base}:{rel}"], cwd=ROOT, capture_output=True,
                         text=True, encoding="utf-8", errors="replace")
    return old.stdout if old.returncode == 0 else None


def diff_check(base, touched):
    """`git diff --check` limited to this run's files, plus a whitespace/conflict scan of new text files."""
    raw = subprocess.run(["git", "diff", "--check", base], cwd=ROOT, capture_output=True).stdout
    wanted, keep, found = set(touched), False, []
    for line in raw.decode("utf-8", errors="replace").splitlines():
        located = re.match(r"^(.+?):(\d+): ", line)
        if located:
            keep = located.group(1) in wanted
        if keep:
            found.append(line)
    untracked = {path for xy, path in status_entries() if xy == "??"}  # -z keeps non-ASCII names unquoted
    for rel in touched:
        if rel not in untracked or not rel.lower().endswith(TEXT_SUFFIXES):
            continue  # Unity YAML is written by the Editor; git's own check covers tracked files
        try:
            data = (ROOT / rel).read_bytes()
        except OSError:
            continue
        if len(data) > 1_000_000 or b"\0" in data:
            continue
        for number, text in enumerate(data.decode("utf-8", errors="replace").splitlines(), 1):
            if text != text.rstrip(" \t"):
                found.append(f"{rel}:{number}: trailing whitespace (new file)")
            elif text.startswith(("<<<<<<< ", ">>>>>>> ")) or text == "=======":
                found.append(f"{rel}:{number}: conflict marker (new file)")
    return found[:40]


def meta_issues(touched, base):
    issues, current_guids, removed_guids = set(), set(), {}
    for rel in touched:
        if not rel.startswith("Assets/"):
            continue
        exists = (ROOT / rel).exists()
        if rel.endswith(".meta"):
            asset = rel[:-5]
            if exists and not (ROOT / asset).exists():
                issues.add(f"{rel}: .meta without its asset")
            if not exists and (ROOT / asset).exists():
                issues.add(f"{rel}: .meta removed but the asset remains")
            was = guid_of(base_text(base, rel))
            if exists:
                now = guid_of((ROOT / rel).read_text(encoding="utf-8", errors="replace"))
                current_guids.add(now)
                if was and now and was != now:
                    issues.add(f"{rel}: GUID changed {was} -> {now}")
            elif was:
                removed_guids[was] = rel
        else:
            if exists and not (ROOT / f"{rel}.meta").exists():
                issues.add(f"{rel}: asset without .meta")
            if not exists and (ROOT / f"{rel}.meta").exists():
                issues.add(f"{rel}: asset removed but .meta remains")
        if exists:
            parent = Path(rel).parent
            while parent.as_posix() not in ("Assets", "."):
                if not (ROOT / f"{parent.as_posix()}.meta").exists():
                    issues.add(f"{parent.as_posix()}/: folder without .meta")
                parent = parent.parent
    for guid, rel in removed_guids.items():
        if guid not in current_guids:  # a move that kept the .meta would carry the GUID to the new path
            issues.add(f"{rel}: GUID {guid} no longer exists; if the asset was moved, its references are lost")
    return sorted(issues)


def summarize(run_dir, meta, log_path, app_route, report):
    before, after = meta["git_before"], meta["git_after"]
    current = {path: xy for xy, path in status_entries()}
    touched = {p for p in current if before.get("dirty", {}).get(p, "<clean>") != file_hash(p)}
    touched |= {p for p in before.get("dirty", {}) if p not in current}  # reverted to clean during the run
    touched = sorted(touched)
    scope = meta.get("scope") or []
    out_of_scope = [p for p in touched if scope and not any(fnmatch.fnmatchcase(p, pat) for pat in scope)]
    reported = {normalize(f.get("path", "")) for f in (report or {}).get("changed_files", [])}
    ignored = set(meta.get("ignored_writes", []))
    unreported = sorted(set(touched) - reported)
    phantom = sorted(reported - set(touched) - ignored)
    head_changed = after["head"] != before["head"]
    flags = []
    unity_before, unity_after = meta.get("unity_before") or {}, meta.get("unity_after")
    if unity_after:
        if unity_after.get("error"):
            flags.append(f"Unity unreadable after the run: {unity_after['error']}")
        else:
            if unity_after.get("is_playing"):
                flags.append("Editor left in Play Mode")
            if unity_after.get("scene_dirty"):
                flags.append(f"{unity_after.get('active_scene')} left with unsaved changes")
            if (unity_after.get("console_errors") or 0) > (unity_before.get("console_errors") or 0):
                flags.append(f"Console errors rose to {unity_after.get('console_errors')}")
    issues = meta_issues(touched, before["head"])
    unity_paths = [p for p in touched if p.startswith(FULL_REVIEW_PREFIXES) or p.endswith((".cs", ".asmdef", ".unity", ".prefab"))]
    reasons = []
    for label, bad in (
            ("touches Assets/ProjectSettings/Packages or Unity code/scenes", unity_paths),
            ("edits PROJECT_STATUS/DECISIONS (forbidden by the contract)", [p for p in touched if p in CONTRACT_PROTECTED]),
            ("touches the hook, contract or agent rules", [p for p in touched if p.startswith(RULE_PATHS)]),
            ("head changed (Sol committed?)", head_changed), ("staged files", after["staged"]),
            ("no --scope was declared", touched and not scope), ("out-of-scope files", out_of_scope),
            ("changes differ from the report", unreported or phantom), (".meta/GUID issues", issues),
            ("Unity flags", flags), ("Unity state was not checked (--unity)", unity_paths and not meta.get("unity_before")),
            ("report missing or not completed", (report or {}).get("status") != "completed")):
        if bad:
            reasons.append(label)
    summary = {
        "task": meta["task"], "route": meta.get("route", "cli"), "effort": meta.get("effort"),
        "duration_sec": meta.get("duration_sec"), "usage": meta.get("usage"),
        "thread_tokens_delta": meta.get("thread_tokens_delta"),
        "report_status": meta.get("report_status"),
        "report_unverified": (report or {}).get("unverified"),
        "questions_for_leader": (report or {}).get("questions_for_leader"),
        "touched": touched, "scope": scope, "out_of_scope": out_of_scope,
        "unreported_changes": unreported, "reported_but_unchanged": phantom,
        "head_changed": head_changed, "staged": after["staged"],
        "meta_issues": issues, "diff_check": diff_check(before["head"], touched) if touched else [],
        "ignored_writes": {"count": len(ignored), "sample": sorted(ignored)[:20]},
        "failed_steps": step_failures(log_path, app_route),
        "unity_before": meta.get("unity_before"), "unity_after": unity_after, "unity_flags": flags,
        "suggested_review": "full" if reasons else "light", "review_reasons": reasons,
    }
    (run_dir / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    return summary


def finish(run_dir, meta, log_path, app_route, report, started, unity):
    """Collect the after-state; each failure is recorded instead of hiding the run's own result."""
    meta["duration_sec"] = round(time.time() - started)
    collectors = [("git_after", git_state), ("ignored_writes", lambda: ignored_writes(started, run_dir))]
    if unity:
        collectors.append(("unity_after", lambda: unity_state(launch=False)))
    for key, collect in collectors:
        try:
            meta[key] = collect()
        except Exception as error:
            meta[f"{key}_error"] = repr(error)
    try:
        summary = summarize(run_dir, meta, log_path, app_route, report)
    except Exception as error:
        meta["summary_error"] = repr(error)
        summary = None
    write_meta(run_dir, meta)
    return summary


def brief_line(summary):
    if not summary:
        return {"summary": "failed; see meta.summary_error"}
    return {key: summary[key] for key in ("suggested_review", "review_reasons", "touched", "out_of_scope",
                                          "meta_issues", "unity_flags")} | {"failed_steps": len(summary["failed_steps"])}


# ---- CLI route ----

def find_session_id(events_path):
    keys = ("thread_id", "session_id", "conversation_id")

    def search(value):
        if isinstance(value, dict):
            for key in keys:
                if isinstance(value.get(key), str):
                    return value[key]
            for child in value.values():
                found = search(child)
                if found:
                    return found
        elif isinstance(value, list):
            for child in value:
                found = search(child)
                if found:
                    return found
        return None

    with open(events_path, encoding="utf-8", errors="replace") as handle:
        for line in handle:
            try:
                found = search(json.loads(line))
            except json.JSONDecodeError:
                continue
            if found:
                return found
    return None


def last_failure(events_path, offset):
    """Error of the newest turn written after `offset`, or None (completed, or no turn at all)."""
    message = None
    with open(events_path, "rb") as handle:
        handle.seek(offset)
        lines = handle.read().decode("utf-8", errors="replace").splitlines()
    for line in lines:
        try:
            event = json.loads(line)
        except json.JSONDecodeError:
            continue
        if event.get("type") == "turn.failed":
            message = (event.get("error") or {}).get("message") or "turn.failed"
        elif event.get("type") == "turn.completed":
            message = None
    return message


def build_command(codex, args, session, schema, report):
    common = ["-m", args.model, "-c", f'model_reasoning_effort="{args.effort}"', *NO_USER_INPUT,
              "--json", "--output-schema", str(schema), "-o", str(report)]
    if session:
        # resume accepts neither --sandbox nor --approve-for-me; restate the approved policy as config.
        if args.read_only:
            policy = ["-c", 'sandbox_mode="read-only"']
        else:
            policy = ["-c", 'sandbox_mode="workspace-write"', "-c", 'approval_policy="on-request"',
                      "-c", 'approvals_reviewer="guardian_subagent"']
        return [str(codex), "exec", "resume", session, *common, *policy, "-"]
    # --approve-for-me already implies workspace-write and rejects an explicit --sandbox.
    policy = ["-s", "read-only"] if args.read_only else ["--approve-for-me"]
    return [str(codex), "exec", *policy, *common, "-C", str(ROOT), "-"]


def kill_tree(process):
    if process.poll() is None:
        # codex spawns shells and node_repl; stop the whole tree so no second implementer survives.
        subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True)


def launch(command, text, events, stderr, timeout_min, on_start, env):
    with open(events, "ab") as out, open(stderr, "ab") as err:
        process = subprocess.Popen(command, cwd=ROOT, stdin=subprocess.PIPE, stdout=out, stderr=err, env=env)
        try:
            on_start(process.pid)
            try:
                process.stdin.write(text.encode("utf-8"))
                process.stdin.close()
            except BrokenPipeError:
                pass  # codex rejected its arguments before reading stdin; stderr.log has the reason
            try:
                return process.wait(timeout=timeout_min * 60 if timeout_min else None)
            except subprocess.TimeoutExpired:
                return "timeout"
        finally:
            kill_tree(process)


def run(args):
    if not TASK_ID.match(args.task):
        raise SystemExit("--task may contain only letters, digits and '-'.")
    brief = Path(args.brief).read_text(encoding="utf-8-sig")
    contract = (HERE / "sol-contract.md").read_text(encoding="utf-8")
    check_clean = not args.resume  # a follow-up continues on this task's own uncommitted changes
    if check_clean:
        require_clean(args.allow_dirty, "this run")  # fail fast, before taking the lock
    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%d-%H%M%S")
    run_dir = RUNS / args.task / stamp
    # Lock before the Unity checks (Launch can take minutes) so the hook already protects the tree; a refused
    # run leaves no folder because mkdir comes after every check.
    token = acquire_lock(run_dir)
    try:
        unity = unity_preflight() if args.unity else None
        if check_clean:
            require_clean(args.allow_dirty, "this run")  # again: nothing may change while Unity was checked
        started = time.time()
        codex = find_codex()  # before mkdir, like every other refusal
        run_dir.mkdir(parents=True, exist_ok=False)
        prompt = f"{contract}\n\n# タスク {args.task}\n\n{brief}\n"
        (run_dir / "prompt.md").write_text(prompt, encoding="utf-8")
        env, rg = child_env()
        version = subprocess.run([str(codex), "--version"], capture_output=True, text=True).stdout.strip()
        schema = HERE / "report.schema.json"
        report = run_dir / "report.json"
        events = run_dir / "events.jsonl"
        meta = {
            "task": args.task, "route": "cli", "model": args.model, "effort": args.effort,
            "sandbox": "read-only" if args.read_only else "workspace-write", "resume": args.resume,
            "scope": args.scope, "allow_dirty": args.allow_dirty, "timeout_min": args.timeout_min,
            "retries": args.retries, "rg": rg,
            "codex": str(codex), "codex_version": version, "attempts": [],
            "started_utc": now_iso(), "git_before": git_state(), "unity_before": unity,
        }
        write_meta(run_dir, meta)
        print(f"[sol] run dir: {run_dir}", flush=True)
        session, text = args.resume, prompt
        for attempt in range(args.retries + 1):
            command = build_command(codex, args, session, schema, report)
            offset = events.stat().st_size if events.exists() else 0
            current = {"command": command, "started_utc": now_iso(), "codex_pid": None}
            meta["attempts"].append(current)  # written before and during the attempt so a kill keeps the record

            def started_codex(pid):
                current["codex_pid"] = pid
                note_codex_pid(token, pid)
                write_meta(run_dir, meta)

            write_meta(run_dir, meta)
            exit_code = launch(command, text, events, run_dir / "stderr.log", args.timeout_min, started_codex, env)
            session = find_session_id(events) or session
            failure = last_failure(events, offset)
            current.update(exit_code=exit_code, failure=failure, ended_utc=now_iso())
            meta["session_id"] = session
            write_meta(run_dir, meta)
            lowered = (failure or "").lower()
            if exit_code == "timeout":
                meta["stop_reason"] = "timeout"
                print(f"[sol] attempt stopped after {args.timeout_min} min; resume {session} to continue.", flush=True)
                break
            if any(m in lowered for m in USAGE_LIMIT_MARKERS):
                meta["stop_reason"] = "usage_limit"
                print(f"[sol] account usage limit: {failure}. Not retrying.", flush=True)
                break
            retryable = failure and session and any(m in lowered for m in CAPACITY_MARKERS)
            if not retryable or attempt == args.retries:
                break
            wait = args.retry_wait_sec * (attempt + 1)
            print(f"[sol] {failure} -> resume {session} in {wait}s", flush=True)
            time.sleep(wait)
            text = RESUME_AFTER_CAPACITY
        meta.update(ended_utc=now_iso(), exit_code=exit_code, session_id=session, usage=cli_usage(events))
        try:
            parsed = json.loads(report.read_text(encoding="utf-8"))
            meta["report_status"] = parsed.get("status")
        except (OSError, json.JSONDecodeError):
            parsed = None
            meta["report_status"] = "missing_or_invalid"
        summary = finish(run_dir, meta, events, False, parsed, started, args.unity)
        print(json.dumps({
            "exit_code": exit_code, "session_id": meta["session_id"], "report_status": meta["report_status"],
            "stop_reason": meta.get("stop_reason"), "duration_sec": meta["duration_sec"],
            "usage": meta["usage"]["total"], "run_dir": str(run_dir), "summary": brief_line(summary),
        }, ensure_ascii=False, indent=2))
        return 0 if exit_code == 0 and parsed is not None else 1
    finally:
        release_lock(token)


# ---- desktop-app route ----

APP_THREAD_FILE = RUNS / "app-thread.json"  # {"thread_id": ...}: the user's desktop-app Sol thread
TERMINAL_TURN = ("completed", "failed", "interrupted")  # values observed in thread_turns.status
TASK_ID = re.compile(r"^[A-Za-z0-9-]+$")  # the nonce is matched with LIKE inside JSON text
MAX_COMMAND_LINE = 32000  # --message travels on the Windows command line (limit 32,767 after quoting)
APP_MODEL, APP_EFFORT = DEFAULT_MODEL, DEFAULT_EFFORT  # WORK-007: the app thread must be Sol at max
REPORT_KEYS = ("status", "summary", "changed_files", "verification", "unverified", "questions_for_leader", "notes")
REPORT_STATUS = ("completed", "partial", "blocked", "failed")
PENDING_EXIT = 2


def codex_db(name):
    home = Path(os.environ.get("CODEX_HOME") or Path.home() / ".codex")
    path = home / name
    if not path.is_file():
        raise SystemExit(f"Codex database not found: {path}")
    return contextlib.closing(sqlite3.connect(f"file:{path.as_posix()}?mode=ro", uri=True))


def app_preflight(thread):
    """Read-only checks before the irreversible queue: history schema, thread, model and folder."""
    needed = {"thread_items": {"thread_id", "turn_id", "item_id", "item_type", "item_json", "rollout_ordinal"},
              "thread_turns": {"turn_id", "status", "started_at", "completed_at", "error_json", "final_agent_item_id"}}
    with codex_db("thread_history_1.sqlite") as db:
        for table, columns in needed.items():
            missing = columns - {row[1] for row in db.execute(f"pragma table_info('{table}')")}
            if missing:
                raise SystemExit(f"Codex history schema changed ({table} lacks {sorted(missing)}); update sol.py first.")
    fields = ("model", "reasoning_effort", "sandbox_policy", "approval_mode", "cwd", "archived", "name")
    with codex_db("state_5.sqlite") as db:
        row = db.execute(f"select {', '.join(fields)} from threads where id=?", (thread,)).fetchone()
    if not row:
        raise SystemExit(f"Desktop-app thread {thread} is not in state_5.sqlite.")
    info = dict(zip(fields, row))
    cwd = re.sub(r"^\\\\\?\\", "", info["cwd"] or "")
    if not cwd or Path(cwd).resolve() != ROOT.resolve():
        raise SystemExit(f"Thread cwd {info['cwd']} is not {ROOT}.")
    if info["archived"]:
        raise SystemExit(f"Thread {thread} is archived.")
    if (info["model"], info["reasoning_effort"]) != (APP_MODEL, APP_EFFORT):
        raise SystemExit(f"Thread runs {info['model']} / {info['reasoning_effort']}; WORK-007 needs {APP_MODEL} / {APP_EFFORT}.")
    if "danger" in str(info["sandbox_policy"]).lower():
        raise SystemExit("Thread runs with full access; WORK-007 keeps Sol sandboxed. Change the thread in the app first.")
    return info


def thread_tokens(thread):
    try:
        with codex_db("state_5.sqlite") as db:
            if "tokens_used" not in {row[1] for row in db.execute("pragma table_info('threads')")}:
                return None
            row = db.execute("select tokens_used from threads where id=?", (thread,)).fetchone()
        return row[0] if row else None
    except (sqlite3.Error, SystemExit):
        return None


def find_app_turn(thread, nonce):
    """The first user message carrying `nonce` and its turn row, or None until the app starts it."""
    keys = ("status", "started_at", "completed_at", "error_json", "final_agent_item_id")
    with codex_db("thread_history_1.sqlite") as db:
        row = db.execute("select item_id, turn_id from thread_items where thread_id=? and item_type='userMessage' "
                         "and item_json like ? order by rollout_ordinal limit 1", (thread, f"%{nonce}%")).fetchone()
        if not row:
            return None
        turn = db.execute(f"select {', '.join(keys)} from thread_turns where turn_id=?", (row[1],)).fetchone()
    return {"user_item_id": row[0], "turn_id": row[1], **(dict(zip(keys, turn)) if turn else {"status": "not_recorded"})}


def turn_progress(thread, turn_id):
    with codex_db("thread_history_1.sqlite") as db:
        count, last = db.execute("select count(*), max(rollout_ordinal) from thread_items where thread_id=? and turn_id=?",
                                 (thread, turn_id)).fetchone()
        kind = db.execute("select item_type from thread_items where thread_id=? and turn_id=? "
                          "order by rollout_ordinal desc limit 1", (thread, turn_id)).fetchone()
    return {"items": count, "last_ordinal": last, "last_type": kind[0] if kind else None}


def turn_done(turn):
    return bool(turn) and (turn.get("status") in TERMINAL_TURN or turn.get("completed_at") is not None)


def save_turn_items(thread, turn, run_dir):
    """Write every item of the turn (commands, file changes, tool calls) and return the final answer."""
    with codex_db("thread_history_1.sqlite") as db:
        items = db.execute("select item_id, item_type, item_json from thread_items where thread_id=? and turn_id=? "
                           "order by rollout_ordinal", (thread, turn["turn_id"])).fetchall()
    final = None
    with open(run_dir / "items.jsonl", "w", encoding="utf-8") as out:
        for item_id, kind, raw in items:
            item = json.loads(raw)
            out.write(json.dumps({"item_id": item_id, "item_type": kind, "item": item}, ensure_ascii=False) + "\n")
            if item_id == turn.get("final_agent_item_id"):
                final = item.get("text")
    return final, len(items)


def parse_report(text):
    if not text:
        return None
    body = re.sub(r"^```(?:json)?\s*|\s*```$", "", text.strip())
    try:
        report = json.loads(body)
    except json.JSONDecodeError:
        return None
    if not isinstance(report, dict) or any(k not in report for k in REPORT_KEYS) or report["status"] not in REPORT_STATUS:
        return None
    return report


def write_meta(run_dir, meta):
    # write-then-replace: a kill during the write (sol.py stop) must not leave a truncated meta.json
    temp = run_dir / f"meta.json.{os.getpid()}.tmp"
    temp.write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")
    os.replace(temp, run_dir / "meta.json")


PENDING_HINT = {
    "not_started": "the app has not started the queued brief (is the Codex desktop app open and the thread idle?)",
    "stalled": "the turn produced no new item for a while (an approval, question or permission prompt in the app?)",
    "timeout": "the turn is still running",
}


def wait_app(run_dir, meta, deadline, start_min, stall_min):
    """Poll the app's history until the queued turn ends; PENDING_EXIT when it has not ended yet."""
    turn, progress, reason = None, None, "timeout"
    begun = changed = time.time()
    try:
        while True:
            turn = find_app_turn(meta["thread"], meta["nonce"])
            if turn_done(turn):
                reason = None
                break
            moment = time.time()
            if turn is None:
                if start_min and moment - begun >= start_min * 60:
                    reason = "not_started"
                    break
            else:
                current = turn_progress(meta["thread"], turn["turn_id"])
                if current != progress:
                    progress, changed = current, moment
                elif stall_min and moment - changed >= stall_min * 60:
                    reason = "stalled"
                    break
            if moment >= deadline:
                break
            time.sleep(10)
    except (sqlite3.Error, OSError) as error:
        meta["wait_error"] = repr(error)
    meta["turn"] = turn
    meta.setdefault("waits", []).append({"until_utc": now_iso(), "status": turn["status"] if turn else "not_started",
                                         "reason": reason, "progress": progress})
    if not turn_done(turn):
        meta["turn_status"] = turn["status"] if turn else "not_started"
        write_meta(run_dir, meta)
        print(f"[sol app] {meta['turn_status']} ({reason}): {PENDING_HINT.get(reason, '')}. The lock is kept; "
              f"check later with `python tools/orchestra/sol.py app-wait --run-dir \"{run_dir}\"`.", flush=True)
        return PENDING_EXIT
    final, count = save_turn_items(meta["thread"], turn, run_dir)
    report = parse_report(final)
    if report is not None:
        (run_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    else:  # keep the raw answer only when it is not the schema JSON (it would duplicate report.json)
        (run_dir / "final.md").write_text(final or "", encoding="utf-8")
    after = thread_tokens(meta["thread"])
    before = meta.get("thread_tokens_before")
    meta.update(turn_status=turn["status"], item_count=count, ended_utc=now_iso(),
                report_status=report["status"] if report else "missing_or_invalid",
                thread_tokens_delta=(after - before) if isinstance(after, int) and isinstance(before, int) else None)
    summary = finish(run_dir, meta, run_dir / "items.jsonl", True, report, meta["started_epoch"], bool(meta.get("unity_before")))
    print(json.dumps({"turn_id": turn["turn_id"], "turn_status": turn["status"], "report_status": meta["report_status"],
                      "items": count, "duration_sec": meta["duration_sec"], "thread_tokens_delta": meta["thread_tokens_delta"],
                      "run_dir": str(run_dir), "summary": brief_line(summary)}, ensure_ascii=False, indent=2))
    return 0 if turn["status"] == "completed" and report else 1


def app(args):
    if not TASK_ID.match(args.task):
        raise SystemExit("--task may contain only letters, digits and '-'.")
    if args.timeout_min <= 0:
        raise SystemExit("--timeout-min must be positive for app runs; use app-wait to keep waiting.")
    thread = args.thread or json.loads(APP_THREAD_FILE.read_text(encoding="utf-8"))["thread_id"]
    info = app_preflight(thread)
    require_clean(args.allow_dirty, "this app run")
    nonce = f"{args.task}-{uuid.uuid4().hex[:8]}"
    brief = Path(args.brief).read_text(encoding="utf-8-sig")
    contract = (HERE / "sol-contract.md").read_text(encoding="utf-8")
    schema = (HERE / "report.schema.json").read_text(encoding="utf-8")
    prompt = (f"{contract}\n\n# タスク {args.task}（受付番号 {nonce}）\n\n"
              "このターンはDesktopアプリ内で実行されるため、Computer Useの撮影・操作が使える。\n\n"
              f"{brief}\n\n最終応答は次のJSON Schemaに合うJSONだけにする。\n```json\n{schema}\n```\n")
    command = [str(find_codex()), "queue", "--thread", thread, "--message", prompt]
    if len(subprocess.list2cmdline(command)) > MAX_COMMAND_LINE:
        raise SystemExit(f"The queue command line exceeds {MAX_COMMAND_LINE} chars after quoting; shorten the brief.")
    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%d-%H%M%S")
    run_dir = RUNS / args.task / stamp
    token = acquire_lock(run_dir)  # before the Unity checks, as in run()
    pending = False
    try:
        unity = unity_preflight() if args.unity else None
        require_clean(args.allow_dirty, "this app run")
        run_dir.mkdir(parents=True, exist_ok=False)
        (run_dir / "prompt.md").write_text(prompt, encoding="utf-8")
        meta = {"task": args.task, "route": "app", "thread": thread, "thread_info": info, "effort": info["reasoning_effort"],
                "nonce": nonce, "scope": args.scope, "allow_dirty": args.allow_dirty,
                "waits_config": {"timeout_min": args.timeout_min, "start_min": args.start_min, "stall_min": args.stall_min},
                "started_utc": now_iso(), "started_epoch": time.time(),
                "thread_tokens_before": thread_tokens(thread), "git_before": git_state(), "unity_before": unity}
        write_meta(run_dir, meta)
        # From the queue call on, the app may run the brief even if this process dies: keep the lock.
        LOCK.write_text(f"{token} app_pending thread={thread} nonce={nonce}\n", encoding="utf-8")
        pending = True
        try:
            queued = subprocess.run(command, cwd=ROOT, capture_output=True, text=True,
                                    encoding="utf-8", errors="replace", timeout=120)
        except OSError as error:  # codex never started, so nothing was queued
            meta["queue_error"] = repr(error)
            write_meta(run_dir, meta)
            pending = False
            raise
        meta.update(queue_exit=queued.returncode, queue_output=(queued.stdout + queued.stderr).strip())
        write_meta(run_dir, meta)
        print(f"[sol app] run dir: {run_dir}\n{meta['queue_output']}", flush=True)
        if queued.returncode != 0:
            pending = False
            return 1
        result = wait_app(run_dir, meta, meta["started_epoch"] + args.timeout_min * 60, args.start_min, args.stall_min)
        pending = result == PENDING_EXIT
        return result
    finally:
        if not pending:
            release_lock(token)


def app_wait(args):
    run_dir = Path(args.run_dir).resolve()
    meta = json.loads((run_dir / "meta.json").read_text(encoding="utf-8"))
    if meta.get("ended_utc"):
        raise SystemExit(f"{run_dir} already finished ({meta.get('turn_status')}); its record is kept unchanged.")
    holder = LOCK.read_text(encoding="utf-8").strip() if LOCK.exists() else ""
    if holder and f"run={run_dir} " not in holder + " ":
        raise SystemExit(f"{LOCK} belongs to another run: {holder}")
    if not holder:  # take the lock again so no other implementer starts while this turn may run
        token = acquire_lock(run_dir)
        LOCK.write_text(f"{token} app_pending thread={meta['thread']} nonce={meta['nonce']}\n", encoding="utf-8")
    else:
        token = holder.split(" app_pending")[0]
    result = wait_app(run_dir, meta, time.time() + args.timeout_min * 60, args.start_min, args.stall_min)
    if result != PENDING_EXIT:
        release_lock(token)
    return result


# ---- leader utilities ----

def stop(args):
    """End a CLI run whose sol.py was killed or must stop now (TaskStop alone can leave codex running)."""
    if not LOCK.exists():
        print("No Sol run holds the lock.")
        return 0
    holder = LOCK.read_text(encoding="utf-8").strip()
    if "app_pending" in holder:
        raise SystemExit("A desktop-app turn cannot be stopped from here: interrupt it in the Codex app, then run "
                         "app-wait to record it.")
    python, codex = lock_pids(holder)
    since = lock_mtime()
    survivors = []
    for pid, kind in ((codex, "codex"), (python, "python")):
        # only the processes that wrote this lock: a reused PID may belong to the Codex app or the MCP server
        state = is_run_process(pid, kind, since) if pid else False
        if state == UNKNOWN:
            raise SystemExit(f"Could not read the process table for {kind} PID {pid}; the lock is kept. Check it by hand.")
        if state:
            # codex: its whole tree (shells, node_repl). sol.py: itself only, because during `--unity` checks
            # its children are the Unity Editor and MCP server that UnityAgent.ps1 Launch started.
            tree = ["/T"] if kind == "codex" else []
            subprocess.run(["taskkill", "/PID", str(pid), *tree, "/F"], capture_output=True)
            if is_run_process(pid, kind, since) is not False:
                survivors.append(pid)
            else:
                print(f"[sol] stopped {kind} PID {pid}" + (" and its children" if tree else ""))
    if survivors:
        raise SystemExit(f"PID {survivors} survived taskkill; the lock is kept. Check them before retrying.")
    run = re.search(r"run=(.+?)(?: codex_pid=| app_pending|$)", holder)
    run_dir = Path(run.group(1)) if run else None
    try:
        meta = json.loads((run_dir / "meta.json").read_text(encoding="utf-8")) if run_dir else None
    except (OSError, json.JSONDecodeError) as error:
        print(f"[sol] could not update {run_dir / 'meta.json'}: {error!r}")
        meta = None
    if meta is not None:
        events = run_dir / "events.jsonl"
        if not meta.get("session_id") and events.exists():
            meta["session_id"] = find_session_id(events)  # lets the leader resume the stopped work
        meta.update(stopped_utc=now_iso(), stop_reason=meta.get("stop_reason") or "sol.py stop")
        write_meta(run_dir, meta)
        for leftover in run_dir.glob("meta.json.*.tmp"):  # a write the kill interrupted
            leftover.unlink(missing_ok=True)
    LOCK.unlink(missing_ok=True)
    print(f"[sol] lock removed: {holder}")
    return 0


def status(args):
    """Lock holder and the newest runs with their cost, result and review count (for resuming work)."""
    if LOCK.exists():
        holder = LOCK.read_text(encoding="utf-8").strip()
        alive = None if "app_pending" in holder else holder_alive(holder)
        state = {None: "app turn pending", True: "running", False: "STALE"}.get(alive, "UNKNOWN (process table unreadable)")
        print(f"lock: {state}: {holder}")
    else:
        print("lock: none")
    metas = sorted(RUNS.glob("*/*/meta.json"), key=lambda p: p.parent.name)
    metas = metas[-args.count:] if args.count > 0 else []  # [-0:] would list every run
    for path in metas:
        summary_path = path.parent / "summary.json"
        try:
            meta = json.loads(path.read_text(encoding="utf-8"))
            summary = json.loads(summary_path.read_text(encoding="utf-8")) if summary_path.exists() else {}
        except (OSError, json.JSONDecodeError) as error:
            print(json.dumps({"run": path.parent.relative_to(RUNS).as_posix(), "unreadable": repr(error)}))
            continue
        usage = (meta.get("usage") or {}).get("total", {})
        tokens = usage.get("input_tokens", meta.get("thread_tokens_delta"))
        row = {
            "run": path.parent.relative_to(RUNS).as_posix(), "route": meta.get("route", "cli"),
            "effort": meta.get("effort"), "result": meta.get("report_status") or meta.get("turn_status"),
            "ended": bool(meta.get("ended_utc")), "stop": meta.get("stop_reason"), "sec": meta.get("duration_sec"),
            "tokens_in": tokens, "tokens_out": usage.get("output_tokens"),
            "review": summary.get("suggested_review"), "reviews": len(list(path.parent.glob("review-*.md"))),
        }
        print(json.dumps(row, ensure_ascii=False))
    return 0


def latest(args):
    runs = sorted((RUNS / args.task).glob("*/meta.json"))
    if not runs:
        raise SystemExit(f"No runs for {args.task}")
    print(runs[-1].read_text(encoding="utf-8"))
    return 0


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    def common(p):
        p.add_argument("--task", required=True, help="task id (letters, digits, '-'), e.g. PLAN-001")
        p.add_argument("--brief", required=True, help="UTF-8 markdown brief written by the leader")
        p.add_argument("--scope", action="append", default=[],
                       help="allowed path glob (fnmatch, repeatable), e.g. 'Assets/Scripts/Elevator/*'")
        p.add_argument("--unity", action="store_true", help="Launch/check Unity before and record it after")
        p.add_argument("--allow-dirty", action="store_true", help="accept an existing dirty tree (follow-ups)")

    start = sub.add_parser("run", help="send a brief to Sol and wait for its structured report")
    common(start)
    start.add_argument("--model", default=DEFAULT_MODEL)
    start.add_argument("--effort", default=DEFAULT_EFFORT, help="max for implementation; high for evidence/test-only runs")
    start.add_argument("--read-only", action="store_true", help="investigation only; no workspace writes")
    start.add_argument("--resume", help="continue an earlier Sol session id (review feedback, follow-ups)")
    start.add_argument("--timeout-min", type=float, default=CLI_TIMEOUT_MIN, help="kill each attempt after N minutes (0 = no limit)")
    start.add_argument("--retries", type=int, default=4, help="resumes after a model-capacity failure")
    start.add_argument("--retry-wait-sec", type=int, default=180, help="base wait; grows linearly per retry")
    desk = sub.add_parser("app", help="queue a brief into the desktop-app Sol thread (OS-level Computer Use only)")
    common(desk)
    desk.add_argument("--thread", help=f"thread id; defaults to {APP_THREAD_FILE.relative_to(ROOT).as_posix()}")
    again = sub.add_parser("app-wait", help="keep waiting for an app run that returned exit 2")
    again.add_argument("--run-dir", required=True)
    for p in (desk, again):
        p.add_argument("--timeout-min", type=float, default=120,
                       help="stop waiting after N minutes; an unfinished turn keeps the lock (exit 2)")
        p.add_argument("--start-min", type=float, default=5, help="exit 2 if the app has not started the turn by then")
        p.add_argument("--stall-min", type=float, default=20, help="exit 2 if the turn adds no item for N minutes")
    sub.add_parser("stop", help="kill the CLI run that holds the lock and remove the lock")
    show = sub.add_parser("status", help="lock holder and the newest runs (cost, result, reviews)")
    show.add_argument("--count", type=int, default=8)
    last = sub.add_parser("latest", help="print the newest run metadata for a task")
    last.add_argument("--task", required=True)
    args = parser.parse_args()
    handlers = {"run": run, "app": app, "app-wait": app_wait, "stop": stop, "status": status, "latest": latest}
    return handlers[args.command](args)


if __name__ == "__main__":
    raise SystemExit(main())
