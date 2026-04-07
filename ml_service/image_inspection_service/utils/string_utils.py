@staticmethod
def extract_result_string(result: dict) -> str:
    result_string = ", ".join(
        f"{r.get('model', 'unknown')}: "
        f"{(r.get('similarity_percent', r.get('similarity', 0)) * 100):.2f}% "
        f"{len(r.get('defects', []))} "
        f"{'defect' if len(r.get('defects', [])) == 1 else 'defects'}"
        for r in result.get("results", [])
    )

    return result_string