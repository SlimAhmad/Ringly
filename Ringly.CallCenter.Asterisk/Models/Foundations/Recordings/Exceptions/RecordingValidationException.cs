using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

public class RecordingValidationException : Xeption
{
    public RecordingValidationException(Xeption innerException)
        : base("Recording validation error occurred, fix errors and try again.", innerException)
    { }
}
