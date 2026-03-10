namespace client.Data.Entities;

public enum ProcessingStatus
{
    PendingDispatch = 0,
    Queued = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}
