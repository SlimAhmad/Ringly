using Ringly.Twilio.Models;

namespace Ringly.Twilio.Brokers;

public partial interface ITwilioBroker
{
    ValueTask<TwilioTaskQueue> InsertTaskQueueAsync(string friendlyName);
}
