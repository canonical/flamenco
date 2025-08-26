// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Flamenco.Packaging.Dpkg;

namespace Flamenco.Packaging;

public class DebianSourcePackageBuilder
{
    public required BuildTarget BuildTarget { get; set; }
    
    public required SourceDirectoryInfo SourceDirectory { get; set; }
    
    public required DirectoryInfo DestinationDirectory { get; set; }
    
    public required TarballCompressionMethod TarballCompressionMethod { get; set; }
    
    public required ITarArchivingServiceProvider TarArchivingServiceProvider { get; set; }
    
    public async ValueTask<Result> BuildDebianSourcePackageAsync(CancellationToken cancellationToken = default)
    {
        var result = new Result();
        var readFirstChangelogEntryResult = 
            await SourceDirectory.ReadFirstChangelogEntryAsync(BuildTarget, cancellationToken).ConfigureAwait(false);
        result = result.Merge(readFirstChangelogEntryResult);
        if (readFirstChangelogEntryResult.IsFailure) return result;
        var changelogEntry = readFirstChangelogEntryResult.Value;
            
        if (changelogEntry.PackageName.Identifier != BuildTarget.PackageName)
        {
            result = result.WithAnnotation(new ChangelogFileNameAndContentMismatch(
                message: "Package name in first changelog entry does not match with the " +
                         $"build target {BuildTarget} implied by the file name",
                changelogEntry.Location));
        }

        switch (changelogEntry.Distributions)
        {
            case []:
                result = result.WithAnnotation(new ChangelogFileNameAndContentMismatch(
                    message: $"Latest changelog entry for build target {BuildTarget} " +
                             "does not define a target series",
                    changelogEntry.Location));
                break;
            case [{} series]:
                if (series.Series.Identifier != BuildTarget.SeriesName)
                {
                    result = result.WithAnnotation(new ChangelogFileNameAndContentMismatch(
                        message: $"Latest changelog entry for build target {BuildTarget} specifies a different " +
                                 "target series",
                        changelogEntry.Location));
                }
                break;
            default:
                result = result.WithAnnotation(new MultipleTargetDistributionsSpecified(
                    buildTarget: BuildTarget,
                    distributions: changelogEntry.Distributions,
                    location: changelogEntry.Location));
                
                if (!changelogEntry.Distributions.Any(series => series.Series.Identifier == BuildTarget.SeriesName))
                {
                    result = result.WithAnnotation(new ChangelogFileNameAndContentMismatch(
                        message: $"Latest changelog entry for build target {BuildTarget} does not specify " +
                                 $"target series {BuildTarget.SeriesName}",
                        changelogEntry.Location));
                }
                break;
        }
        
        var destinationDebianDirectory = new DirectoryInfo(Path.Combine(
            DestinationDirectory.FullName,
            $"{BuildTarget.PackageName}-{changelogEntry.Version}", 
            "debian"));

        var origTarball = new FileInfo(Path.Combine(
            DestinationDirectory.FullName,
            $"{changelogEntry.PackageName}_{changelogEntry.Version.UpstreamVersion}" +
            $".orig.tar{TarballCompressionMethod.FileExtension()}"));

        bool debianDirectoryOnly = false;
        if (!debianDirectoryOnly)
        {
            if (!origTarball.Exists)
            {
                return result.WithAnnotation(new OrigTarballNotFound(
                    BuildTarget, new Location { ResourceLocator = origTarball.FullName }));
            }
        }

        result = await result
            .Then(() => RecursivelyCopyMatchingFilesAsync(
                sourceDirectory: SourceDirectory.DirectoryInfo,
                destinationDirectory: destinationDebianDirectory,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (debianDirectoryOnly) return result;

        return await result
            /* .Bind(() => TarArchivingServiceProvider.CreateTarArchiveAsync(
                archiveFile: new FileInfo(fileName: Path.Combine(
                    DestinationDirectory.FullName,
                    $"{BuildTarget.PackageName}_{changelogEntry.Version}" +
                    $".debian.tar{TarballCompressionMethod.FileExtension()}")),
                archiveRoot: destinationDirectory
                    .Parent!, // the steps before should ensure that the parent directory exists
                includedPaths: new[] { "debian" },
                TarballCompressionMethod,
                cancellationToken)) */
            .Then(() => TarArchivingServiceProvider.ExtractTarArchiveAsync(
                archiveFile: origTarball,
                targetDirectory: destinationDebianDirectory.Parent!,
                stripComponents: 1,
                cancellationToken))
            .Then(() => RunDpkgBuildPackageAsync(
                sourceTreeDirectory: destinationDebianDirectory.Parent!,
                buildTarget: BuildTarget,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    private static async Task<Result> RunDpkgBuildPackageAsync(
        DirectoryInfo sourceTreeDirectory, 
        BuildTarget buildTarget, 
        CancellationToken cancellationToken)
    {
        try
        {
            var dpkgBuildPackage = new Process()
            {
                StartInfo = new ProcessStartInfo(
                    fileName: "/usr/bin/env",
                    arguments: [
                        "dpkg-buildpackage", 
                        "--build=source",
                        "--no-pre-clean", 
                        "--no-check-builddeps",
                        "-sa",
                    ])
                {
                    WorkingDirectory = sourceTreeDirectory.FullName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
#if SNAPCRAFT
            string snapRoot = Environment.GetEnvironmentVariable("SNAP") 
                              ?? throw new UnreachableException("Could not find SNAP environment variable.");
            
            // dpkg-genbuildinfo tries to search outside the snap confinement by default
            dpkgBuildPackage.StartInfo.ArgumentList.Add($"--buildinfo-option=--admindir={snapRoot}/var/lib/dpkg");
            
            // fix perl applications in snap confinement, see also:
            // https://forum.snapcraft.io/t/the-perl-launch-launcher-fix-perl-applications-in-the-snap-runtime/11736
            dpkgBuildPackage.StartInfo.Environment.Add("PERLLIB", GetPerlLibraryPaths(snapRoot));
#endif
            dpkgBuildPackage.Start();
            await dpkgBuildPackage.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (dpkgBuildPackage.ExitCode != 0)
            {
                return new Result().WithAnnotation(
                    new DpkgBuildPackageFailed(
                        buildTarget: buildTarget, 
                        location: new Location { ResourceLocator = sourceTreeDirectory.FullName }, 
                        reason: $"Exit code '{dpkgBuildPackage.ExitCode}' is non zero"));
            }

            return Result.Success;
        }
        catch (Exception exception)
        {
            return new Result().WithAnnotation(
                new DpkgBuildPackageFailed(
                    reason: $"Unexpected exception '{exception.GetType().FullName}'. {exception.Message}", 
                    buildTarget: buildTarget, 
                    location: new Location { ResourceLocator = sourceTreeDirectory.FullName }, 
                    innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception))));
        }
    }

#if SNAPCRAFT
    private static string GetPerlLibraryPaths(string snapRoot)
    {
        List<string> perlLibraryPaths =  [];
        string debianMultiarchTriplet = Environment.GetEnvironmentVariable("X_DEBIAN_MULTIARCH_TRIPLET") ?? string.Empty;
        
        string[] staticSearchPaths = [
                $"{snapRoot}/usr/lib/{debianMultiarchTriplet}/perl-base",
                $"{snapRoot}/usr/share/perl5",
                $"{snapRoot}/etc/perl",
                $"{snapRoot}/usr/local/lib/site_perl",
            ];
        perlLibraryPaths.AddRange(
            staticSearchPaths
            .Where(Directory.Exists));
        
        string[] dynamicSearchPaths = [
            $"{snapRoot}/usr/lib/{debianMultiarchTriplet}/perl",
            $"{snapRoot}/usr/lib/{debianMultiarchTriplet}/perl5",
            $"{snapRoot}/usr/share/perl",
            $"{snapRoot}/usr/local/lib/{debianMultiarchTriplet}/perl",
            $"{snapRoot}/usr/local/share/perl",
        ];
        perlLibraryPaths.AddRange(
            dynamicSearchPaths
            .Where(Directory.Exists)
            .SelectMany(Directory.GetDirectories));

        return string.Join(':', perlLibraryPaths);
    }
#endif
    
    private async Task<Result> RecursivelyCopyMatchingFilesAsync(
        DirectoryInfo sourceDirectory,
        DirectoryInfo destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        return await CreateDestinationDirectory(destinationDirectory, cancellationToken)
            .Then(() => IndexSourceFiles(sourceDirectory, cancellationToken))
            .Then(sourceFileIndex => CopySourceFilesToDestinationDirectoryAsync(
                sourceDirectory,
                sourceFileIndex,
                destinationDirectory,
                cancellationToken))
            .Then(() =>
            {
                var copyTasks = new List<Task<Result>>();
                foreach (var sourceSubDirectory in sourceDirectory.EnumerateDirectories())
                {
                    var destinationSubDirectoryPath =
                        Path.Combine(destinationDirectory.FullName, sourceSubDirectory.Name);

                    copyTasks.Add(RecursivelyCopyMatchingFilesAsync(
                        sourceDirectory: sourceSubDirectory,
                        destinationDirectory: new DirectoryInfo(destinationSubDirectoryPath),
                        cancellationToken));
                }

                return Result.Success.MergeWhenAll(copyTasks);
            })
            .ConfigureAwait(false);
    }
    
    private Result CreateDestinationDirectory(DirectoryInfo destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return new OperationCanceled();
        
        if (destinationDirectory.Exists)
        {
            Result result = new OutputDirectoryAlreadyExists(destinationDirectory);
            
            if (destinationDirectory.EnumerateFiles().Any() || destinationDirectory.EnumerateDirectories().Any())
            {
                result = result.WithAnnotation(new OutputDirectoryContainsFiles(destinationDirectory));
            }

            return result;
        }
        
        try
        {
            destinationDirectory.Create();
            return Result.Success;
        }
        catch (Exception exception)
        {
            return new CreatingOutputDirectoryFailed(destinationDirectory, exception);
        }
    }
    
    private Result<ImmutableDictionary<string, FileInfo>> IndexSourceFiles(
        DirectoryInfo sourceDirectory, 
        CancellationToken cancellationToken = default)
    {
        var result = new Result();
        var destinationFiles = new Dictionary<string, (FileInfo Info, int BuildTargetSpecificity)>();

        foreach (var file in sourceDirectory.EnumerateFiles())
        {
            if (cancellationToken.IsCancellationRequested) return result.WithAnnotation(new OperationCanceled());

            result = result.Merge(
                AnalyzeFileExtension(file)
                .Then(fileContext =>
                {
                    // build target is mutually exclusive; nothing to do
                    if (fileContext.BuildTargetSpecificity < 0) return Result.Success;

                    if (destinationFiles.TryGetValue(fileContext.FileName, out var otherFile))
                    {
                        if (fileContext.BuildTargetSpecificity > otherFile.BuildTargetSpecificity)
                        {
                            destinationFiles[fileContext.FileName] = (file, fileContext.BuildTargetSpecificity);
                        }
                        else if (fileContext.BuildTargetSpecificity == otherFile.BuildTargetSpecificity)
                        {
                            return new ConflictingBuildTargetSpecificity(
                                conflictingFilePath1: file.FullName,
                                conflictingFilePath2: otherFile.Info.FullName);
                        }
                    }
                    else
                    {
                        destinationFiles[fileContext.FileName] = (file, fileContext.BuildTargetSpecificity);
                    }

                    return Result.Success;
                })
            );
        }

        return result.Then<ImmutableDictionary<string, FileInfo>>(
            () => destinationFiles.ToImmutableDictionary(
                keySelector: e => e.Key,
                elementSelector: e => e.Value.Info));
    }
    
    private Result<(string FileName, int BuildTargetSpecificity)> AnalyzeFileExtension(FileInfo file)
    {
        var result = new Result();
        
        string[] fileExtensions = file.Name.Split('.');
        // Remember, that fileExtensions[0] is just the file name!

        if (fileExtensions.Length < 2) 
        {
            return result.WithValue((file.Name, BuildTargetSpecificity: 0));    
        }

        bool matchesTarget = true;
        bool packageNameIsDefined = false;
        bool seriesNameIsDefined = false;
        
        string extensionName;
        string[] extensionNames;
        int totalExtensionLength = 0;
        
        if (fileExtensions.Length >= 2)
        {
            extensionName = fileExtensions[^1];
            extensionNames = extensionName.Split(',');
            totalExtensionLength = extensionName.Length + 1;
            
            if (SourceDirectory.BuildableTargets.PackageNames.Any(extensionNames.Contains))
            {
                packageNameIsDefined = true;
                matchesTarget = extensionNames.Contains(BuildTarget.PackageName);
            }
            else if (SourceDirectory.BuildableTargets.SeriesNames.Any(extensionNames.Contains))
            {
                seriesNameIsDefined = true;
                matchesTarget = extensionNames.Contains(BuildTarget.SeriesName);
            }
            else
            {
                return result.WithValue((file.Name, BuildTargetSpecificity: 0));
            }
        }

        int buildTargetSpecificity;
        
        if (fileExtensions.Length >= 3)
        {
            extensionName = fileExtensions[^2];
            extensionNames = extensionName.Split(',');

            if (SourceDirectory.BuildableTargets.PackageNames.Any(extensionNames.Contains))
            {
                if (packageNameIsDefined)
                {
                    return result.WithAnnotation(new ConflictingPackageNameFileExtensions(file.FullName));
                }

                packageNameIsDefined = true;
                matchesTarget = matchesTarget && extensionNames.Contains(BuildTarget.PackageName);
                totalExtensionLength += extensionName.Length + 1;
            }
            else if (SourceDirectory.BuildableTargets.SeriesNames.Any(extensionNames.Contains))
            {
                if (seriesNameIsDefined)
                {
                    return result.WithAnnotation(new ConflictingSeriesNameFileExtensions(file.FullName));
                }

                result = result.WithAnnotation(new NonStandardFileExtensionFormat(file.FullName));
                
                seriesNameIsDefined = true;
                matchesTarget = matchesTarget && extensionNames.Contains(BuildTarget.SeriesName);
                totalExtensionLength += extensionName.Length + 1;
            }
        }

        string fileName = file.Name.Substring(startIndex: 0, length: file.Name.Length - totalExtensionLength);

        if (!matchesTarget)
        {
            buildTargetSpecificity = -1;
        }
        else if (packageNameIsDefined && seriesNameIsDefined)
        {
            buildTargetSpecificity = 2;
        }
        else
        {
            buildTargetSpecificity = 1;
        }
        
        return result.WithValue((fileName, buildTargetSpecificity));
    }

    private async Task<Result> CopySourceFilesToDestinationDirectoryAsync(
        DirectoryInfo sourceDirectory,
        ImmutableDictionary<string, FileInfo> sourceFileIndex,
        DirectoryInfo destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        var result = Result.Success;
        
        if (IsPatchesDirectory(sourceDirectory))
        {
            if (!sourceFileIndex.TryGetValue("series", out var seriesFile))
            {
                return result.WithAnnotation(new CouldNotFindPatchesSeriesFile(BuildTarget));
            }

            result = result.Merge(CopyFile(
                sourceFile: seriesFile, 
                destinationDirectory: destinationDirectory, 
                fileName: "series"));

            if (result.IsFailure) return result;

            StreamReader seriesFileTextReader;
            
            try
            {
                seriesFileTextReader = seriesFile.OpenText();
            }
            catch (Exception exception)
            {
                return result.WithAnnotation(new ReadingPatchesSeriesFileFailed(
                    message: $"Opening the patches/series file '{seriesFile.FullName}' failed",
                    location: new Location { ResourceLocator = seriesFile.FullName },
                    exception: exception));
            }

            using (seriesFileTextReader)
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested) return result.WithAnnotation(new OperationCanceled());
                    string? line;
                    int lineNumber = 0;

                    try
                    {
                        line = await seriesFileTextReader.ReadLineAsync(cancellationToken);
                        ++lineNumber;
                    }
                    catch (OperationCanceledException)
                    {
                        return result.WithAnnotation(new OperationCanceled());
                    }
                    catch (Exception exception)
                    {
                        return result.WithAnnotation(new ReadingPatchesSeriesFileFailed(
                            message: $"Reading from the patches/series file '{seriesFile.FullName}' failed",
                            location: new Location
                            {
                                ResourceLocator = seriesFile.FullName, 
                                TextSpan = new LinePositionSpan(new LinePosition(lineNumber))
                            },
                            exception: exception));
                    }
                    
                    if (line == null) return result;
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

                    var patchFilePath = line.Split(' ')[0];

                    // TODO: handle patches in subdirectories
                    if (!sourceFileIndex.TryGetValue(patchFilePath, out var patchFile))
                    {
                        return result.WithAnnotation(new CouldNotFindPatchFile(seriesFile, lineNumber, patchFilePath));
                    }

                    result = result.Merge(CopyFile(
                        sourceFile: patchFile, 
                        destinationDirectory: destinationDirectory, 
                        fileName: patchFilePath));
                }
            }
        }
        else
        {
            foreach (var (fileName, fileInfo) in sourceFileIndex)
            {
                if (cancellationToken.IsCancellationRequested) return result.WithAnnotation(new OperationCanceled());

                result = result.Merge(CopyFile(fileInfo, destinationDirectory, fileName));
                if (result.IsFailure) return result;
            }  
        }
        
        return result;
    }
    
    private bool IsPatchesDirectory(DirectoryInfo directory)
    {
        var relativePath = Path.GetRelativePath(
            path: directory.FullName,
            relativeTo: SourceDirectory.DirectoryInfo.FullName);

        return relativePath.Equals("patches");
    }

    private Result CopyFile(FileInfo sourceFile, DirectoryInfo destinationDirectory, string fileName)
    {
        var destinationPath = Path.Combine(destinationDirectory.FullName, fileName);
                
        try
        {
            sourceFile.CopyTo(destinationPath);
            return Result.Success;
        }
        catch (Exception exception)
        {
            return new CreatingOutputFileFailed(
                sourceFilePath: sourceFile.FullName,
                outputFilePath: destinationPath,
                exception);
        }
    }
    
    public class ConflictingBuildTargetSpecificity(
        string conflictingFilePath1,
        string conflictingFilePath2)
        : ErrorBase(
            identifier: "FL0005",
            title: "Conflicting build target specificity",
            message: $"The file '{conflictingFilePath1}' has the same build target specificity " +
                     $"as '{conflictingFilePath2}'",
            locations: ImmutableList.Create(
                new Location { ResourceLocator = conflictingFilePath1 },
                new Location { ResourceLocator = conflictingFilePath2 })) {}
    
    public class OutputDirectoryAlreadyExists(
        DirectoryInfo outputDirectory)
        : AnnotationBase(
            identifier: "FL0006",
            title: "Output directory already exists",
            message: $"Output directory '{outputDirectory.FullName}' already exists",
            severity: AnnotationSeverity.Warning,
            warningLevel: WarningLevels.MinorWarning,
            locations: ImmutableList.Create(new Location { ResourceLocator = outputDirectory.FullName })) {}
        
    public class OutputDirectoryContainsFiles(
        DirectoryInfo outputDirectory)
        : ErrorBase(
            identifier: "FL0007",
            title: "Output directory contains files",
            message: $"Output directory '{outputDirectory.FullName}' already contains files",
            locations: ImmutableList.Create(new Location { ResourceLocator = outputDirectory.FullName })) {}
    
    public class CreatingOutputDirectoryFailed(
        DirectoryInfo outputDirectory,
        Exception exception)
        : ErrorBase(
            identifier: "FL0008",
            title: "Creating output directory failed",
            message: $"An unexpected error occured while creating the output directory '{outputDirectory.FullName}'",
            locations: ImmutableList.Create(new Location { ResourceLocator = outputDirectory.FullName }),
            innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception))) {}
    
