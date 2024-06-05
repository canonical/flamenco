using System.Diagnostics.CodeAnalysis;

namespace Flamenco.Packaging;

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
        var destinationFiles = new Dictionary<string, (FileInfo Info, int BuildTargetSpecificity)>();
        
        foreach (var file in sourceDirectory.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (!TryInspectFileExtension(file, out var fileName, out var buildTargetSpecificity))
            {
                errorDetected = true;
                continue;
            }

            if (buildTargetSpecificity < 0) continue;
            
            if (destinationFiles.TryGetValue(fileName, out var otherFile))
            {
                if (buildTargetSpecificity.Value > otherFile.BuildTargetSpecificity)
                {
                    destinationFiles[fileName] = (file, buildTargetSpecificity.Value);
                }
                else if (buildTargetSpecificity.Value == otherFile.BuildTargetSpecificity)
                {
                    Log.Error(message: $"The file '{file.FullName}' has the same build target specificity as '{otherFile.Info.FullName}'");
                    errorDetected = true;
                }
            }
            else
            {
                destinationFiles[fileName] = (file, buildTargetSpecificity.Value);
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
        
        foreach (var (fileName, (fileInfo, _)) in destinationFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
                
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
    
    private bool TryInspectFileExtension(
        FileInfo file, 
        [NotNullWhen(returnValue: true)]
        out string? fileName, 
        [NotNullWhen(returnValue: true)]
        out int? buildTargetSpecificity)
    {
        string[] fileExtensions = file.Name.Split('.');
        // Remember, that fileExtensions[0] is just the file name!

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