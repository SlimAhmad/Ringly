using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Recordings.Exceptions;

public class InvalidRecordingRequestException : Xeption
{
    public InvalidRecordingRequestException()
        : base("Recording request is invalid.")
    { }
}
