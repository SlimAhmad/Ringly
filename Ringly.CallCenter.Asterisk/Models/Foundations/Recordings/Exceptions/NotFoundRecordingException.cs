using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

public class NotFoundRecordingException : Xeption
{
    public NotFoundRecordingException(Exception innerException)
        : base("Recording not found.", innerException)
    { }
}
