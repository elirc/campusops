"""Create a new external lab from current CampusOps source; never edit the application."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, help="New absolute directory outside CampusOps")
    parser.add_argument("--suite", choices=["workflow", "policy"], default="workflow")
    args = parser.parse_args()
    here = Path(__file__).resolve().parent
    project = here.parents[3]
    template = (here / ("Program.cs.txt" if args.suite == "workflow" else "PolicyProgram.cs.txt")).read_text(encoding="utf-8")
    extras = [
        "src/CampusOps.Infrastructure/Requests/InMemoryMaintenanceRequestStore.cs",
        "src/CampusOps.Infrastructure/WorkOrders/InMemoryWorkOrderStore.cs",
        "src/CampusOps.Infrastructure/WorkOrders/InMemoryWorkOrderHistoryStore.cs",
    ]
    if args.suite == "policy":
        extras.append("src/CampusOps.Infrastructure/Integrations/InMemoryWebhookStore.cs")
    prepared = [(relative, (project / relative).read_bytes()) for relative in extras]
    generator = here.parent.parent / "lab" / "generate_lab.py"
    subprocess.run([sys.executable, str(generator), "--mode", "reference", "--output", args.output], check=True)
    output = Path(args.output).expanduser().resolve()
    map_path = output / "SOURCE-MAP.json"
    source_map = json.loads(map_path.read_text(encoding="utf-8"))
    for relative, raw in prepared:
        destination = output / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(raw)
        digest = hashlib.sha256(raw).hexdigest()
        source_map["files"].append({"path": relative, "original_sha256": digest,
                                    "lab_sha256": digest, "deliberate_defects": []})
    source_map["suite"] = "extended-" + args.suite + "-observations"
    map_path.write_text(json.dumps(source_map, indent=2) + "\n", encoding="utf-8")
    (output / "Program.cs").write_text(template, encoding="utf-8")
    (output / "README.txt").write_text(
        "Bounded " + args.suite + " checks using current copied source.\n"
        "Run: dotnet run --project Lab.csproj\n"
        "Observation checks document current limitations, not desired production guarantees.\n",
        encoding="utf-8")


if __name__ == "__main__":
    main()
