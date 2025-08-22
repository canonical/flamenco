using System.CommandLine;
using System.CommandLine.Invocation;
using Flamenco.Sandbox.Services;

namespace Flamenco.Sandbox.Commands;

public class GreetingCommand : CommandBase<GreetingCommandHandler>
{
    public GreetingCommand(LazyCommandHandler<GreetingCommandHandler> commandHandler) : base(name: "greeting", commandHandler, description: "Greeting someone")
    {
    }
}