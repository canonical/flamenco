using LibGit2Sharp;

namespace Flamenco.ExternalSources;

public class GitExternalSource(Uri repositoryUrl, string? reference = null) : ExternalSourceBase
{
    public Uri RepositoryUrl { get; } = repositoryUrl;
    public string? Reference { get; } = reference;

    public override async Task Download(string destinationDirectory)
    {
        await Task.Run(() => Repository.Clone(RepositoryUrl.ToString(), destinationDirectory));

        if (Reference is not null)
        {
            using var repo = new Repository(destinationDirectory);
            LibGit2Sharp.Commands.Checkout(repo, Reference);
        }

        Directory.Delete(Path.Join(destinationDirectory, ".git"), recursive: true);
    }
}
