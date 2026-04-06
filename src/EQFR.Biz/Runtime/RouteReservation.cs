namespace EQFR.Biz.Runtime;

public sealed record RouteReservation(string EdgeId, string TransportId, DateTimeOffset ReservedAt);

