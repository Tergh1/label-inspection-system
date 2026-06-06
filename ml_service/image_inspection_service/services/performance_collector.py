from dataclasses import dataclass, field
from threading import Lock


@dataclass
class PerformanceCollector:

    total_requests: int = 0

    total_request_ms: float = 0

    template_download_ms: list = field(default_factory=list)
    image_download_ms: list = field(default_factory=list)

    resnet_extraction_ms: list = field(default_factory=list)
    efficientnet_extraction_ms: list = field(default_factory=list)

    similarity_ms: list = field(default_factory=list)

    defect_detection_ms: list = field(default_factory=list)

    webhook_ms: list = field(default_factory=list)

    lock: Lock = field(default_factory=Lock)

    def add_request(self, metrics):

        with self.lock:

            self.total_requests += 1

            self.total_request_ms += metrics.total_ms

            self.template_download_ms.append(metrics.template_download_ms)
            self.image_download_ms.append(metrics.image_download_ms)

            for model in metrics.model_metrics:

                if model.model == "ResNet18":
                    self.resnet_extraction_ms.append(
                        model.feature_extraction_ms
                    )

                elif model.model == "EfficientNetB0":
                    self.efficientnet_extraction_ms.append(
                        model.feature_extraction_ms
                    )

                self.similarity_ms.append(
                    model.similarity_ms
                )

                self.defect_detection_ms.append(
                    model.defect_detection_ms
                )
            
            self.webhook_ms.append(metrics.webhook_ms)

collector = PerformanceCollector()