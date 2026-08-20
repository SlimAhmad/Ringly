using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class RecordingDependencyValidationException : Xeption
{
    public RecordingDependencyValidationException(Xeption innerException)
        : base("Recording dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
