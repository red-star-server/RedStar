#!/usr/bin/env python3

import argparse
import subprocess
from pathlib import Path

BOM = b"\xef\xbb\xbf"


def git_output(repo: Path, *args: str) -> list[Path]:
    result = subprocess.run(
        ["git", *args],
        cwd=repo,
        capture_output=True,
        check=True,
    )

    return [
        Path(item.decode("utf-8", errors="surrogateescape"))
        for item in result.stdout.split(b"\0")
        if item
    ]


def get_branch_files(repo: Path, base: str) -> set[Path]:
    files = set()

    files.update(git_output(
        repo,
        "diff", "--name-only", "-z",
        "--diff-filter=ACMR", f"{base}...HEAD"
    ))

    files.update(git_output(
        repo,
        "diff", "--name-only", "-z",
        "--diff-filter=ACMR", "HEAD"
    ))

    files.update(git_output(
        repo,
        "ls-files", "--others", "--exclude-standard", "-z"
    ))

    return files


def normalize(data: bytes) -> tuple[bytes, bool]:
    original = data

    if b"\0" in data:
        return original, False

    if data.startswith(BOM):
        data = data[len(BOM):]

    try:
        content = data.decode("utf-8")
    except UnicodeDecodeError:
        return original, False

    content = content.replace("\r\n", "\n").replace("\r", "\n")

    if content and not content.endswith("\n"):
        content += "\n"

    result = content.encode("utf-8")
    return result, result != original


def main():
    parser = argparse.ArgumentParser(
        description="Normalize files changed in the current Git branch."
    )

    parser.add_argument(
        "--base",
        default="origin/master",
        help="Base branch (default: origin/master)"
    )

    parser.add_argument(
        "--path",
        default=".",
        help="Directory to process (default: entire repository)"
    )

    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Preview changes without modifying files"
    )

    args = parser.parse_args()

    repo = Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True,
        text=True,
        check=True,
    ).stdout.strip()).resolve()

    root = (repo / args.path).resolve()

    if not root.is_dir():
        parser.error(f"Directory does not exist: {root}")

    try:
        files = get_branch_files(repo, args.base)
    except subprocess.CalledProcessError as error:
        parser.error(
            f"Git error. Check base branch '{args.base}': {error}"
        )

    checked = 0
    changed = 0
    skipped = 0

    print(f"Repository: {repo}")
    print(f"Directory:  {root}")
    print(f"Candidates: {len(files)}")
    print()

    for relative in sorted(files):

        original_path = repo / relative

        if original_path.is_symlink():
            skipped += 1
            print(f"[SKIPPED SYMLINK] {relative}")
            continue

        file = original_path.resolve()

        if not file.is_relative_to(root):
            continue

        if not file.is_file():
            continue

        checked += 1

        try:
            original = file.read_bytes()
            normalized, modified = normalize(original)

            if not modified:
                continue

            changed += 1

            if not args.dry_run:
                file.write_bytes(normalized)

            action = "WOULD NORMALIZE" if args.dry_run else "NORMALIZED"
            print(f"[{action}] {relative}")

        except OSError as error:
            skipped += 1
            print(f"[ERROR] {relative}: {error}")

    print()
    print(f"Files checked: {checked}")
    print(f"Files changed: {changed}")
    print(f"Files skipped/errors: {skipped}")


if __name__ == "__main__":
    main()
