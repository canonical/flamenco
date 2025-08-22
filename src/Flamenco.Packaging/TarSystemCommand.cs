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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace Flamenco.Packaging;

public class TarSystemCommand : ITarArchivingServiceProvider
{
    public async Task<Result> CreateTarArchiveAsync(
        FileInfo archiveFile, 
        DirectoryInfo archiveRoot, 
        IEnumerable<string> includedPaths,
        TarballCompressionMethod compressionMethod = TarballCompressionMethod.None,
        CancellationToken cancellationToken = default)
    {
        if (archiveFile.Exists) 
            return new Result().WithAnnotation(new TarArchiveAlreadyExists(archivePath: archiveFile.FullName));
        
        if (!archiveRoot.Exists)
            return new Result().WithAnnotation(new TarArchiveRootNotFound(archiveRootPath: archiveRoot.FullName));

        var arguments = ImmutableList.CreateBuilder<string>();
        arguments.Add("--create");
        arguments.Add("--file");
        arguments.Add(archiveFile.FullName);
        arguments.Add("--directory");
        arguments.Add(archiveRoot.FullName);
        
        switch (compressionMethod)
        {
            case TarballCompressionMethod.XZ:
                arguments.Add("--xz");
                break;
            case TarballCompressionMethod.GZip:
                arguments.Add("--gzip");
                break;
        }
        
        foreach (var file in includedPaths)   
        {
            arguments.Add(file);
        }

        return await RunTarAsync(
            arguments: arguments.ToImmutable(), 
            archiveLocation: new Location { ResourceLocator = archiveFile.FullName },
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result> ExtractTarArchiveAsync(
        FileInfo archiveFile, 
        DirectoryInfo targetDirectory, 
        uint stripComponents = 0,
        CancellationToken cancellationToken = default)
    {
        var result = new Result();

        if (!archiveFile.Exists)
        {
            return result.WithAnnotation(new TarArchiveNotFound(archiveFile.FullName));
        }

        if (!targetDirectory.Exists)
        {
            result = result.WithAnnotation(new ExtractionDirectoryDoesNotExists(targetDirectory.FullName));
            targetDirectory.Create();
        }
        
        var arguments = ImmutableList.CreateBuilder<string>();
        arguments.Add("--extract");
        arguments.Add("--file");
        arguments.Add(archiveFile.FullName);
        arguments.Add("--directory");
        arguments.Add(targetDirectory.FullName);

        if (stripComponents > 0)
        {
            arguments.Add("--strip-component");
            arguments.Add(stripComponents.ToString());
        }
        
        return result.Merge(await RunTarAsync(
            arguments: arguments.ToImmutable(), 
            archiveLocation: new Location { ResourceLocator = archiveFile.FullName },
            cancellationToken)
            .ConfigureAwait(false));
    }

    private static async Task<Result> RunTarAsync(
        ImmutableList<string> arguments,
        Location archiveLocation,
        CancellationToken cancellationToken = default)
    {
        var result = new Result();
        var tarProcessInfo = new ProcessStartInfo(
            fileName: "/usr/bin/env", 
            arguments: arguments.Prepend("tar"))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        
        using var tarProcess = new Process();
        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
    
        try
        {
            tarProcess.StartInfo = tarProcessInfo;
            tarProcess.OutputDataReceived += (_, output) =>
            {
                if (output.Data is null) return;
                standardOutput.Append(output.Data);
            };
            tarProcess.ErrorDataReceived += (_, output) =>
            {
                if (output.Data is null) return;
                standardError.Append(output.Data);
            };

            tarProcess.Start();
            tarProcess.BeginOutputReadLine();
            tarProcess.BeginErrorReadLine();
            await tarProcess.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return result.WithAnnotation(new OperationCanceled(locations: ImmutableList.Create(archiveLocation)));
        }
        catch (Exception exception)
        {
            return result.WithAnnotation(
                new UnexpectedTarRuntimeError(
                    archiveLocation, arguments,
                    standardOutput: standardOutput.ToString(),
                    standardError: standardError.ToString(),
                    exception: exception));
        }
        
        if (tarProcess.ExitCode != 0)
        {
            return result.WithAnnotation(new TarFailed(
                archiveLocation, arguments,
                exitCode: tarProcess.ExitCode,
                standardOutput: standardOutput.ToString(),
                standardError: standardError.ToString()));
        }

        return result;
    }
    
    public class TarArchiveAlreadyExists(
        string archivePath)
    : ErrorBase(
        identifier: "FL0001",
        title: "tar archive already exists",
        message: $"Can not create tar archive, because a file with the specified path '{archivePath}' already exists",
        locations: ImmutableList.Create(new Location { ResourceLocator = archivePath })) {}
    
    public class TarArchiveRootNotFound(
        string archiveRootPath)
    : ErrorBase(
        identifier: "FL0002",
        title: "tar archive root not found",
        message: $"Can not find a directory at the specified tar archive root '{archiveRootPath}'",
        locations: ImmutableList.Create(new Location { ResourceLocator = archiveRootPath })) {}

    public class UnexpectedTarRuntimeError(
        Location archiveLocation,
        ImmutableList<string> arguments,
        string standardOutput,
        string standardError,
        Exception exception)
        : ErrorBase(
            identifier: "FL0003",
            title: "Unexpected exception occured while running tar",
            message: $"An unexpected exception '{exception.GetType().FullName}' has occured while running tar. {exception.Message}",
            locations: ImmutableList.Create(archiveLocation),
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(nameof(Arguments), arguments)
                .Add(nameof(StandardOutput), standardOutput)
                .Add(nameof(StandardError), standardError)
                .Add(nameof(Exception), exception))
    {
        public ImmutableList<string> Arguments => FromMetadata<ImmutableList<string>>(nameof(Arguments));
        public string StandardOutput => FromMetadata<string>(nameof(StandardOutput));
        public string StandardError => FromMetadata<string>(nameof(StandardError));
        public Exception Exception => FromMetadata<Exception>(nameof(Exception));
    }

    public class TarFailed(
        Location archiveLocation,
        ImmutableList<string> arguments,
        int exitCode,
        string standardOutput,
        string standardError)
        : ErrorBase(
            identifier: "FL0004",
            title: "tar process exited with failure status code",
            message: $"Running tar failed (ExitCode: {exitCode}).",
            locations: ImmutableList.Create(archiveLocation),
            metadata: ImmutableDictionary<string, object?>.Empty
                .Add(nameof(Arguments), arguments)
                .Add(nameof(ExitCode), exitCode)
                .Add(nameof(StandardOutput), standardOutput)
                .Add(nameof(StandardError), standardError))
    {
        public ImmutableList<string> Arguments => FromMetadata<ImmutableList<string>>(nameof(Arguments));
        public int ExitCode => FromMetadata<int>(nameof(ExitCode));
        public string StandardOutput => FromMetadata<string>(nameof(StandardOutput));
        public string StandardError => FromMetadata<string>(nameof(StandardError));
    }
    
    public class TarArchiveNotFound(
        string archivePath)
        : ErrorBase(
            identifier: "FL0033",
            title: "tar archive not found",
            message: $"Can not find tar archive '{archivePath}'",
            locations: ImmutableList.Create(new Location { ResourceLocator = archivePath })) {}
    
    public class ExtractionDirectoryDoesNotExists(
        string extractionDirectoryPath)
        : AnnotationBase(
            identifier: "FL0034",
            title: "tar archive extraction directory does not already exist",
            message: $"tar archive extraction directory '{extractionDirectoryPath}' does not already exist",
            severity: AnnotationSeverity.Warning,
            warningLevel: WarningLevels.MinorWarning,
            locations: ImmutableList.Create(new Location { ResourceLocator = extractionDirectoryPath })) {}
}