    public class CreatingOutputFileFailed(
        string sourceFilePath,
        string outputFilePath,
        Exception exception)
        : ErrorBase(
            identifier: "FL0009",
            title: "Creating output file failed",
            message: $"An unexpected error occured while creating the output file '{outputFilePath}' (src: {sourceFilePath})",
            locations: ImmutableList.Create(new Location { ResourceLocator = outputFilePath }),
            innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception)),
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(key: "SourceFile", value: sourceFilePath)) {}
    
    public class ConflictingPackageNameFileExtensions(
        string sourceFilePath)
        : ErrorBase(
            identifier: "FL0010",
            title: "Conflicting package name file extensions",
            message: $"The file extensions of '{sourceFilePath}' specifies two package names",
            locations: ImmutableList.Create(new Location { ResourceLocator = sourceFilePath })) {}
    
    public class ConflictingSeriesNameFileExtensions(
        string sourceFilePath)
        : ErrorBase(
            identifier: "FL0011",
            title: "Conflicting package name file extensions",
            message: $"The file extensions of '{sourceFilePath}' specifies two series names",
            locations: ImmutableList.Create(new Location { ResourceLocator = sourceFilePath })) {}
    
    public class NonStandardFileExtensionFormat(
        string sourceFilePath)
        : AnnotationBase(
            identifier: "FL0012",
            title: "Non-standard file extensions format",
            message: $"The file extensions of '{sourceFilePath}' has the format "+
                     "'*.SERIES.PACKAGE' instead of '*.PACKAGE.SERIES'.",
            severity: AnnotationSeverity.Warning,
            warningLevel: WarningLevels.MinorWarning,
            locations: ImmutableList.Create(new Location { ResourceLocator = sourceFilePath })) {}
    
    public class ChangelogFileNameAndContentMismatch(
        string message,
        Location location)
        : AnnotationBase(
            identifier: "FL0024",
            title: "Changelog file name and contained changelog details mismatch",
            message: message,
            severity: AnnotationSeverity.Warning,
            warningLevel: WarningLevels.SevereWarning,
            locations: ImmutableList.Create(location)) {}
    
    public class OrigTarballNotFound(
        BuildTarget buildTarget,
        Location location)
        : ErrorBase(
            identifier: "FL0035",
            title: "Orig tarball not found",
            message: $"Orig tarball for packaging target {buildTarget} could not be found",
            locations: ImmutableList.Create(location)) {}
    
    public class DpkgBuildPackageFailed(
        BuildTarget buildTarget,
        Location location,
        string reason,
        ImmutableList<IAnnotation>? innerAnnotations = null)
        : ErrorBase(
            identifier: "FL0036",
            title: "dpkg-buildpackage failed",
            message: $"dpkg-buildpackage failed for target {buildTarget}. {reason}",
            locations: ImmutableList.Create(location),
            innerAnnotations: innerAnnotations) {}
    
    public class MultipleTargetDistributionsSpecified(
        BuildTarget buildTarget,
        ImmutableArray<DpkgSuite> distributions,
        Location location)
        : ErrorBase(
            identifier: "FL0037",
            title: "Latest changelog entry specifies multiple target distributions",
            message: $"Latest changelog entry for target {buildTarget} specifies multiple target distributions",
            locations: ImmutableList.Create(location),
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(nameof(BuildTarget), buildTarget)
                .Add(nameof(Distributions), distributions))
    {
        public BuildTarget BuildTarget => FromMetadata<BuildTarget>(nameof(BuildTarget));
        public ImmutableArray<DpkgSuite> Distributions => FromMetadata<ImmutableArray<DpkgSuite>>(nameof(Distributions));
    }
    
    public class CouldNotFindPatchesSeriesFile(
        BuildTarget buildTarget)
        : ErrorBase(
            identifier: "FL0038",
            title: "Could not find a patches/series file for build target",
            message: $"Could not find a patches/series file for build target {buildTarget}",
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(nameof(BuildTarget), buildTarget))
    {
        public BuildTarget BuildTarget => FromMetadata<BuildTarget>(nameof(BuildTarget));
    }
    
    public class ReadingPatchesSeriesFileFailed(
        string message,
        Location location,
        Exception exception)
        : ErrorBase(
            identifier: "FL0039",
            title: "Reading the patches/series file failed",
            message: message,
            locations: ImmutableList.Create(location),
            innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception)))
    {
    }
    
    public class CouldNotFindPatchFile(
        FileInfo seriesFile,
        int seriesFileLineNumber,
        string patchFilePath)
        : ErrorBase(
            identifier: "FL0040",
            title: "A patch file that was listed in the patches/series file could not be found",
            message: $"Could not find the patch file '{patchFilePath}' listed in patches/series file '{seriesFile.FullName}'",
            locations: ImmutableList.Create(
                new Location
                {
                    ResourceLocator = seriesFile.FullName, 
                    TextSpan = new LinePositionSpan(new LinePosition(seriesFileLineNumber))
                }, 
                new Location
                {
                    ResourceLocator = patchFilePath,
                }))
    {
        public Location SeriesFileLocation => Locations[0];
        
        public Location MissingPatchFileLocation => Locations[1];
    } 
}

