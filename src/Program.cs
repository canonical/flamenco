using System.CommandLine;
using Flamenco.Commands;

namespace Flamenco;

public static class Program
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
    
    public static bool IsSnap => Environment.ProcessPath?.StartsWith("/snap/") ?? false;

    public static bool IsPathAccessible(string path)
    {
        if (!IsSnap || path.StartsWith("/home/")) return true;
        
        Log.Error(message: $"The path '{path}' is not accessible for the {nameof(Flamenco)} process.");
        Log.Info(message: $"{nameof(Flamenco)} is packaged as a snap in strict mode. Only files under the " +
                          "'/home' directory are accessible.");
        return false;
    }
}
