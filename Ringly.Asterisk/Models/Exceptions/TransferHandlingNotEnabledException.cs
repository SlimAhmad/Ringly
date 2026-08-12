using Xeptions;

namespace Ringly.Asterisk.Models.Exceptions;

public class TransferHandlingNotEnabledException : Xeption
{
    public TransferHandlingNotEnabledException()
        : base("Endpoint is missing PJSIP_TRANSFER_HANDLING()=ari-only configuration.")
    { }
}
