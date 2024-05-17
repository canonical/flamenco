using System.CommandLine;
using Flamenco.Commands;

namespace Flamenco;

public class Program
{
    public static async Task<int> BuildDebianTarball(
        string[] targets,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Build debian tarball for:");
        foreach (var target in targets)
        {
            Console.WriteLine($"{target}");
        }
        
        return 0;
    }
    
    public static async Task<int> BuildOrigTarball(
        string[] targets,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Build orig tarball for:");
        foreach (var sourcePackageName in targets)
        {
            Console.WriteLine($"{sourcePackageName}");
        }
        
        return 0;
    }
    
    public static Task<int> Main(string[] args)
    {
        try
        {
            return BuildRootCommand().InvokeAsync(args);
        }
        catch (Exception exception)
        {
            return Task.FromException<int>(exception);
        }
    }

    private static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand(description: "Provides tooling for maintainers of .NET Ubuntu packages.");
        rootCommand.AddCommand(new BuildCommand());
        
        return rootCommand;
    }
}
