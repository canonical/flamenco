using System.CommandLine;

namespace Flamenco.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base(name: "build", description: "Builds various packaging related targets.")
    {
        AddCommand(new BuildDebianTarballCommand());
    }
}