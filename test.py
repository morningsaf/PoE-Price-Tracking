import json

result = {}
with open("stats.json", "r", encoding="utf-8") as f:
    data = json.load(f)
    for el in data["result"]:
        for pos in el["entries"]:
            result[pos["id"]] = pos["text"]

with open("mods.json", "w", encoding="utf-8") as f:
    json.dump(result, f, ensure_ascii=False, indent=2)
