using Ringly.CallCenter.Abstractions;
using Ringly.CallCenter.Abstractions.Models;
using Ringly.Twilio.Brokers;
using Ringly.Twilio.Models;

namespace Ringly.CallCenter.Twilio.Services.Foundations.Queues;

// §9's "same contract, different internals" — the Twilio counterpart to
// Ringly.CallCenter.Asterisk.AsteriskCallCenterFoundationService. Asterisk's "Queue" is a
// holding-bridge lifecycle primitive; TaskRouter's equivalent is a TaskQueue under a Workspace
// (§9 comparison table: "TaskRouter — Workflow assigns the Task to one Worker via a
// Reservation"). Creating a TaskQueue by friendly name is the row's whole "Workspace/Workflow/
// Queue wrapper" scope — Workflow assignment is a TaskRouter-side config, not something this
// provider needs to drive.
public partial class TwilioCallCenterProvider : ICallCenterProvider
{
    private readonly ITwilioBroker twilioBroker;
    private readonly ILoggingBroker loggingBroker;

    public TwilioCallCenterProvider(
        ITwilioBroker twilioBroker,
        ILoggingBroker loggingBroker)
    {
        this.twilioBroker = twilioBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<HoldingBridge> CreateQueueAsync(QueueConfig config) =>
    TryCatch(async () =>
    {
        ValidateQueueConfig(config);

        TwilioTaskQueue taskQueue = await this.twilioBroker.InsertTaskQueueAsync(config.Name);

        return new HoldingBridge
        {
            BridgeId = taskQueue.Sid,
            QueueName = taskQueue.FriendlyName
        };
    });
}
