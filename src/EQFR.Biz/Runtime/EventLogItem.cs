namespace EQFR.Biz.Runtime;

public sealed record EventLogItem(DateTimeOffset Timestamp, string Message, string? EntityId = null, string? EventType = null);

