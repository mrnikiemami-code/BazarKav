#!/usr/bin/env python3
"""Structural guards only; runtime and type-level guards belong to .NET tests."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
errors = []

def fail(path, message):
    errors.append(f"{path.relative_to(ROOT)}: {message}")

def classify(path):
    p = path.relative_to(ROOT).parts
    if len(p) == 5 and p[:2] == ("src", "Modules"):
        module, layer = p[2:4]
        if layer in {"Domain", "Application", "Infrastructure", "Presentation", "Contracts"}:
            return ("module", module, layer)
    if len(p) == 4 and p[:2] == ("src", "BuildingBlocks"):
        if p[2] in {"Application", "Observability", "Web"}:
            return ("shared", p[2], "")
    if len(p) == 3 and p[:2] == ("src", "Host"):
        return ("host", "", "")
    return None

projects = {}
graph = {}
for path in (ROOT / "src").rglob("*.csproj"):
    if any(part in {"bin", "obj"} for part in path.parts):
        continue
    info = classify(path)
    if not info:
        fail(path, "project must be at an approved layer root")
        continue
    projects[path.resolve()] = info
    kind, name, layer = info
    expected = (f"BazarKav.{name}.{layer}.csproj" if kind == "module" else
                f"BazarKav.BuildingBlocks.{name}.csproj" if kind == "shared" else
                "BazarKav.Host.csproj")
    if path.name != expected:
        fail(path, f"expected project name {expected}")

def allowed(source, target):
    kind, name, layer = source
    tk, tn, tl = target
    if kind == "module":
        if layer in {"Domain", "Contracts"}:
            return False
        if tk == "module":
            if name != tn:
                return tl == "Contracts"
            return tl in {
                "Application": {"Domain", "Contracts"},
                "Infrastructure": {"Domain", "Application", "Contracts"},
                "Presentation": {"Application", "Contracts"}
            }[layer]
        return tk == "shared" and tn in {
            "Application": {"Application"},
            "Infrastructure": {"Application", "Observability"},
            "Presentation": {"Application", "Web"}
        }[layer]
    if kind == "shared":
        return tk == "shared" and tn in {
            "Application": set(), "Observability": set(),
            "Web": {"Application", "Observability"}
        }[name]
    return ((tk == "module" and tl in {"Infrastructure", "Presentation"}) or
            (tk == "shared" and tn in {"Web", "Observability"}))

for path, info in projects.items():
    graph[path] = []
    try:
        nodes = list(ET.parse(path).iter())
    except ET.ParseError as exc:
        fail(path, f"invalid project XML: {exc}")
        continue
    pure = info[0] == "module" and info[2] in {"Domain", "Contracts"}
    for node in nodes:
        tag = node.tag.rsplit("}", 1)[-1]
        if pure and tag in {"PackageReference", "FrameworkReference", "Reference"}:
            fail(path, "Domain/Contracts cannot reference external packages/frameworks/assemblies")
        if tag == "Import":
            fail(path, "custom project imports need an explicit architecture guard decision")
        if tag == "ProjectReference":
            value = node.get("Include")
            if not value or re.search(r"[$*?;]", value):
                fail(path, "reference must be a concrete relative path")
                continue
            target = (path.parent / value.replace("\\", "/")).resolve()
            graph[path].append(target)
            if target not in projects or not allowed(info, projects[target]):
                fail(path, f"forbidden or missing reference: {value}")

for pattern in ("*.props", "*.targets"):
    for path in ROOT.rglob(pattern):
        if any(p in {".git", "bin", "obj"} for p in path.parts):
            continue
        try:
            nodes = list(ET.parse(path).iter())
        except ET.ParseError:
            fail(path, "invalid shared build XML")
            continue
        if any(n.tag.rsplit("}", 1)[-1] in {"ProjectReference", "Import"} for n in nodes):
            fail(path, "shared build files cannot hide project references/imports")

visiting, visited = set(), set()
def visit(path):
    if path in visiting:
        fail(path, "cyclic project dependency")
        return
    if path in visited:
        return
    visiting.add(path)
    for target in graph.get(path, []):
        if target in projects:
            visit(target)
    visiting.remove(path)
    visited.add(path)
for path in projects:
    visit(path)

for path in (ROOT / "src").rglob("*.cs"):
    rel = path.relative_to(ROOT)
    if any(p in {"bin", "obj"} for p in rel.parts):
        continue
    p = rel.parts
    if p[:2] == ("src", "Host"):
        if p != ("src", "Host", "Program.cs"):
            fail(path, "Host permits only Program.cs; feature code belongs to modules")
    elif p[:2] == ("src", "Modules"):
        if len(p) < 5 or p[3] not in {"Domain", "Application", "Infrastructure", "Presentation", "Contracts"}:
            fail(path, "C# file outside module layer")
            continue
        if not any(project.parent == ROOT.joinpath(*p[:4]) for project in projects):
            fail(path, "module layer needs its own csproj")
        # *Request is reserved for HTTP input DTOs. Inspect declarations as well as
        # filenames so a combined TenantRequests.cs cannot bypass the location guard.
        request_types = re.findall(
            r"\b(?:class|record(?:\s+(?:class|struct))?|struct)\s+([A-Za-z_]\w*Request)\b",
            path.read_text(encoding="utf-8"))
        if request_types or path.name.endswith("Request.cs"):
            if p[3] != "Presentation" or len(p) < 6 or p[4] != "Requests":
                fail(path, "HTTP request DTO belongs in Presentation/Requests, never Contracts")
            if len(request_types) != 1 or path.stem != request_types[0]:
                fail(path, "each HTTP request DTO needs its own matching file")
        required = None
        if re.search(r"(Command|CommandHandler)\.cs$", path.name):
            required = "Commands"
        elif re.search(r"(Query|QueryHandler)\.cs$", path.name):
            required = "Queries"
        elif path.name.endswith("Validator.cs"):
            required = "Validation"
        if required and (p[3] != "Application" or p[4] != required):
            fail(path, f"file belongs in Application/{required}")
        if p[3] == "Application" and p[4] not in {"Commands", "Queries", "Validation", "DTOs", "Abstractions"}:
            fail(path, "unapproved Application directory")
    elif p[:2] == ("src", "BuildingBlocks"):
        if len(p) < 4 or p[2] not in {"Application", "Observability", "Web"}:
            fail(path, "unapproved BuildingBlocks area")
        elif not any(project.parent == ROOT.joinpath(*p[:3]) for project in projects):
            fail(path, "BuildingBlocks area needs its own csproj")
    else:
        fail(path, "production C# must be in Host, Modules or BuildingBlocks")

if errors:
    print("\n".join(errors))
    sys.exit(1)
print(f"Architecture structure PASS ({len(projects)} production projects; runtime not checked)")
