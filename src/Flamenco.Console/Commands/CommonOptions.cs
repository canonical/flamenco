using System.CommandLine;
using System.CommandLine.Parsing;
using Flamenco.Packaging;

namespace Flamenco.Console.Commands;

public static class CommonOptions
{
    public static readonly Option<DirectoryInfo> SourceDirectoryOption = new(
        name: "--source-directory")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The directory that flamenco uses to produce its targets. [default: ./src]",
        };

    public static readonly Option<DirectoryInfo> DestinationDirectoryOption = new(
        name: "--destination-directory")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "The directory where the targets are produced. [default: ./dist]",
        };
}