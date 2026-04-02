namespace EQFR.Common;

public sealed record TickOptions(TimeSpan TickInterval, int MaxRecentEvents = 200);

