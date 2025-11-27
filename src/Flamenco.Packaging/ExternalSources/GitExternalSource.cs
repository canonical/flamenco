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

        await Task.Run(() => LibGit2Sharp.Repository.Clone(Repository, destinationDirectory, new CloneOptions
        {
            BranchName = Commitish
        }), cancellationToken);

        Directory.Delete(Path.Join(destinationDirectory, ".git"), recursive: true);

        return result;
    }
}
