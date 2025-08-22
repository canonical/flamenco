using System.CommandLine;
using Flamenco.Packaging;

namespace Flamenco.Console.Commands;

public static class CommonOptions
{
    public static readonly Option<DirectoryInfo?> SourceDirectoryOption = new(
        name: "--source-directory",
        description: "The directory that flamenco uses to produce its targets. [default: ./src]")
        {
            Arity = ArgumentArity.ExactlyOne
        };

    public static readonly Option<DirectoryInfo?> DestinationDirectoryOption = new(
        name: "--destination-directory",
        description: "The directory where the targets are produced. [default: ./dist]")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
    
    public static readonly Option<TarballCompressionMethod> DebianTarballCompressionMethod = new (
        name: "--debian-tarball-compression-method",
        description: "The compression method used to create debian tar archives. [default: xz]")
    {
        Arity = ArgumentArity.ExactlyOne,
    };
}