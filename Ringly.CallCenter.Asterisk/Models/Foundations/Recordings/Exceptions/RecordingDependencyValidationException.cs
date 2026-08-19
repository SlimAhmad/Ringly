using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

public class RecordingDependencyValidationException : Xeption
{
    public RecordingDependencyValidationException(Xeption innerException)
        : base("Recording dependency validation error occurred, fix errors and try again.", innerException)
    { }
}
