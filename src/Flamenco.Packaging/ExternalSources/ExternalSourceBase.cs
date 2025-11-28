using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace Flamenco.Packaging.ExternalSources;

public abstract class ExternalSourceBase
{
    public abstract Task<Result> Download(DirectoryInfo destinationDirectory, DirectoryInfo cacheDirectory,
        CancellationToken cancellationToken = default);

    public static Result<ExternalSourceBase> Parse(JsonNode externalSource)
    {
        var invalidFields = new Dictionary<string, object?>();
        var type = externalSource["sourceType"]?.GetValue<string>();

        switch (type)
        {
            case "git":
                var repository = externalSource["repository"]?.GetValue<string>();
                if (repository is null)
                {
                    invalidFields.Add("repository", "The 'repository' field is required for git external sources.");
                }
                var commitish = externalSource["commitish"]?.GetValue<string>();

                return invalidFields.Any()
                    ? new InvalidGitDescriptorFile(invalidFields)
                    : new Result<ExternalSourceBase>(Result.Success, new GitExternalSource(repository!, commitish));
            case null:
                return new UnspecifiedExternalSourceType();
            default:
                return new UnsupportedExternalSourceType(type);
        }
    }

    public class UnsupportedExternalSourceType(
        string sourceType)
        : ErrorBase(
            identifier: "FL0042",
            title: "Unsupported external source type.",
            message: $"The external source type '{sourceType}' is not supported.");

    public class UnspecifiedExternalSourceType()
        : ErrorBase(
            identifier: "FL0043",
            title: "Unspecified external source type.",
            message: "The external source type is not specified in the descriptor file.");

    public class InvalidGitDescriptorFile(
        Dictionary<string, object?> invalidFields)
        : ErrorBase(
            identifier: "FL0044",
            title: "Invalid git external source descriptor.",
            message: "The git external source descriptor file is invalid.",
            metadata: invalidFields.ToImmutableDictionary());
}
