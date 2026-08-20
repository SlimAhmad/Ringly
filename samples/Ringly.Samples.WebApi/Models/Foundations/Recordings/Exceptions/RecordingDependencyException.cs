using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class RecordingDependencyException : Xeption
{
    public RecordingDependencyException(Xeption innerException)
        : base("Recording dependency error occurred, contact support.", innerException)
    { }
}
