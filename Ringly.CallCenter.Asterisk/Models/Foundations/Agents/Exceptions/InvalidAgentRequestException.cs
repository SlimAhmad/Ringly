using Xeptions;

namespace Ringly.CallCenter.Asterisk.Models.Foundations.Agents.Exceptions;

public class InvalidAgentRequestException : Xeption
{
    public InvalidAgentRequestException()
        : base("Agent request is invalid.")
    { }
}
