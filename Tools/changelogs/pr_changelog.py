#!/usr/bin/env python3

import argparse
import json
import re
from pathlib import Path

HEADER_RE = re.compile(r"^\s*(?::cl:|🆑)\s*$", re.IGNORECASE)
COMMENT_RE = re.compile(r"<!--.*?-->", re.DOTALL)
ENTRY_RE = re.compile(
    r"^\s*[-*]?\s*(?P<section>добавлено|удалено|изменено|исправлено):\s*"
    r"(?P<message>\S.*)$",
    re.IGNORECASE,
)

SECTION_TITLES = {
    "добавлено": "🆕 Добавлено",
    "удалено": "❌ Удалено",
    "изменено": "🛠️ Изменено",
    "исправлено": "🐛 Исправлено",
}

FIELD_VALUE_LIMIT = 1024


def parse_changes(body: str) -> dict[str, list[str]]:
    body = COMMENT_RE.sub("", body).replace("\r\n", "\n").replace("\r", "\n")
    lines = iter(body.splitlines())

    if not any(HEADER_RE.match(line) for line in lines):
        return {}

    changes: dict[str, list[str]] = {}
    for line in lines:
        match = ENTRY_RE.match(line)
        if not match:
            continue

        section = match.group("section").lower()
        message = re.sub(r"\s+", " ", match.group("message")).strip()
        changes.setdefault(section, []).append(message)

    return changes


def build_embed_fields(changes: dict[str, list[str]]) -> list[dict[str, object]]:
    fields: list[dict[str, object]] = []

    for section, title in SECTION_TITLES.items():
        messages = changes.get(section)
        if not messages:
            continue

        value = "\n".join(f"• {message}" for message in messages)
        if len(value) > FIELD_VALUE_LIMIT:
            value = value[: FIELD_VALUE_LIMIT - 1].rstrip() + "…"

        fields.append({"name": title, "value": value, "inline": False})

    return fields


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--event-file", required=True, type=Path)
    parser.add_argument("--output-file", required=True, type=Path)
    parser.add_argument("--username", default="Красная Звезда")
    args = parser.parse_args()

    event = json.loads(args.event_file.read_text(encoding="utf-8"))
    pull_request = event["pull_request"]
    changes = parse_changes(pull_request.get("body") or "")

    if not changes:
        print("No changelog entries found in PR body.")
        args.output_file.unlink(missing_ok=True)
        return 0

    number = event["number"]
    url = pull_request["html_url"]
    title = (pull_request.get("title") or f"Изменение (PR #{number})").strip()
    author = pull_request["user"]["login"]

    payload = {
        "username": args.username,
        "allowed_mentions": {"parse": []},
        "embeds": [
            {
                "title": title,
                "url": url,
                "description": f"Список изменений из [PR #{number}]({url})",
                "fields": build_embed_fields(changes),
                "color": 14360064,
                "footer": {"text": f"Автор: {author} • PR #{number}"},
                "timestamp": pull_request["merged_at"],
            }
        ],
    }

    args.output_file.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"Rendered Discord payload: {args.output_file}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
