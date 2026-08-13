using Xeptions;

namespace Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

public class TrunkSpendLimitExceededException : Xeption
{
    public TrunkSpendLimitExceededException(string trunkName)
        : base($"Trunk {trunkName} has exceeded its configured spend or concurrency limit.")
    { }
}
