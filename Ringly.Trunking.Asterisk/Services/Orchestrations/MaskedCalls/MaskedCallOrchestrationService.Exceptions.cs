using Ringly.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Orchestrations.MaskedCalls.Exceptions;
using Xeptions;

namespace Ringly.Trunking.Asterisk.Services.Orchestrations.MaskedCalls;

public partial class MaskedCallOrchestrationService
{
    private delegate ValueTask<CallSession> ReturningCallSessionFunction();

    private async ValueTask<CallSession> TryCatch(ReturningCallSessionFunction returningCallSessionFunction)
    {
        try
        {
            return await returningCallSessionFunction();
        }
        catch (InvalidMaskedCallRequestException invalidMaskedCallRequestException)
        {
            throw await CreateAndLogValidationException(invalidMaskedCallRequestException);
        }
        catch (MaskingSessionNotFoundException maskingSessionNotFoundException)
        {
            throw await CreateAndLogValidationException(maskingSessionNotFoundException);
        }
        catch (Exception exception)
        {
            var failedMaskedCallDependencyException = new FailedMaskedCallDependencyException(exception);
            throw await CreateAndLogDependencyException(failedMaskedCallDependencyException);
        }
    }

    private async ValueTask<MaskedCallValidationException> CreateAndLogValidationException(Xeption exception)
    {
        var maskedCallValidationException = new MaskedCallValidationException(exception);
        await this.loggingBroker.LogErrorAsync(maskedCallValidationException);

        return maskedCallValidationException;
    }

    private async ValueTask<MaskedCallDependencyException> CreateAndLogDependencyException(Xeption exception)
    {
        var maskedCallDependencyException = new MaskedCallDependencyException(exception);
        await this.loggingBroker.LogErrorAsync(maskedCallDependencyException);

        return maskedCallDependencyException;
    }
}
