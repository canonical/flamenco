using System.Collections.Immutable;
using System.Text;

namespace Flamenco;

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
}