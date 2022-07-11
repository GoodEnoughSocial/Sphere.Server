using Sphere.Shared;

namespace Sphere.Server.Options;

public class MainOptions
{
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public ServiceDefinition[] Services { get; set; } = Array.Empty<ServiceDefinition>();
}

public class ConnectionStrings
{
    public string AccountStore { get; set; } = string.Empty;
    public string ProfileStore { get; set; } = string.Empty;
    public string Orleans { get; set; } = string.Empty;
}
