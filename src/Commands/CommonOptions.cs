using System.CommandLine;
using Flamenco.Packaging;

namespace Flamenco.Commands;

public static class CommonOptions
{
    public static readonly Option<DirectoryInfo?> SourceDirectoryOption = new(
        name: "--source-directory",
        description: "The directory that the build tool uses to produce its targets. [default: ./src]")
        {
            Arity = ArgumentArity.ExactlyOne
        };

    public static readonly Option<DirectoryInfo?> DestinationDirectoryOption = new(
        name: "--destination-directory",
        description: "The directory where the targets are build. [default: ./dist]")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
    
    public static readonly Option<Tarball.CompressionMethod> DebianTarballCompressionMethod = new (
        name: "--debian-tarball-compression-method",
        description: "The compression method used to create debian tar archives. [default: xz]")
    {
        Arity = ArgumentArity.ExactlyOne,
    };
}