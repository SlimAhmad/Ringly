using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class NullRecordingException : Xeption
{
    public NullRecordingException()
        : base("Recording is null.")
    { }
}
