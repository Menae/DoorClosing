"""WORK-007: run the Sol implementer (Codex CLI) non-interactively for the Opus leader.

Each run stores the exact prompt, JSONL events, structured report and git state under
artifacts/orchestra/<task>/<UTC stamp>/ so the leader and the independent reviewer can audit it.
Standard library only.
"""
import argparse
import datetime as dt
import glob
import json
import os
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
RUNS = ROOT / "artifacts" / "orchestra"
LOCK = RUNS / ".sol.lock"
DEFAULT_MODEL = "gpt-6.1-sol"
DEFAULT_EFFORT = "max"
# Server-side saturation is transient: resume the same session instead of switching models.
CAPACITY_MARKERS = ("at capacity", "overloaded", "rate limit", "try again later")
RESUME_AFTER_CAPACITY = (
    "前のターンはモデルの容量不足（サーバー側）で中断した。作業状態とファイルを確認し直し、"
    "同じタスクを続けて完了させ、最後に指定スキーマのJSONだけで報告すること。")


def find_codex():
    override = os.environ.get("CODEX_CLI_PATH")
    if override and Path(override).is_file():
        return Path(override)
    # The desktop app installs the CLI under a hash folder that changes on update.
    pattern = os.path.join(os.environ.get("LOCALAPPDATA", ""), "OpenAI", "Codex", "bin", "*", "codex.exe")
    candidates = [Path(p) for p in glob.glob(pattern)]
    if not candidates:
        raise SystemExit("codex.exe not found. Install/update the Codex desktop app or set CODEX_CLI_PATH.")
    return max(candidates, key=lambda p: p.stat().st_mtime)


def git(*args):
    result = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True, encoding="utf-8")
    return result.stdout.rstrip("\n")  # keep porcelain's leading status column


def git_state():
    return {
        "head": git("rev-parse", "HEAD"),
        "status": git("status", "--porcelain=v1", "-uall").splitlines(),
        "staged": git("diff", "--cached", "--name-only").splitlines(),
    }


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
    common = ["-m", args.model, "-c", f'model_reasoning_effort="{args.effort}"',
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


def launch(command, text, events, stderr, timeout_min, on_start):
    with open(events, "ab") as out, open(stderr, "ab") as err:
        process = subprocess.Popen(command, cwd=ROOT, stdin=subprocess.PIPE, stdout=out, stderr=err)
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


def acquire_lock(run_dir):
    RUNS.mkdir(parents=True, exist_ok=True)
    token = f"pid={os.getpid()} run={run_dir}"
    try:
        handle = os.open(LOCK, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError:
        holder = LOCK.read_text(encoding="utf-8").strip()
        raise SystemExit(f"Another Sol run holds {LOCK}: {holder}. Confirm that python and codex PIDs ended before removing it.")
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
    touched = []
    for current, dirs, files in os.walk(root):
        here = Path(current)
        if here == root:
            # cache/ and server/ are written by uv and the leader's MCP server, not by Sol.
            dirs[:] = [d for d in dirs if d not in ("cache", "server")]
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


def run(args):
    brief = Path(args.brief).read_text(encoding="utf-8-sig")
    contract = (HERE / "sol-contract.md").read_text(encoding="utf-8")
    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%d-%H%M%S")
    run_dir = RUNS / args.task / stamp
    run_dir.mkdir(parents=True, exist_ok=False)
    token = acquire_lock(run_dir)
    started = time.time()
    try:
        prompt = f"{contract}\n\n# タスク {args.task}\n\n{brief}\n"
        (run_dir / "prompt.md").write_text(prompt, encoding="utf-8")
        codex = find_codex()
        version = subprocess.run([str(codex), "--version"], capture_output=True, text=True).stdout.strip()
        schema = HERE / "report.schema.json"
        report = run_dir / "report.json"
        events = run_dir / "events.jsonl"
        meta = {
            "task": args.task, "model": args.model, "effort": args.effort,
            "sandbox": "read-only" if args.read_only else "workspace-write", "resume": args.resume,
            "codex": str(codex), "codex_version": version, "attempts": [],
            "started_utc": dt.datetime.now(dt.timezone.utc).isoformat(), "git_before": git_state(),
        }
        (run_dir / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"[sol] run dir: {run_dir}", flush=True)
        session, text = args.resume, prompt
        for attempt in range(args.retries + 1):
            command = build_command(codex, args, session, schema, report)
            offset = events.stat().st_size if events.exists() else 0
            pids = []
            exit_code = launch(command, text, events, run_dir / "stderr.log", args.timeout_min,
                               lambda pid: (pids.append(pid), note_codex_pid(token, pid)))
            session = find_session_id(events) or session
            failure = last_failure(events, offset)
            meta["attempts"].append({"command": command, "codex_pid": pids[0] if pids else None,
                                     "exit_code": exit_code, "failure": failure,
                                     "ended_utc": dt.datetime.now(dt.timezone.utc).isoformat()})
            retryable = failure and session and any(m in failure.lower() for m in CAPACITY_MARKERS)
            if not retryable or attempt == args.retries:
                break
            wait = args.retry_wait_sec * (attempt + 1)
            print(f"[sol] {failure} -> resume {session} in {wait}s", flush=True)
            time.sleep(wait)
            text = RESUME_AFTER_CAPACITY
        meta["ended_utc"] = dt.datetime.now(dt.timezone.utc).isoformat()
        meta["exit_code"] = exit_code
        meta["session_id"] = session
        meta["git_after"] = git_state()
        meta["ignored_writes"] = ignored_writes(started, run_dir)
        try:
            parsed = json.loads(report.read_text(encoding="utf-8"))
            meta["report_status"] = parsed.get("status")
        except (OSError, json.JSONDecodeError):
            parsed = None
            meta["report_status"] = "missing_or_invalid"
        (run_dir / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps({
            "exit_code": exit_code, "session_id": meta["session_id"], "report_status": meta["report_status"],
            "staged_by_run": meta["git_after"]["staged"], "status_after": meta["git_after"]["status"],
            "report": str(report),
        }, ensure_ascii=False, indent=2))
        return 0 if exit_code == 0 and parsed is not None else 1
    finally:
        release_lock(token)


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
    start = sub.add_parser("run", help="send a brief to Sol and wait for its structured report")
    start.add_argument("--task", required=True, help="task id, e.g. PLAN-001; becomes the artifacts folder")
    start.add_argument("--brief", required=True, help="UTF-8 markdown brief written by the leader")
    start.add_argument("--model", default=DEFAULT_MODEL)
    start.add_argument("--effort", default=DEFAULT_EFFORT)
    start.add_argument("--read-only", action="store_true", help="investigation only; no workspace writes")
    start.add_argument("--resume", help="continue an earlier Sol session id (review feedback, follow-ups)")
    start.add_argument("--timeout-min", type=float, default=0, help="kill each attempt after N minutes (0 = no limit)")
    start.add_argument("--retries", type=int, default=4, help="resumes after a model-capacity failure")
    start.add_argument("--retry-wait-sec", type=int, default=180, help="base wait; grows linearly per retry")
    show = sub.add_parser("latest", help="print the newest run metadata for a task")
    show.add_argument("--task", required=True)
    args = parser.parse_args()
    return run(args) if args.command == "run" else latest(args)


if __name__ == "__main__":
    raise SystemExit(main())
