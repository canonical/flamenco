using System.Diagnostics.CodeAnalysis;
using Flamenco.ExternalSources;

namespace Flamenco;

public class DebianTarballBuilder
{
    public required BuildTarget BuildTarget { get; set; }
    public required SourceDirectoryInfo SourceDirectory { get; set; }
    public required DirectoryInfo DestinationDirectory { get; set; }

    public async Task<bool> BuildDebianDirectoryAsync(CancellationToken cancellationToken = default)
    {
        return await ProcessDirectoryAsync(
            sourceDirectory: SourceDirectory.DirectoryInfo,
            destinationDirectory: DestinationDirectory,
            cancellationToken);
    }

    private async Task<bool> ProcessDirectoryAsync(
        DirectoryInfo sourceDirectory,
        DirectoryInfo destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        // TODO: rename this method. "process" is a term that is to generic.

        bool errorDetected = false;
        var destinationFiles = new Dictionary<string, (FileInfo Info, int BuildTargetSpecificity, bool IsFlamencoFile)>();

        foreach (var file in sourceDirectory.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryInspectFileExtension(file, out var fileName, out var buildTargetSpecificity, out var isFlamencoFile))
            {
                errorDetected = true;
                continue;
            }

            if (buildTargetSpecificity < 0) continue;

            if (destinationFiles.TryGetValue(fileName, out var otherFile))
            {
                if (buildTargetSpecificity.Value > otherFile.BuildTargetSpecificity)
                {
                    destinationFiles[fileName] = (file, buildTargetSpecificity.Value, isFlamencoFile);
                }
                else if (buildTargetSpecificity.Value == otherFile.BuildTargetSpecificity)
                {
                    Log.Error(message: $"The file '{file.FullName}' has the same build target specificity as '{otherFile.Info.FullName}'");
                    errorDetected = true;
                }
            }
            else
            {
                destinationFiles[fileName] = (file, buildTargetSpecificity.Value, isFlamencoFile);
            }
        }

        if (errorDetected) return false;
        cancellationToken.ThrowIfCancellationRequested();

        if (destinationDirectory.Exists)
        {
            Log.Warning(message: $"Destination directory '{destinationDirectory.FullName}' already exists.");

            if (destinationDirectory.EnumerateFiles().Any() || destinationDirectory.EnumerateDirectories().Any())
            {
                Log.Error(message: $"Destination directory '{destinationDirectory.FullName}' is not empty.");
                return false;
            }
        }
        else
        {
            try
            {
                destinationDirectory.Create();
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to create destination directory '{destinationDirectory.FullName}'");
                Log.Debug($"Exception Message: {exception.Message}");
                return false;
            }
        }

        foreach (var (fileName, (fileInfo, _, isFlamencoFile)) in destinationFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (isFlamencoFile)
            {
                switch (fileName)
                {
                    case ValidDescriptorFileNames.External:
                        try
                        {
                            var externalSource = ExternalSourceBase.Create(fileInfo.FullName);
                            await externalSource.Download(destinationDirectory.FullName);
                            break;
                        }
                        catch (Exception exception)
                        {
                            Log.Error($"Failed to reference external source with {fileInfo.FullName}");
                            Log.Debug($"Exception Message: {exception.Message}");
                            return false;
                        }
                }
            }
            else
            {
                var destinationPath = Path.Combine(destinationDirectory.FullName, fileName);

                try
                {
                    fileInfo.CopyTo(destinationPath);
                }
                catch (Exception exception)
                {
                    Log.Error($"Failed to create destination file '{destinationPath}'");
                    Log.Debug($"Exception Message: {exception.Message}");
                    return false;
                }
            }
        }

        var tasks = new List<Task<bool>>();
        foreach (var sourceSubDirectory in sourceDirectory.EnumerateDirectories())
        {
            var destinationSubDirectoryPath = Path.Combine(destinationDirectory.FullName, sourceSubDirectory.Name);

            tasks.Add(ProcessDirectoryAsync(
                sourceDirectory: sourceSubDirectory,
                destinationDirectory: new DirectoryInfo(destinationSubDirectoryPath),
                cancellationToken));
        }

