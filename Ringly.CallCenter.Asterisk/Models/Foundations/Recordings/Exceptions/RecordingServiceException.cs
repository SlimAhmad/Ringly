using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

public class RecordingServiceException : Xeption
{
    public RecordingServiceException(Xeption innerException)
        : base("Recording service error occurred, contact support.", innerException)
    { }
}
