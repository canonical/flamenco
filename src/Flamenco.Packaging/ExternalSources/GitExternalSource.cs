using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using LibGit2Sharp;

namespace Flamenco.Packaging.ExternalSources;

public class GitExternalSource(
    string repository,
    string? commitish = null,
    string? rootDirectory = null,
    IEnumerable<string>? postCloneCommands = null,
    IEnumerable<string>? ignoredFiles = null) : ExternalSourceBase
{
    // We want to clone a repository only once and copy the local clone of the repo to every package x series destination.
    // For that reason, we use a semaphore for each repository to ensure that only one clone operation happens at a time.
    // All subsequent operations will be a directory copy.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RepositorySemaphores =
        new(StringComparer.OrdinalIgnoreCase);

    public string Repository { get; } = repository;
    public string? Commitish { get; } = commitish;
    public string? RootDirectory { get; set; } = rootDirectory;
    public IReadOnlyList<string> PostCloneCommands { get; } = (postCloneCommands ?? []).ToList();
    public IReadOnlyList<string> IgnoredFiles { get; } = (ignoredFiles ?? []).ToList();

    public override async Task<Result> Download(DirectoryInfo destinationDirectory, DirectoryInfo cacheDirectory,
        CancellationToken cancellationToken = default)
    {
        var result = Result.Success;

        if (cancellationToken.IsCancellationRequested) return result.WithAnnotation(new OperationCanceled());

        // We use both the repository URL and the commitish to create a unique cache directory for each specific state
        // of the repository.
        var repoCacheDirName = $"{destinationDirectory.Name}@{Commitish ?? "default"}";
        var repositoryCacheDir =
            new DirectoryInfo(Path.Join(cacheDirectory.FullName, "git", repoCacheDirName));

        var semaphore = RepositorySemaphores.GetOrAdd(repositoryCacheDir.FullName,
            _ => new SemaphoreSlim(initialCount: 1, maxCount: 1));

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!repositoryCacheDir.Exists)
            {
                repositoryCacheDir.Create();
                try
                {
                    await Task.Run(() =>
                    {
                        LibGit2Sharp.Repository.Clone(Repository, repositoryCacheDir.FullName);

                        if (string.IsNullOrEmpty(Commitish)) return;

                        using var repo = new Repository(repositoryCacheDir.FullName);
                        Commands.Checkout(repo, Commitish);
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return result.WithAnnotation(new OperationCanceled());
                }
                catch (Exception ex)
                {
                    return new GitCloneFailed(Repository, Commitish, ex);
                }

                var postCloneResult = await RunPostCloneCommands(repositoryCacheDir, cancellationToken);
                if (postCloneResult.IsFailure)
                {
                    return postCloneResult;
                }

                var deletionResult = DeleteIgnoredFiles(repositoryCacheDir, cancellationToken);
                if (deletionResult.IsFailure)
                {
                    return deletionResult;
                }

                var gitDir = Path.Join(repositoryCacheDir.FullName, ".git");
                if (Directory.Exists(gitDir))
                {
                    Directory.Delete(gitDir, recursive: true);
                }
            }
        }
        finally
        {
            semaphore.Release();
        }

        try
        {
            var sourceDirectory = repositoryCacheDir;
            if (!string.IsNullOrWhiteSpace(RootDirectory))
            {
                var newSourceDirectory = Path.Join(repositoryCacheDir.FullName, RootDirectory);

                if (!Directory.Exists(newSourceDirectory))
                {
                    return result.WithAnnotation(new RootDirectoryNotFound(RootDirectory!));
                }

                sourceDirectory = new DirectoryInfo(newSourceDirectory);
            }

            await Task.Run(() => CopyDirectory(sourceDirectory, destinationDirectory, cancellationToken),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return result.WithAnnotation(new OperationCanceled());
        }
        catch (Exception ex)
        {
            return new RepositoryCopyFromCacheFailed(repositoryCacheDir.FullName, destinationDirectory.FullName, ex);
        }

        return result;
    }

    private async Task<Result> RunPostCloneCommands(DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var result = new Result();

        foreach (var command in PostCloneCommands)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/env",
                    ArgumentList = { "sh", "-c", command },
                    WorkingDirectory = workingDirectory.FullName,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return result.Merge(new Result().WithAnnotation(new PostCloneCommandFailed(command, process.ExitCode)));
            }
        }

        return result;
    }

    private Result DeleteIgnoredFiles(DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var result = new Result();

        foreach (var ignoredFile in IgnoredFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Join(workingDirectory.FullName, ignoredFile);

            // If neither a file nor directory exists, we don't want to treat it as an error, similar to how '.gitignore' works.
            var isFile = File.Exists(path);
            var isDirectory = Directory.Exists(path);

            if (!isFile && !isDirectory) continue;

            try
            {
                if (isDirectory)
                {
                    Directory.Delete(path, recursive: true);
                }
                else
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                return result.Merge(new Result().WithAnnotation(
                    new DeletionOfIgnoredFileFailed(path, ex)));
            }
        }

        return result;
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination, CancellationToken cancellationToken)
    {
        foreach (var file in source.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(source.FullName, file.FullName);
            var targetPath = Path.Join(destination.FullName, relativePath);

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            file.CopyTo(targetPath, overwrite: true);
        }
    }

    public class GitCloneFailed(
        string repository,
        string? commitish,
        Exception exception)
        : ErrorBase(
            identifier: "FL0050",
            title: "Git clone failed.",
            message: $"Failed to clone the git repository '{repository}' at '{commitish ?? "default branch"}'",
            innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception)));

    public class RepositoryCopyFromCacheFailed(
        string sourcePath,
        string destinationPath,
        Exception exception)
        : ErrorBase(
            identifier: "FL0051",
            title: "Repository copy from cache failed.",
            message: $"Failed to copy the git repository '{sourcePath}' from cache to '{destinationPath}'.",
            innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception)),
            locations: ImmutableList.Create(
                new Location { ResourceLocator = sourcePath },
                new Location { ResourceLocator = destinationPath }));

    public class RootDirectoryNotFound(
        string rootDirectory)
        : ErrorBase(
            identifier: "FL0052",
            title: "Root directory not found.",
            message: $"The specified root directory '{rootDirectory}' was not found in the cloned repository.");

    public class PostCloneCommandFailed(
    string command,
    int exitCode)
    : ErrorBase(
        identifier: "FL0053",
        title: "Post-clone command failed.",
        message: $"The post-clone command '{command}' failed with exit code {exitCode}.");

    public class DeletionOfIgnoredFileFailed(
        string filePath,
        Exception exception)
        : ErrorBase(
            identifier: "FL0054",
            title: "Deletion of ignored file failed.",
            message: $"Failed to delete the ignored file '{filePath}'.",
            innerAnnotations: ImmutableList.Create<IAnnotation>(new ExceptionalAnnotation(exception)),
            locations: ImmutableList.Create(
                new Location { ResourceLocator = filePath }));
}
