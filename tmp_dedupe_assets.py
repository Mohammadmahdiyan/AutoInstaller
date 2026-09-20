import json
from pathlib import Path

p = Path(r"H:\B\AA-MiniProject\ModAutoInstaller\Assets\Assets.json")
data = json.loads(p.read_text(encoding="utf-8"))
cleaned = {}
removed_total = 0

for section, items in data.items():
    if not isinstance(items, list):
        cleaned[section] = items
        continue

    seen = set()
    unique_items = []
    removed = 0

    for item in items:
        if isinstance(item, dict):
            name_file = str(item.get("nameFile") or "").strip().lower()
            name = str(item.get("name") or "").strip().lower()
            image = str(item.get("image") or "").strip().lower()
            if name_file:
                identity = f"nameFile:{name_file}"
            elif name or image:
                identity = f"name:{name}|image:{image}"
            else:
                identity = json.dumps(item, ensure_ascii=False, sort_keys=True)
        else:
            identity = json.dumps(item, ensure_ascii=False, sort_keys=True)

        if identity in seen:
            removed += 1
            removed_total += 1
            continue

        seen.add(identity)
        unique_items.append(item)

    cleaned[section] = unique_items
    print(f"{section}: removed={removed}, kept={len(unique_items)}")

p.write_text(json.dumps(cleaned, ensure_ascii=False, indent=4) + "\n", encoding="utf-8")
print(f"duplicates_removed_total={removed_total}")
