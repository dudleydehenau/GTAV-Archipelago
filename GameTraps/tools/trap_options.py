import json, sys
c = json.load(open(sys.argv[1]))
out = []
def fmt(v):
    if isinstance(v, bool): return "`true`" if v else "`false`"
    if isinstance(v, (int, float)): return f"`{v}`"
    if isinstance(v, str): return f'`"{v}"`' if v else '`""`'
    if isinstance(v, list):
        if all(isinstance(x, str) for x in v) and len(", ".join(v)) < 70:
            return ", ".join(f"`{x}`" for x in v) if v else "empty list"
        return f"list of {len(v)}"
    if isinstance(v, dict):
        s = json.dumps(v, separators=(", ", ": "))
        return f"`{s}`" if len(s) < 70 else "object"
    return str(v)
for k, v in c.items():
    if not isinstance(v, dict): continue
    out.append(f"<details><summary><code>{k}</code></summary>\n\n| Setting | Default |\n|---|---|")
    for kk, vv in v.items():
        out.append(f"| `{kk}` | {fmt(vv)} |")
    out.append("\n</details>\n")
print("\n".join(out))