        foreach (var result in await Task.WhenAll(tasks))
        {
            errorDetected = errorDetected && result;
        }

        return !errorDetected;
    }

    /// <summary>
    /// Inspects the file name and determines which Flamenco parameters are set based on the file extensions.
    /// </summary>
    /// <param name="file">The file information.</param>
    /// <param name="fileName">The final file name without any Flamenco parameters.</param>
    /// <param name="buildTargetSpecificity">
    /// A specificity score that determines how specific a file is to a source package or series.
    /// A specificity of <c>-1</c> means that the file is not specific to current build target.
    /// A specificity of <c>0</c> means that the file is not specific to a source package or series.
    /// A specificity of <c>1</c> means that the file is specific to either a source package or a series.
    /// A specificity of <c>2</c> means that the file is specific to both a source package and a series.
    /// </param>
    /// <param name="isFlamencoFile">
    /// Flags whether the current file is a Flamenco internal file that does not get directly copied to the
    /// destination directory, such as a Flamenco external link or orig file.
    /// </param>
    /// <returns></returns>
    private bool TryInspectFileExtension(
        FileInfo file,
        [NotNullWhen(returnValue: true)]
        out string? fileName,
        [NotNullWhen(returnValue: true)]
        out int? buildTargetSpecificity,
        out bool isFlamencoFile)
    {
        string[] fileExtensions = file.Name.Split('.');
        // Remember, that fileExtensions[0] is just the file name!

        isFlamencoFile = ValidDescriptorFileNames.All.Any(f => file.Name.Contains(f));

        if (fileExtensions.Length < 2)
        {
            fileName = file.Name;
            buildTargetSpecificity = 0;
            return true;
        }

        bool matchesTarget = true;
        bool packageNameIsDefined = false;
        bool seriesNameIsDefined = false;

        string extensionName;
        int totalExtensionLength = 0;

        if (fileExtensions.Length >= 2)
        {
            extensionName = fileExtensions[^1];
            totalExtensionLength = extensionName.Length + 1;

            if (SourceDirectory.BuildableTargets.PackageNames.Contains(extensionName))
            {
                packageNameIsDefined = true;
                matchesTarget = extensionName.Equals(BuildTarget.PackageName);
            }
            else if (SourceDirectory.BuildableTargets.SeriesNames.Contains(extensionName))
            {
                seriesNameIsDefined = true;
                matchesTarget = extensionName.Equals(BuildTarget.SeriesName);
            }
            else
            {
                fileName = file.Name;
                buildTargetSpecificity = 0;
                return true;
            }
        }

        if (fileExtensions.Length >= 3)
        {
            extensionName = fileExtensions[^2];

            if (SourceDirectory.BuildableTargets.PackageNames.Contains(extensionName))
            {
                if (packageNameIsDefined)
                {
                    Log.Error(message: $"The file extensions of '{file.FullName}' specifies two package names.");

                    fileName = null;
                    buildTargetSpecificity = null;
                    return false;
                }

                packageNameIsDefined = true;
                matchesTarget = matchesTarget && extensionName.Equals(BuildTarget.PackageName);
                totalExtensionLength += extensionName.Length + 1;
            }
            else if (SourceDirectory.BuildableTargets.SeriesNames.Contains(extensionName))
            {
                if (seriesNameIsDefined)
                {
                    Log.Error(message: $"The file extensions of '{file.FullName}' specifies two series names.");

                    fileName = null;
                    buildTargetSpecificity = null;
                    return false;
                }

                Log.Warning(message: $"The file extensions of '{file.FullName}' has the format '*.SERIES.PACKAGE' instead of '*.PACKAGE.SERIES'.");
                seriesNameIsDefined = true;
                matchesTarget = matchesTarget && extensionName.Equals(BuildTarget.SeriesName);
                totalExtensionLength += extensionName.Length + 1;
            }
        }

        fileName = file.Name.Substring(startIndex: 0, length: file.Name.Length - totalExtensionLength);

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

        return true;
    }
}
