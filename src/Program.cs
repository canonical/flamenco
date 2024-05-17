using System.CommandLine;
using Flamenco.Commands;

namespace Flamenco;

public class Program
{
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
