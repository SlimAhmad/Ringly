using Ringly.Twilio.Models;

namespace Ringly.Twilio.Brokers;

public partial class TwilioBroker
{
    private const string TaskQueuesRelativeUrlFormat = "{0}/Workspaces/{1}/TaskQueues";

    // POST /v1/Workspaces/{WorkspaceSid}/TaskQueues — confirmed against Twilio's TaskQueue
    // resource docs. TaskRouter is served from a different host than Calls/Conferences
    // (TwilioOptions.TaskRouterBaseUrl), so the full absolute URL is passed through rather than
    // a path relative to this.httpClient.BaseAddress — an absolute URI passed to Uri combination
    // overrides the base, so this still goes out on the same authenticated httpClient.
    public async ValueTask<TwilioTaskQueue> InsertTaskQueueAsync(string friendlyName)
    {
        TwilioTaskQueueResponse response = await this.PostFormAsync<TwilioTaskQueueResponse>(
            string.Format(TaskQueuesRelativeUrlFormat, this.taskRouterBaseUrl, this.workspaceSid),
            [new("FriendlyName", friendlyName)]);

        return new TwilioTaskQueue
        {
            Sid = response.Sid,
            FriendlyName = response.FriendlyName
        };
    }
}
