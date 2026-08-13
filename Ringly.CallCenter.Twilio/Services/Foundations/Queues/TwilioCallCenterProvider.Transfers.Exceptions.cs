using Ringly.CallCenter.Twilio.Models.Foundations.Transfers.Exceptions;
using Xeptions;

namespace Ringly.CallCenter.Twilio.Services.Foundations.Queues;

public partial class TwilioCallCenterProvider
{
    private delegate ValueTask ReturningValueTaskFunction();

    private async ValueTask TryCatchTransfer(ReturningValueTaskFunction returningValueTaskFunction)
    {
        try
        {
            await returningValueTaskFunction();
        }
        catch (InvalidTransferProgressRequestException invalidTransferProgressRequestException)
        {
            throw await CreateAndLogTransferValidationException(invalidTransferProgressRequestException);
        }
        catch (Exception exception)
        {
            var failedTransferServiceException = new FailedTransferServiceException(exception);
            throw await CreateAndLogTransferServiceException(failedTransferServiceException);
        }
    }

    private async ValueTask<TransferValidationException> CreateAndLogTransferValidationException(Xeption exception)
    {
        var transferValidationException = new TransferValidationException(exception);
        await this.loggingBroker.LogErrorAsync(transferValidationException);

        return transferValidationException;
    }

    private async ValueTask<TransferServiceException> CreateAndLogTransferServiceException(Xeption exception)
    {
        var transferServiceException = new TransferServiceException(exception);
        await this.loggingBroker.LogErrorAsync(transferServiceException);

        return transferServiceException;
    }
}
