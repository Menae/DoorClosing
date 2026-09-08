"""Pinned MCP SDK fallback. Tool calls verify the target Unity project before execution."""
import argparse
import asyncio
import json
import sys
from datetime import timedelta
from pathlib import Path
from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client


def model(value):
    return value.model_dump(mode="json", exclude_none=True)


def payload(result):
    value = result.get("structuredContent")
    if value is None:
        blocks = result.get("content", result.get("contents", []))
        for block in blocks:
            if "text" in block:
                try:
                    value = json.loads(block["text"])
                    break
                except json.JSONDecodeError:
                    continue
    if isinstance(value, dict) and "result" in value:
        value = value["result"]
    return value


def require_success(result):
    value = payload(result)
    if result.get("isError") or (isinstance(value, dict) and value.get("success") is False):
        raise RuntimeError(json.dumps(value or result, ensure_ascii=False))


async def run(args):
    async with streamablehttp_client(args.url) as (read, write, _):
        async with ClientSession(read, write, read_timeout_seconds=timedelta(seconds=args.timeout)) as session:
            await session.initialize()
            if args.instance:
                require_success(model(await session.call_tool("set_active_instance", {"instance": args.instance})))
            if args.command == "call":
                if not args.instance:
                    raise ValueError("Tool calls require --instance from mcpforunity://instances.")
                info = model(await session.read_resource("mcpforunity://project/info"))
                require_success(info)
                actual = Path(payload(info)["data"]["projectRoot"]).resolve()
                if actual != Path(args.project).resolve():
                    raise ValueError(f"Wrong Unity project: {actual}. Expected {args.project}")
            for group in args.group:
                require_success(model(await session.call_tool("manage_tools", {"action": "activate", "group": group})))
            if args.command == "tools":
                result = model(await session.list_tools())["tools"]
                result = [t for t in result if not args.name or t["name"] == args.name]
                if not args.name:
                    result = [{"name": t["name"], "description": t.get("description", "")} for t in result]
            elif args.command == "resources":
                result = model(await session.list_resources())
            elif args.command == "resource":
                result = model(await session.read_resource(args.uri))
            else:
                arguments = json.loads(Path(args.args_file).read_text(encoding="utf-8-sig")) if args.args_file else json.loads(args.arguments)
                result = model(await session.call_tool(args.name, arguments))
            serialized = json.dumps(result, ensure_ascii=False, indent=2)
            if args.output:
                destination = Path(args.output)
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_text(serialized + "\n", encoding="utf-8")
            if not args.quiet:
                print(serialized)
            if isinstance(result, dict):
                require_success(result)


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--url", default="http://127.0.0.1:8080/mcp")
    parser.add_argument("--instance", help="Exact Name@hash from the instances resource")
    parser.add_argument("--project", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--timeout", type=float, default=120)
    parser.add_argument("--group", action="append", default=[])
    parser.add_argument("--output")
    parser.add_argument("--quiet", action="store_true")
    sub = parser.add_subparsers(dest="command", required=True)
    schema = sub.add_parser("tools"); schema.add_argument("--name")
    sub.add_parser("resources")
    resource = sub.add_parser("resource"); resource.add_argument("uri")
    call = sub.add_parser("call"); call.add_argument("name")
    call.add_argument("--arguments", default="{}"); call.add_argument("--args-file")
    try:
        asyncio.run(run(parser.parse_args()))
    except Exception as error:
        def describe(exception):
            nested = getattr(exception, "exceptions", ())
            return "; ".join(describe(child) for child in nested) if nested else str(exception)
        print(f"MCP client failed: {describe(error)}", file=sys.stderr)
        raise SystemExit(1)


if __name__ == "__main__":
    main()
