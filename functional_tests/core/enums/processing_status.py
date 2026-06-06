from enum import StrEnum


class ProcessingStatus(StrEnum):
    PENDING_DISPATCH = "PendingDispatch"
    QUEUED = "Queued"
    PROCESSING = "Processing"
    COMPLETED = "Completed"
    FAILED = "Failed"


IN_FLIGHT_STATUSES = {
    ProcessingStatus.PENDING_DISPATCH,
    ProcessingStatus.QUEUED,
    ProcessingStatus.PROCESSING,
}
