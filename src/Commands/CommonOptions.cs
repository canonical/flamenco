using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Flamenco.Packaging;

namespace Flamenco.Commands;

public static class CommonOptions
{
    public static readonly Option<DirectoryInfo?> SourceDirectoryOption = new(
        name: "--source-directory",
        description: "The directory that the build tool uses to produce its targets.")
        {
            Arity = ArgumentArity.ExactlyOne
        };

    public static readonly Option<DirectoryInfo?> DestinationDirectoryOption = new(
        name: "--destination-directory",
        description: "The directory where the targets are build.")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
}