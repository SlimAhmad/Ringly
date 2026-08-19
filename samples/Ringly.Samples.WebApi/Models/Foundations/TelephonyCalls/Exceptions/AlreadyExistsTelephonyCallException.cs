using Xeptions;

namespace Ringly.Samples.WebApi.Models.Foundations.TelephonyCalls.Exceptions;

public class AlreadyExistsTelephonyCallException : Xeption
{
    public AlreadyExistsTelephonyCallException(Exception innerException)
        : base("Telephony call already exists.", innerException)
    { }
}
