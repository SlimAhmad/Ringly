using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.Recordings.Exceptions;

public class AlreadyExistsRecordingException : Xeption
{
    public AlreadyExistsRecordingException(Exception innerException)
        : base("Recording already exists.", innerException)
    { }
}
