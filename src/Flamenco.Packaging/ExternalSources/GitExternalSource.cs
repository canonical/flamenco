using System.Collections.Immutable;
using LibGit2Sharp;

namespace Flamenco.Packaging.ExternalSources;

public class GitExternalSource(string repository, string? commitish = null) : ExternalSourceBase
{
    public string Repository { get; } = repository;
    public string? Commitish { get; } = commitish;

    public override async Task<Result> Download(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        var result = Result.Success;

        if (cancellationToken.IsCancellationRequested) return result.WithAnnotation(new OperationCanceled());

        try
        {
            await Task.Run(() =>
            {
                LibGit2Sharp.Repository.Clone(Repository, destinationDirectory);

                if (string.IsNullOrEmpty(Commitish)) return;

                using var repo = new Repository(destinationDirectory);
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

        var gitDir = Path.Join(destinationDirectory, ".git");
        if (Directory.Exists(gitDir))
        {
            Directory.Delete(Path.Join(destinationDirectory, ".git"), recursive: true);
        }

        return result;
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
