using System.Text.Json.Nodes;

namespace Flamenco.Packaging.ExternalSources;

public abstract class ExternalSourceBase
{
    public abstract Task Download(string destinationDirectory, CancellationToken cancellationToken = default);

    public static ExternalSourceBase Create(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Descriptor file not found.", fileInfo.FullName);
        }

        var jsonNode = JsonNode.Parse(File.ReadAllText(fileInfo.FullName));
        var type = jsonNode?["sourceType"]?.GetValue<string>();

        switch (type)
        {
            case "git":
                var repository = jsonNode?["repository"]?.GetValue<string>();
                var commitish = jsonNode?["commitish"]?.GetValue<string>();
                return repository == null
                    ? throw new InvalidDataException("Git external source descriptor must contain a 'repository' field.")
                    : new GitExternalSource(repository, commitish);
            default:
                throw new NotSupportedException($"External source type '{type}' is not supported.");
        }
    }
}
