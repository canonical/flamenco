using System.CommandLine;

namespace Flamenco.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base(name: "build", description: "Builds various packaging related targets.")
    {
        var sourceDirectoryOption = new Option<DirectoryInfo>(
            name: "--source-directory",
            description: "The directory that the build tool uses to produce its targets.",
            getDefaultValue:  () => new DirectoryInfo("src"));
        
        var destinationDirectoryOption = new Option<DirectoryInfo>(
            name: "--destination-directory",
            description: "The directory where the targets are build.",
            getDefaultValue: () => new DirectoryInfo("dist"));

        AddGlobalOption(sourceDirectoryOption);
        AddGlobalOption(destinationDirectoryOption);
        AddCommand(new BuildDebianTarballCommand());
    }
}