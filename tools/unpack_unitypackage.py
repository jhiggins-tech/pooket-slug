"""Unpack a .unitypackage into a Unity project, laid out exactly as the editor's importer would.

A .unitypackage is a gzipped tar of `<guid>/pathname` (target path), `<guid>/asset` (file contents)
and `<guid>/asset.meta` (GUID + import settings). CI uses this to drop a privately stored SDK into
the project before Unity starts, so the SDK never has to be committed.

usage: python3 -I tools/unpack_unitypackage.py <package.unitypackage> <project-root>
"""
import os
import sys
import tarfile


def main(package_path: str, project_root: str) -> None:
    root = os.path.abspath(project_root)
    entries: dict[str, dict[str, bytes]] = {}
    with tarfile.open(package_path, "r:gz") as tar:
        for member in tar.getmembers():
            parts = member.name.lstrip("./").split("/")
            if len(parts) == 2 and member.isfile():
                data = tar.extractfile(member)
                if data is not None:
                    entries.setdefault(parts[0], {})[parts[1]] = data.read()

    files = 0
    for guid, item in entries.items():
        if "pathname" not in item:
            continue
        rel = item["pathname"].decode("utf-8").splitlines()[0].strip()
        dest = os.path.abspath(os.path.join(root, rel))
        if not rel.startswith(("Assets/", "Packages/")) or not dest.startswith(root + os.sep):
            sys.exit(f"refusing to write outside the project: {rel}")
        if "asset" in item:
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with open(dest, "wb") as f:
                f.write(item["asset"])
            files += 1
        else:
            os.makedirs(dest, exist_ok=True)
        if "asset.meta" in item:
            with open(dest + ".meta", "wb") as f:
                f.write(item["asset.meta"])

    print(f"unpacked {files} files ({len(entries)} entries) from {os.path.basename(package_path)} into {root}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2])
