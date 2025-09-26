using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Flamenco.Packaging;

namespace Flamenco.Console.Commands;

public class PackCommand : Command
{
    private static readonly Option<TarballCompressionMethod> DebianTarballCompressionMethodOption = new (
        name: "--debian-tarball-compression-method",
        description: "The compression method used to create debian tar archives. [default: xz]")
    {
        Arity = ArgumentArity.ExactlyOne,
    };
    
    private static readonly Option<bool> DebianTreeOnlyOption = new (
        name: "--debian-tree-only",
        description: "Only generate the debian directory within the source tree without extracting the " +
                     ".orig tarball or invoking dpkg-buildpackage. [default: false]",
        getDefaultValue: () => false)
    {
        Arity = ArgumentArity.ZeroOrOne,
    };
    
    private static readonly Option<bool> SourceTreeOnlyOption = new (
        name: "--source-tree-only",
        description: "Only generate the complete debian source directory without " +
                     "invoking dpkg-buildpackage. [default: false]",
        getDefaultValue: () => false)
    {
        Arity = ArgumentArity.ZeroOrOne,
    };
    
    private static readonly Option<bool> ExcludeOrigTarballOption = new (
        name: "--exclude-orig-tarball",
        description: "Exclude the .orig tarball from the generated .changes file. [default: false]",
        getDefaultValue: () => false)
    {
        Arity = ArgumentArity.ZeroOrOne,
    };
    
    public PackCommand() : base(
        name: "pack", 
        description: "Packages the debian tarball(s) for specific packages and series from a provided source directory.")
    {
        var targetArguments = new Argument<string[]>(
            name: "targets", 
            description: "The packaging targets that should be produced. A packaging target is in the " +
                         "format 'PACKAGE:SERIES' (e.g. 'dotnet8:noble'). If no packaging target " +
                         "is specified all packageable targets in the source directory will be selected.")
        {
            Arity = ArgumentArity.ZeroOrMore,
        };

        AddArgument(targetArguments);
        AddOption(CommonOptions.SourceDirectoryOption);
        AddOption(CommonOptions.DestinationDirectoryOption);
        AddOption(DebianTarballCompressionMethodOption);
        AddOption(DebianTreeOnlyOption);
        AddOption(SourceTreeOnlyOption);
        AddOption(ExcludeOrigTarballOption);
        Handler = CommandHandler.Create(Run);
    }
    
