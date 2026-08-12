using Xeptions;

namespace Ringly.Asterisk.Models.Exceptions;

public class ChannelCurrentlyRecordingException : Xeption
{
    public ChannelCurrentlyRecordingException()
        : base("Channel is currently being recorded.")
    { }
}
