from statistics import mean

from image_inspection_service.services.performance_collector import collector


def generate_report():

    with open("performance_report.txt", "w") as f:

        f.write("ML SERVICE PERFORMANCE REPORT\n")
        f.write("=" * 50 + "\n\n")

        f.write(
            f"Total Requests: {collector.total_requests}\n"
        )

        f.write(
            f"Average Request Duration: "
            f"{collector.total_request_ms / collector.total_requests:.2f} ms\n\n"
        )

        f.write(
            f"Average Template Download Duration: "
            f"{mean(collector.template_download_ms):.2f} ms\n\n"
        )

        f.write(
            f"Average Image Download Duration: "
            f"{mean(collector.image_download_ms):.2f} ms\n\n"
        )

        f.write(
            f"ResNet Average Feature Extraction: "
            f"{mean(collector.resnet_extraction_ms):.2f} ms\n"
        )

        f.write(
            f"EfficientNet Average Feature Extraction: "
            f"{mean(collector.efficientnet_extraction_ms):.2f} ms\n\n"
        )

        f.write(
            f"Similarity Computation Average: "
            f"{mean(collector.similarity_ms):.2f} ms\n\n"
        )

        f.write(
            f"Defect Detection Average: "
            f"{mean(collector.defect_detection_ms):.2f} ms\n\n"
        )

        f.write(
            f"Webhook Sending Duration Average: "
            f"{mean(collector.webhook_ms):.2f} ms\n"
        )
