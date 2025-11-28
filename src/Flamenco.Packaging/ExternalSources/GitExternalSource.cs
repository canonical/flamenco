using System.Collections.Concurrent;
using System.Collections.Immutable;
using LibGit2Sharp;

namespace Flamenco.Packaging.ExternalSources;

public class GitExternalSource(string repository, string? commitish = null) : ExternalSourceBase
{
    // We want to clone a repository only once and copy the local clone of the repo to every package x series destination.
    // For that reason, we use a semaphore for each repository to ensure that only one clone operation happens at a time.
    // All subsequent operations will be a directory copy.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RepositorySemaphores =
        new(StringComparer.OrdinalIgnoreCase);

    public string Repository { get; } = repository;
    public string? Commitish { get; } = commitish;

    public override async Task<Result> Download(DirectoryInfo destinationDirectory, DirectoryInfo cacheDirectory,
        CancellationToken cancellationToken = default)
    {
        var result = Result.Success;

        if (cancellationToken.IsCancellationRequested) return result.WithAnnotation(new OperationCanceled());

        var repositoryCacheDir =
            new DirectoryInfo(Path.Join(cacheDirectory.FullName, "git", destinationDirectory.Name));

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

                var gitDir = Path.Join(repositoryCacheDir.FullName, ".git");
                if (Directory.Exists(gitDir))
                {
                    Directory.Delete(Path.Join(repositoryCacheDir.FullName, ".git"), recursive: true);
                }
            }
        }
        finally
        {
            semaphore.Release();
        }

        await Task.Run(() => CopyDirectory(repositoryCacheDir, destinationDirectory), cancellationToken);

        return result;
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        foreach (var file in source.EnumerateFiles("*", SearchOption.AllDirectories))
        {
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
}
