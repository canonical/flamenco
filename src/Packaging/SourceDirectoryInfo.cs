using System.Security;
using Flamenco.Packaging.Dpkg;

namespace Flamenco.Packaging;

public class SourceDirectoryInfo
{
    public static SourceDirectoryInfo? FromDirectory(DirectoryInfo sourceDirectory)
    {
        if (!sourceDirectory.Exists)
        {
            Log.Error($"The source directory '{sourceDirectory}' does not exist!");
            return null;
        }

        var targetCollection = DiscoverTargets(sourceDirectory);

        if (targetCollection is null)
        {
            return null;
        }
        
        return new SourceDirectoryInfo(sourceDirectory, targetCollection);
    }
    
    private static BuildTargetCollection? DiscoverTargets(DirectoryInfo sourceDirectory)
    {
        bool errorDetected = false;
        BuildTargetCollection targetCollection = new BuildTargetCollection();
        
        foreach (var changelogFile in sourceDirectory.EnumerateFiles("changelog*"))
        {
            var extensions = changelogFile.Name.Split('.')[1..];

            if (extensions.Length != 2)
            {
                Log.Error($"The changelog file '{changelogFile}' does not follow the format 'changelog.PACKAGE.SERIES'!");
                errorDetected = true;
                continue;
            }
            
            targetCollection.Add(new BuildTarget(PackageName: extensions[0], SeriesName: extensions[1]));
        }

        // we want to fail only after checking the format of all changelog files 
        if (errorDetected) return null;

        return targetCollection;
    }
    
    private SourceDirectoryInfo(
        DirectoryInfo directoryInfo,
        BuildTargetCollection buildableTargets)
    {
        DirectoryInfo = directoryInfo;
        BuildableTargets = buildableTargets;
    }
    
    public DirectoryInfo DirectoryInfo { get; }
    
    public BuildTargetCollection BuildableTargets { get; }

    public DpkgChangelogReader? ReadChangelog(BuildTarget buildTarget)
    {
        var path = Path.Combine(
            DirectoryInfo.FullName,
            $"changelog.{buildTarget.PackageName}.{buildTarget.SeriesName}");

        try
        {
            return DpkgChangelogReader.FromFile(path);
        }
        catch (Exception exception)
        {
            switch (exception)
            {
                case FileNotFoundException or DirectoryNotFoundException:
                    Log.Error(message: $"Could not find file '{path}'.");
                    break;
                case UnauthorizedAccessException or SecurityException:
                    Log.Error(message: $"Permissions denied to access file '{path}'.");
                    Log.Debug(exception.Message);
                    break;
                case IOException:
                    Log.Error(message: $"Permissions denied to access file '{path}'.");
                    Log.Debug(exception.Message);
                    break;
                default:
                    Log.Error(message: "An unexpected error occured.");
                    Log.Debug(message: $"{exception.GetType().FullName}: {exception.Message}");
                    break;
            }

            return null;
        }
    }
}