from dataclasses import dataclass, field
from time import perf_counter


@dataclass
class ModelMetrics:
    model: str
    feature_extraction_ms: float = 0
    similarity_ms: float = 0
    defect_detection_ms: float = 0


@dataclass
class RequestMetrics:
    image_id: str

    request_started: float = field(default_factory=perf_counter)
    request_finished: float = 0

    template_download_ms: float = 0
    image_download_ms: float = 0

    model_metrics: list[ModelMetrics] = field(default_factory=list)

    webhook_ms: float = 0

    @property
    def total_ms(self):
        return (self.request_finished - self.request_started) * 1000