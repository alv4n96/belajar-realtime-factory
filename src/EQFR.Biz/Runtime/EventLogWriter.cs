namespace EQFR.Biz.Runtime;

public static class EventLogWriter
{
    public static void Add(FactoryState state, DateTimeOffset now, string message, string? entityId, string eventType)
    {
        state.RecentEvents.Add(new EventLogItem(now, message, entityId, eventType));

        var max = state.TickOptions.MaxRecentEvents;
        while (state.RecentEvents.Count > max)
        {
            state.RecentEvents.RemoveAt(0);
        }
    }
}

