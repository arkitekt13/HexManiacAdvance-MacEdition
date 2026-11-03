#!/usr/bin/env python3
"""Validate discovery and TRX evidence; an empty or aborted test run cannot pass."""
import argparse
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


def summarize(discovery_path, trx_path, format="trx"):
    discovered = [line.strip() for line in Path(discovery_path).read_text().splitlines()
                  if line.startswith("    HavenSoft.HexManiac.")]
    if not discovered:
        raise ValueError("No upstream tests were discovered.")
    root = ET.parse(trx_path).getroot()
    if format == "xunit":
        assemblies = root.findall("assembly")
        if len(assemblies) != 1:
            raise ValueError("Expected one completed xUnit assembly.")
        assembly = assemblies[0]
        values = {key: int(assembly.get(key, "0")) for key in ("total", "passed", "failed", "skipped", "errors")}
        results = assembly.findall("collection/test")
        outcomes = [r.get("result") for r in results]
        if (values["total"] <= values["skipped"] or values["total"] != len(results)
                or values["total"] != values["passed"] + values["failed"] + values["skipped"]
                or outcomes.count("Pass") != values["passed"] or outcomes.count("Fail") != values["failed"]
                or outcomes.count("Skip") != values["skipped"] or values["errors"] or assembly.findall("errors/error")):
            raise ValueError("xUnit results are empty, incomplete, inconsistent, or include engine errors.")
        failures = [{"test": r.get("name"), "message": r.findtext("failure/message", default="")}
                    for r in results if r.get("result") == "Fail"]
        return {"format": "xunit", "discovered_entries": len(discovered), "total": values["total"],
                "executed": values["passed"] + values["failed"], "passed": values["passed"], "failed": values["failed"],
                "notExecuted": values["skipped"], "failures": failures}
    namespace = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    counters = root.find("t:ResultSummary/t:Counters", namespace)
    result_summary = root.find("t:ResultSummary", namespace)
    results = root.findall("t:Results/t:UnitTestResult", namespace)
    if counters is None or result_summary is None:
        raise ValueError("TRX has no result summary/counters.")
    values = {key: int(counters.get(key, "0")) for key in
              ("total", "executed", "passed", "failed", "notExecuted", "error", "timeout", "aborted", "notRunnable")}
    if values["executed"] <= 0 or values["total"] != len(results):
        raise ValueError("TRX is empty, entirely skipped, or incomplete.")
    outcomes = [r.get("outcome") for r in results]
    if (values["total"] != values["passed"] + values["failed"] + values["notExecuted"]
            or values["executed"] != values["passed"] + values["failed"]
            or outcomes.count("Passed") != values["passed"]
            or outcomes.count("Failed") != values["failed"]
            or outcomes.count("NotExecuted") != values["notExecuted"]):
        raise ValueError("TRX counters do not match completed case outcomes.")
    if any(values[key] for key in ("error", "timeout", "aborted", "notRunnable")):
        raise ValueError("TRX includes infrastructure errors or aborted cases.")
    if result_summary.get("outcome") not in ("Completed", "Passed", "Failed"):
        raise ValueError("TRX reports an incomplete or aborted run.")
    if result_summary.get("outcome") == "Failed" and values["failed"] == 0:
        raise ValueError("TRX reports failure outside the individual test cases.")
    failures = []
    for result in results:
        if result.get("outcome") != "Failed":
            continue
        message = result.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=namespace)
        failures.append({"test": result.get("testName"), "message": message})
    return {"discovered_entries": len(discovered), **values, "failures": failures}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--format", choices=("trx", "xunit"), default="trx")
    parser.add_argument("discovery")
    parser.add_argument("trx")
    parser.add_argument("summary")
    args = parser.parse_args()
    try:
        summary = summarize(args.discovery, args.trx, args.format)
    except (OSError, ET.ParseError, ValueError) as error:
        print(f"FAIL: invalid upstream test evidence: {error}", file=sys.stderr)
        return 2
    Path(args.summary).write_text(json.dumps(summary, indent=2) + "\n")
    print(f"Upstream: {summary['discovered_entries']} discovery entries; "
          f"{summary['passed']} passed, {summary['failed']} failed, {summary['notExecuted']} skipped "
          f"({summary['total']} cases).")
    for failure in summary["failures"][:10]:
        print(f"FAIL: {failure['test']}: {failure['message'][:300]}")
    return 1 if summary["failed"] else 0


if __name__ == "__main__":
    sys.exit(main())
