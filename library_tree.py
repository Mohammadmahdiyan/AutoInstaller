from pathlib import Path
import re

ROOT = Path(".")
OUTPUT = ROOT / "LibraryTree.txt"
EXTENSIONS = {
    ".cs",
    ".user",
    ".csproj",
    ".txt",
    ".gitignore",
    ".resx",
    ".targets",
    ".props",
    ".pdb",
    ".exe",
    ".dll",
}

ignoredFolders = {
    "obj/Debug",
    "obj/Release",
    "Release/net8.0-windows/win-x64"
}

folders = 0
files = 0


def is_ignored(folder: Path) -> bool:
    folder_posix = folder.as_posix()
    return any(folder_posix.endswith(ignored) for ignored in ignoredFolders)


def contains_book(folder: Path) -> bool:
    """آیا این پوشه یا زیرپوشه‌هایش فایل pdf/docx دارند؟"""
    for item in folder.iterdir():
        if item.is_file() and item.suffix.lower() in EXTENSIONS:
            return True
        if item.is_dir():
            if is_ignored(item):
                continue
            if contains_book(item):
                return True
    return False


def children(folder: Path):
    dirs = []
    docs = []

    for item in folder.iterdir():
        if item.is_dir():
            if is_ignored(item):
                continue
            if contains_book(item):
                dirs.append(item)
        elif item.suffix.lower() in EXTENSIONS:
            docs.append(item)

    dirs.sort(key=lambda x: x.name.casefold())
    docs.sort(key=lambda x: x.name.casefold())

    return dirs + docs


def walk(folder: Path, prefix: str, out):
    global folders, files

    items = children(folder)

    # Determine numbering for sibling directories at this level: if more
    # than one directory exists among items, prefix them with 01-, 02-, ...;
    # otherwise leave the single directory unnumbered. Strip existing leading
    # numeric prefixes from folder names to avoid duplication.
    dirs = [it for it in items if it.is_dir()]
    total_dirs = len(dirs)

    for i, item in enumerate(items):
        last = i == len(items) - 1
        branch = "└── " if last else "├── "
        next_prefix = prefix + ("    " if last else "│   ")

        if item.is_dir():
            folders += 1
            # base name without any existing leading NN- prefix
            base = re.sub(r"^\s*\d{1,3}-", "", item.name)
            if total_dirs > 1:
                dir_index = dirs.index(item)
                num = f"{dir_index+1:02d}-"
            else:
                num = ""
            out.write(f"{prefix}{branch}📁 {num}{base}\n")
            walk(item, next_prefix, out)
        else:
            files += 1
            out.write(f"{prefix}{branch}📄 {item.name}\n")


if OUTPUT.exists():
    OUTPUT.write_text("", encoding="utf-8")

with open(OUTPUT, "w", encoding="utf-8") as out:
    out.write("📚 کتابخانه\n\n")
    walk(ROOT, "", out)

    out.write("\n")
    out.write("────────────────────────────\n")
    out.write(f"پوشه‌ها: {folders}\n")
    out.write(f"فایل‌ها: {files}\n")

print("✅ ساخته شد:", OUTPUT)