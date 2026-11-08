#!/usr/bin/env python3
# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

"""Normalize staged text without reading worktree contents or reapplying clean filters."""
import os
import subprocess
import sys


def git(*arguments, data=None):
    """Run Git with literal paths and preserve arbitrary filename bytes in its output."""
    return subprocess.run(["git", "--literal-pathspecs", *arguments], input=data, check=True,
                          stdout=subprocess.PIPE).stdout


def normalize_staged_file(path_bytes):
    """Replace only CRLF in one staged regular text blob, preserving its path and mode."""
    path = os.fsdecode(path_bytes)
    records = git("ls-files", "--stage", "-z", "--", path).split(b"\0")
    entries = [record for record in records if record]
    if len(entries) != 1:
        raise RuntimeError("Cannot identify one staged entry for " + repr(path))
    metadata, separator, actual_path = entries[0].partition(b"\t")
    if not separator or actual_path != path_bytes:
        raise RuntimeError("Staged entry does not match requested path " + repr(path))
    mode, object_id, stage = metadata.split()
    if stage != b"0" or mode not in (b"100644", b"100755"):
        return

    attributes = git("check-attr", "--cached", "-z", "binary", "text", "--", path).split(b"\0")
    if len(attributes) != 7 or attributes[-1] != b"":
        raise RuntimeError("Cannot read staged attributes for " + repr(path))
    if attributes[2] == b"set" or attributes[5] == b"unset":
        return

    # Inspect the entire blob; a NUL after a long text prefix still makes this binary.
    original = git("cat-file", "blob", object_id.decode("ascii"))
    if b"\0" in original:
        return
    normalized = original.replace(b"\r\n", b"\n")
    if normalized == original:
        return
    normalized_id = git("hash-object", "-w", "--stdin", "--no-filters", data=normalized).strip()
    # Update the original literal path and mode directly, never git add or clean filters.
    git("update-index", "-z", "--index-info",
        data=mode + b" " + normalized_id + b"\t" + path_bytes + b"\0")
    print("pre-commit: normalized staged content: " + repr(path))


def main():
    """Process NUL-delimited index paths, propagating enumeration and normalization failures."""
    paths = git("diff", "--cached", "--name-only", "--diff-filter=ACM", "-z")
    for path in paths.split(b"\0"):
        if path:
            normalize_staged_file(path)


if __name__ == "__main__":
    try:
        main()
    except (OSError, RuntimeError, subprocess.CalledProcessError) as error:
        print("pre-commit: " + str(error), file=sys.stderr)
        sys.exit(1)
