using System.CommandLine;

namespace Flamenco.Commands;

public static class CommonOptions
{
    public static readonly Option<DirectoryInfo> SourceDirectoryOption = new (
        name: "--source-directory",
        description: "The directory that the build tool uses to produce its targets.",
        getDefaultValue:  GetDirectoryFromEnvironmentOrDefault(
            environmentVariableName: "FLAMENCO_SOURCE_DIRECTORY", 
            defaultPath: "src"));
    
    public static readonly Option<DirectoryInfo> DestinationDirectoryOption = new (
            name: "--destination-directory",
            description: "The directory where the targets are build.",
            getDefaultValue: GetDirectoryFromEnvironmentOrDefault(
                environmentVariableName: "FLAMENCO_DESTINATION_DIRECTORY", 
                defaultPath: "dist"));
    
    private static Func<DirectoryInfo> GetDirectoryFromEnvironmentOrDefault(
        string environmentVariableName, 
        string defaultPath)
    {
        return () =>
        {
            string path = Environment.GetEnvironmentVariable(environmentVariableName) ?? defaultPath;
            return new DirectoryInfo(path);
        };
    }
}