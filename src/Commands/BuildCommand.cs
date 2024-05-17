using System.CommandLine;

namespace Flamenco.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base(name: "build", description: "Builds various packaging related targets.")
    {
        var sourceDirectoryOption = new Option<DirectoryInfo>(
            name: "--source-directory",
            description: "The directory that the build tool uses to produce its targets.",
            getDefaultValue:  GetDirectoryFromEnvironmentOrDefault(
                EnvironmentVariableNames.DestinationDirectory, 
                defaultPath: "src"));
        
        var destinationDirectoryOption = new Option<DirectoryInfo>(
            name: "--destination-directory",
            description: "The directory where the targets are build.",
            getDefaultValue: GetDirectoryFromEnvironmentOrDefault(
                EnvironmentVariableNames.DestinationDirectory, 
                defaultPath: "dist"));

        AddGlobalOption(sourceDirectoryOption);
        AddGlobalOption(destinationDirectoryOption);
        AddCommand(new BuildDebianTarballCommand());
    }
    
    private Func<DirectoryInfo> GetDirectoryFromEnvironmentOrDefault(string environmentVariableName, string defaultPath)
    {
        return () =>
        {
            string path = Environment.GetEnvironmentVariable(environmentVariableName) ?? defaultPath;
            return new DirectoryInfo(path);
        };
    }
}