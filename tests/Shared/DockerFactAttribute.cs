using Xunit;

namespace Vitarish.Testing;

internal sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!IsDockerAvailable())
            Skip = "Integration coverage requires an available Docker engine.";
    }

    private static bool IsDockerAvailable() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")) ||
        (OperatingSystem.IsWindows()
            ? File.Exists(@"\\.\pipe\docker_engine") || File.Exists(@"\\.\pipe\dockerDesktopLinuxEngine")
            : File.Exists("/var/run/docker.sock"));
}