    private static async Task<int> Run(
        string[] targets, 
        DirectoryInfo? sourceDirectory,
        DirectoryInfo? destinationDirectory,
        TarballCompressionMethod? debianTarballCompressionMethod,
        bool debianTreeOnly,
        bool sourceTreeOnly,
        bool excludeOrigTarball,
        CancellationToken cancellationToken = default)
    {
        if (!EnvironmentVariables.TryGetSourceDirectoryInfoFromEnvironmentOrDefaultIfNull(ref sourceDirectory) ||
            !EnvironmentVariables.TryGetDestinationDirectoryInfoFromEnvironmentOrDefaultIfNull(ref destinationDirectory) ||
            !EnvironmentVariables.TryGetDebianTarballCompressionMethodFromEnvironmentOrDefaultIfNull(ref debianTarballCompressionMethod)) 
        {
            return -1;
        }

        if (!TryGetBuildOutput(debianTreeOnly, sourceTreeOnly, excludeOrigTarball, out BuildOutput buildOutput))
        {
            Log.Fatal($"The combination of the {DebianTreeOnlyOption.Name}, {SourceTreeOnlyOption.Name}" +
                      $" and {ExcludeOrigTarballOption.Name} flags is invalid.");
            return -1;
        }
            
        Log.Debug("Source Directory: " + sourceDirectory.FullName);
        Log.Debug("Destination Directory: " + destinationDirectory.FullName);
        Log.Debug("Build Output: " + buildOutput.ToString());
        
#if SNAPCRAFT
        if (!await Program.IsPathAccessibleAsync(sourceDirectory.FullName, cancellationToken) || 
            !await Program.IsPathAccessibleAsync(destinationDirectory.FullName, cancellationToken))
        {
            Log.Fatal("Aborting the packaging process, because some paths are not accessible.");
            return -1;
        }
#endif
        var sourceDirectoryInfoResult = SourceDirectoryInfo.FromDirectory(sourceDirectory);
        Log.Annotations(sourceDirectoryInfoResult);
        if (sourceDirectoryInfoResult.IsFailure)
        {
            Log.Fatal("Aborting the packaging process, because the source directory contains errors.");
            return -1;
        }

        var sourceDirectoryInfo = sourceDirectoryInfoResult.Value;
        
        Log.Debug($"Discovered targets in source directory: {sourceDirectoryInfo.BuildableTargets}");
        
        var buildTargets = ParseBuildTargets(targets);
        if (buildTargets is null)
        {
            Log.Fatal("Aborting the packaging process, because the requested packaging targets contains errors.");
            return 1;
        }
        Log.Debug($"Requested packaging targets: {buildTargets}");

        if (buildTargets.Count == 0)
        {
            Log.Info("Packaging all packageable targets.");
            buildTargets = sourceDirectoryInfo.BuildableTargets;
        }
        else if (!BuildTargetsAreSubSetOfDefinedTargets())
        {
            Log.Fatal("Aborting the packaging process, because some packaging targets are not defined in the source directory.");
            return 1;
        }

        int failedBuilds = 0;

        if (!destinationDirectory.Exists)
        {
            destinationDirectory.Create();
        }

        var packagingTasks = new List<Task>();
        foreach (var buildTarget in buildTargets)
        {
            var debianTarballBuilder = new DebianSourcePackageBuilder()
            {
                SourceDirectory = sourceDirectoryInfo,
                DestinationDirectory = destinationDirectory,
                TarballCompressionMethod = debianTarballCompressionMethod.Value,
                BuildTarget = buildTarget,
                TarArchivingServiceProvider = new TarSystemCommand(),
                BuildOutput = buildOutput,
            };

            packagingTasks.Add(debianTarballBuilder
                .BuildDebianSourcePackageAsync(cancellationToken)
                .AsTask().ContinueWith((task) =>
                {
                    var result = task.Result;
                    
                    Log.Annotations(result);
                    if (result.IsSuccess)
                    {
                        Log.Info($"Packaging target {debianTarballBuilder.BuildTarget} succeeded.");
                    }
                    else
                    {
                        Interlocked.Increment(ref failedBuilds);
                        Log.Error($"Packaging target {debianTarballBuilder.BuildTarget} failed.");
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion));
        }

        await Task.WhenAll(packagingTasks);

        Log.Info($"Total Packages: {buildTargets.Count}; Succeeded: {buildTargets.Count - failedBuilds}; Failed: {failedBuilds}");
        
        if (failedBuilds > 0)
        {
            Log.Fatal("Packaging one or more packaging targets failed.");
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
                    Log.Error($"Packaging target '{target}' is not defined in the source directory.");
                    allBuildTargetsAreDefined = false;
                }
            }

            return allBuildTargetsAreDefined;
        }
    }

    private static bool TryGetBuildOutput(bool debianTreeOnly, bool sourceTreeOnly, bool excludeOrigTarball, out BuildOutput buildOutput)
    {   
        switch ((debianTreeOnly, sourceTreeOnly, excludeOrigTarball))
        {
            case (true, false, false):
                buildOutput = BuildOutput.DebianDirectoryOnly;
                return true;
            case (false, true, false):
                buildOutput = BuildOutput.DebianSourceTreeOnly;
                return true;
            case (false, false, false):
                buildOutput = BuildOutput.SourcePackageIncludingOrigTarball;
                return true;
            case (false, false, true): 
                buildOutput = BuildOutput.SourcePackageExcludingOrigTarball;
                return true;
            default:
                buildOutput = default;
                return false;
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
                Log.Error($"The packaging target '{target}' does not follow the format 'PACKAGE:SERIES'!");
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