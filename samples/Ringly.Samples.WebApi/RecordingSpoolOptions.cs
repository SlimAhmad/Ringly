namespace Ringly.Samples.WebApi;

// Asterisk (in docker/docker-compose.yml) bind-mounts its recording spool directory to
// docker/asterisk-recordings on the host — this app runs natively, not in that compose network,
// so it reads a just-finished recording file from that same host path before uploading it via
// IRecordingStorageProvider. Directory is relative to this project's own working directory,
// matching how this repo's README already runs `dotnet run` from here.
public class RecordingSpoolOptions
{
    public string Directory { get; set; } = string.Empty;
}
