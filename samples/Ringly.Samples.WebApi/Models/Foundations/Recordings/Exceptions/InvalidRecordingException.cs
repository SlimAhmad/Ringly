using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class InvalidRecordingException : Xeption
{
    public InvalidRecordingException()
        : base("Recording is invalid.")
    { }
}
