"""WORK-007: run the Sol implementer (Codex CLI) non-interactively for the Opus leader.

Each run stores the exact prompt, JSONL events, structured report and git state under
artifacts/orchestra/<task>/<UTC stamp>/ so the leader and the independent reviewer can audit it.
`run` uses `codex exec`; `app` queues the brief into the user's Codex desktop-app Sol thread,
where Computer Use approvals work (CU-001). Standard library only.
"""
import argparse
import contextlib
import datetime as dt
import glob
import json
import os
import re
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
        if "app_pending" in holder:
            run = re.search(r"run=(.+?) app_pending", holder)
            raise SystemExit(f"A desktop-app Sol turn may still be queued or running ({holder}). Use "
                             f"`python tools/orchestra/sol.py app-wait --run-dir \"{run.group(1) if run else '<run dir>'}\"`.")
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
    token = acquire_lock(run_dir)  # before mkdir: a refused run leaves no empty folder
    started = time.time()
    try:
        run_dir.mkdir(parents=True, exist_ok=False)
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
    (run_dir / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")


def wait_app(run_dir, meta, deadline):
    """Poll the app's history until the queued turn ends; PENDING_EXIT when it has not ended yet."""
    turn = None
    try:
        while True:
            turn = find_app_turn(meta["thread"], meta["nonce"])
            if turn_done(turn) or time.time() >= deadline:
                break
            time.sleep(10)
    except (sqlite3.Error, OSError) as error:
        meta["wait_error"] = repr(error)
    meta["turn"] = turn
    meta.setdefault("waits", []).append({"until_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
                                         "status": turn["status"] if turn else "not_started"})
    if not turn_done(turn):
        meta["turn_status"] = turn["status"] if turn else "not_started"
        write_meta(run_dir, meta)
        print(f"[sol app] {meta['turn_status']}: the brief stays queued or running in the app. The lock is kept; "
              f"check later with `python tools/orchestra/sol.py app-wait --run-dir \"{run_dir}\"`.", flush=True)
        return PENDING_EXIT
    final, count = save_turn_items(meta["thread"], turn, run_dir)
    meta.update(turn_status=turn["status"], item_count=count, ended_utc=dt.datetime.now(dt.timezone.utc).isoformat(),
                git_after=git_state(), ignored_writes=ignored_writes(meta["started_epoch"], run_dir))
    (run_dir / "final.md").write_text(final or "", encoding="utf-8")
    report = parse_report(final)
    if report is not None:
        (run_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    meta["report_status"] = report["status"] if report else "missing_or_invalid"
    write_meta(run_dir, meta)
    print(json.dumps({"turn_id": turn["turn_id"], "turn_status": turn["status"], "report_status": meta["report_status"],
                      "items": count, "staged_by_run": meta["git_after"]["staged"], "run_dir": str(run_dir)},
                     ensure_ascii=False, indent=2))
    return 0 if turn["status"] == "completed" and report else 1


def app(args):
    if not TASK_ID.match(args.task):
        raise SystemExit("--task may contain only letters, digits and '-'.")
    if args.timeout_min <= 0:
        raise SystemExit("--timeout-min must be positive for app runs; use app-wait to keep waiting.")
    thread = args.thread or json.loads(APP_THREAD_FILE.read_text(encoding="utf-8"))["thread_id"]
    info = app_preflight(thread)
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
    token = acquire_lock(run_dir)
    pending = False
    try:
        run_dir.mkdir(parents=True, exist_ok=False)
        (run_dir / "prompt.md").write_text(prompt, encoding="utf-8")
        meta = {"task": args.task, "route": "desktop-app thread", "thread": thread, "thread_info": info,
                "nonce": nonce, "started_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
                "started_epoch": time.time(), "git_before": git_state()}
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
        result = wait_app(run_dir, meta, meta["started_epoch"] + args.timeout_min * 60)
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
    result = wait_app(run_dir, meta, time.time() + args.timeout_min * 60)
    if result != PENDING_EXIT:
        release_lock(token)
    return result


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
    desk = sub.add_parser("app", help="queue a brief into the desktop-app Sol thread (Computer Use work)")
    desk.add_argument("--task", required=True)
    desk.add_argument("--brief", required=True)
    desk.add_argument("--thread", help=f"thread id; defaults to {APP_THREAD_FILE.relative_to(ROOT).as_posix()}")
    desk.add_argument("--timeout-min", type=float, default=120,
                      help="stop waiting after N minutes; an unfinished turn keeps the lock (exit 2)")
    again = sub.add_parser("app-wait", help="keep waiting for an app run that returned exit 2")
    again.add_argument("--run-dir", required=True)
    again.add_argument("--timeout-min", type=float, default=120)
    show = sub.add_parser("latest", help="print the newest run metadata for a task")
    show.add_argument("--task", required=True)
    args = parser.parse_args()
    return {"run": run, "app": app, "app-wait": app_wait, "latest": latest}[args.command](args)


if __name__ == "__main__":
    raise SystemExit(main())
