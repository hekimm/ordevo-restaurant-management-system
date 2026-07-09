namespace Ordevo.Web.Api;

public sealed class OrdevoApiOptions
{
    public const string SectionName = "OrdevoApi";

    public Uri BaseUrl { get; set; } = new("http://localhost:5000");
    public int TimeoutSeconds { get; set; } = 20;
}
