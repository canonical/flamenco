using System.Diagnostics.CodeAnalysis;

namespace Flamenco.Commands;

public static class EnvironmentVariables
{
    public const string SourceDirectory = "FLAMENCO_SOURCE_DIRECTORY";
    public const string DestinationDirectory = "FLAMENCO_DESTINATION_DIRECTORY";

    public static bool TryGetSourceDirectoryInfoFromEnvironmentOrDefaultIfNull(
        [NotNullWhen(returnValue: true)] ref DirectoryInfo? directoryInfo) =>
        TryGetDirectoryInfoFromEnvironmentOrDefaultIfNull(
            environmentVariableName: SourceDirectory,
            defaultPath: "src",
            ref directoryInfo);
    
    public static bool TryGetDestinationDirectoryInfoFromEnvironmentOrDefaultIfNull(
        [NotNullWhen(returnValue: true)] ref DirectoryInfo? directoryInfo) =>
        TryGetDirectoryInfoFromEnvironmentOrDefaultIfNull(
            environmentVariableName: DestinationDirectory,
            defaultPath: "dist",
            ref directoryInfo);
    
    private static bool TryGetDirectoryInfoFromEnvironmentOrDefaultIfNull(
        string environmentVariableName, 
        string defaultPath,
        [NotNullWhen(returnValue: true)] ref DirectoryInfo? directoryInfo)
    {
        string? value = GetEnvironmentVariable(environmentVariableName);

        if (directoryInfo is not null)
        {
            if (value is not null)
            {
                Log.Warning(message: $"Environment variable '{environmentVariableName}' will be ignored, because a " +
                                     "value is provided via command line parameter.");
            }

            return true;
        }

        value ??= defaultPath;
        
        try
        {
            directoryInfo = new DirectoryInfo(value);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(message: $"Failed to parse the path '{value}' provided by environment variable " +
                               $"'{environmentVariableName}'");
            Log.Debug(exception.Message);
            
            directoryInfo = null;
            return false;
        }
    }
    
    private static string? GetEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (value is not null) Log.Info(message: $"Environment variable '{name}' is set to value '{value}'.");
        
        return value;
    }
}