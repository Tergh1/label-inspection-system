from datetime import datetime
from pathlib import Path

METRICS_FILE = Path("performance_metrics.txt")


def save_metrics(metrics):

    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    lines = []

    lines.append("=" * 80)
    lines.append(f"Timestamp: {timestamp}")
    lines.append(f"Image ID: {metrics.image_id}")
    lines.append("")

    lines.append(
        f"Template download: {metrics.template_download_ms:.2f} ms"
    )

    lines.append(
        f"Image download: {metrics.image_download_ms:.2f} ms"
    )

    lines.append("")

    for model in metrics.model_metrics:

        lines.append(f"Model: {model.model}")

        lines.append(
            f"  Feature extraction: {model.feature_extraction_ms:.2f} ms"
        )

        lines.append(
            f"  Similarity: {model.similarity_ms:.2f} ms"
        )

        lines.append(
            f"  Defect detection: {model.defect_detection_ms:.2f} ms"
        )

        lines.append("")

    lines.append(
        f"Webhook: {metrics.webhook_ms:.2f} ms"
    )

    lines.append(
        f"Total request duration: {metrics.total_ms:.2f} ms"
    )

    lines.append("")
    lines.append("")

    with open(METRICS_FILE, "a", encoding="utf-8") as f:
        f.write("\n".join(lines))