namespace Flamenco.ExternalSources;

public abstract class ExternalSourceBase
{
    public abstract Task Download(string destinationDirectory);

    public static ExternalSourceBase Create(string descriptorFilePath)
    {
        if (!File.Exists(descriptorFilePath))
        {
            throw new FileNotFoundException("Descriptor file not found.", descriptorFilePath);
        }

        var fileContentLines = File.ReadAllLines(descriptorFilePath);
        var type = fileContentLines.FirstOrDefault(l => l.StartsWith("type"))?.Split("=").Last();

        if (type is null)
        {
            throw new ApplicationException($"{descriptorFilePath} is missing a type.");
        }

        switch (type)
        {
            case "git":
                var repositoryUrl = fileContentLines.FirstOrDefault(l => l.StartsWith("repo"))?.Split('=').Last();
                var branch = fileContentLines.FirstOrDefault(l => l.StartsWith("branch"))?.Split('=').Last();
                var tag = fileContentLines.FirstOrDefault(l => l.StartsWith("tag"))?.Split('=').Last();
                var commit = fileContentLines.FirstOrDefault(l => l.StartsWith("commit"))?.Split('=').Last();

                if (repositoryUrl is null)
                {
                    throw new ApplicationException($"Repository URL is required when descriptor file is git.");
                }

                // Reference precedence is commit, then tag, then branch. If all of them are null, the default
                // behavior is to check out the repo's default branch.
                return new GitExternalSource(new Uri(repositoryUrl), commit ?? (tag ?? branch));
            default:
                throw new ApplicationException($"Unknown external source type: {type}.");
        }
    }
}
