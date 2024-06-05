using System.Diagnostics.CodeAnalysis;
using Flamenco.Packaging;

namespace Flamenco.Commands;

public static class EnvironmentVariables
{
    public const string SourceDirectory = "FLAMENCO_SOURCE_DIRECTORY";
    public const string DestinationDirectory = "FLAMENCO_DESTINATION_DIRECTORY";
    public const string DebianTarballCompressionMethod = "FLAMENCO_DEBIAN_TARBALL_COMPRESSION_METHOD";

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

    public static bool TryGetDebianTarballCompressionMethodFromEnvironmentOrDefaultIfNull(
        [NotNullWhen(returnValue: true)] ref Tarball.CompressionMethod? compressionMethod) =>
        TryGetTarballCompressionMethodFromEnvironmentOrDefaultIfNull(
            environmentVariableName: DebianTarballCompressionMethod,
            ref compressionMethod);
    
    private static bool TryGetTarballCompressionMethodFromEnvironmentOrDefaultIfNull(
        string environmentVariableName, 
        [NotNullWhen(returnValue: true)] ref Tarball.CompressionMethod? compressionMethod)
    {
        var value = GetEnvironmentVariable(environmentVariableName);

        if (compressionMethod.HasValue)
        {
            if (value is not null)
            {
                Log.Warning(message: $"Environment variable '{environmentVariableName}' will be ignored, because a " +
                                     "value is provided via command line parameter.");
            }

            return true;
        }

        if (value is null || value.Equals("xz", StringComparison.OrdinalIgnoreCase))
        {
            compressionMethod = Tarball.CompressionMethod.XZ;
            return true;
        }

        if (value.Equals("gzip", StringComparison.OrdinalIgnoreCase))
        {
            compressionMethod = Tarball.CompressionMethod.GZip;
            return true;
        }

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            compressionMethod = Tarball.CompressionMethod.None;
            return true;
        }

        Log.Error(message: $"Unknown compression method '{value}' " +
                           $"(source: environment variable '{environmentVariableName}'). " +
                           "See the help information for possible value.");
        return false;
    }
    
    private static string? GetEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (value is not null) Log.Info(message: $"Environment variable '{name}' is set to value '{value}'.");
        
        return value;
    }
}