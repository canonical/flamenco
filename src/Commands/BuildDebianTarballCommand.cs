using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Flamenco.Commands;

public class BuildDebianTarballCommand : Command
{
    public BuildDebianTarballCommand() : base(name: "debian-tarball", description: "")
    {
        var targetArguments = new Argument<string[]>(name: "targets", description: "")
        {
            Arity = ArgumentArity.ZeroOrMore,
        };

        AddArgument(targetArguments);
        Handler = CommandHandler.Create(Run);
    }
    
    private static async Task<int> Run(
        string[] targets, 
        DirectoryInfo sourceDirectory,
        DirectoryInfo destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        Log.Debug("Source Directory: " + sourceDirectory.FullName);
        Log.Debug("Destination Directory: " + destinationDirectory.FullName);
        
        var sourceDirectoryInfo = SourceDirectoryInfo.FromDirectory(sourceDirectory);
        if (sourceDirectoryInfo is null)
        {
            Log.Fatal("Aborting the build process, because the source directory contains errors.");
            return -1;
        }
        Log.Debug($"Discovered targets in source directory: {sourceDirectoryInfo.BuildableTargets}");
        
        var buildTargets = ParseBuildTargets(targets);
        if (buildTargets is null)
        {
            Log.Fatal("Aborting the build process, because the requested build targets contains errors.");
            return 1;
        }
        Log.Debug($"Requested build targets: {buildTargets}");

        if (buildTargets.Count == 0)
        {
            Log.Info("Building all buildable targets.");
            buildTargets = sourceDirectoryInfo.BuildableTargets;
        }
        else if (!BuildTargetsAreSubSetOfDefinedTargets())
        {
            Log.Fatal("Aborting the build process, because some build targets are not defined in the source directory.");
            return 1;
        }

        var debianTarballBuilder = new DebianTarballBuilder()
        {
            SourceDirectory = sourceDirectoryInfo,
            DestinationDirectory = null!,
            BuildTarget = null!
        };

        int failedBuilds = 0;

        if (!destinationDirectory.Exists)
        {
            destinationDirectory.Create();
        }
        
        foreach (var buildTarget in buildTargets)
        {
            debianTarballBuilder.BuildTarget = buildTarget;
            debianTarballBuilder.DestinationDirectory = new DirectoryInfo(
                Path.Combine(
                    destinationDirectory.FullName,
                    $"{buildTarget.PackageName}-{buildTarget.SeriesName}"));
            
            if (await debianTarballBuilder.BuildDebianDirectoryAsync(cancellationToken))
            {
                Log.Info($"Building target {buildTarget} succeeded.");    
            }
            else
            {
                ++failedBuilds;
                Log.Error($"Building target {buildTarget} failed.");
            }
        }

        Log.Info($"Total Builds: {buildTargets.Count}; Succeeded: {buildTargets.Count - failedBuilds}; Failed: {failedBuilds}");
        
        if (failedBuilds > 0)
        {
            Log.Fatal("Building one or more build targets failed.");
            return 1;
        }
        
        return 0;

        bool BuildTargetsAreSubSetOfDefinedTargets()
        {
            bool allBuildTargetsAreDefined = true;
            
            foreach (var target in buildTargets)
            {
                if (!sourceDirectoryInfo.BuildableTargets.Contains(target))
                {
                    Log.Error($"Build target '{target}' is not defined in the source directory.");
                    allBuildTargetsAreDefined = false;
                }
            }

            return allBuildTargetsAreDefined;
        }
    }

    private static BuildTargetCollection? ParseBuildTargets(string[] targets)
    {
        var targetCollection = new BuildTargetCollection();

        bool errorDetected = false;
        
        foreach (var target in targets)
        {
            var targetComponents = target.Split(':');

            if (targetComponents.Length != 2)
            {
                Log.Error($"The build target '{target}' does not follow the format 'PACKAGE:SERIES'!");
                errorDetected = true;
                continue;
            }
            
            targetCollection.Add(new BuildTarget(PackageName: targetComponents[0], SeriesName: targetComponents[1]));
        }
        
        // we want to fail only after checking the format of all changelog files 
        if (errorDetected) return null;
        
        return targetCollection;
    }
}