namespace EQFR.UI.Services;

public sealed class MockBackendOptions
{
    public string BaseUrl { get; set; } = "http://localhost:3001";
    public int PollIntervalMs { get; set; } = 250;
}
