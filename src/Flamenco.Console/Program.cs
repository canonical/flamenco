using System.CommandLine;
using System.Diagnostics;
using Flamenco.Console.Commands;

namespace Flamenco.Console;

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
        rootCommand.Name = "flamenco";
        rootCommand.AddCommand(new PackCommand());
        rootCommand.AddCommand(new StatusCommand());
        
        return rootCommand;
    }
    
#if SNAPCRAFT
    private const string SnapHomePlugName = "home";
    private const string SnapRemovableMediaPlugName = "removable-media";
    
    public static async ValueTask<bool> IsPathAccessibleAsync(string path, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(path);
        string? missingConnection = null;
        
        if (fullPath.StartsWith("/home"))
        {
            if (await IsSnapPlugConnectedAsync(SnapHomePlugName, cancellationToken)) return true;
            missingConnection = SnapHomePlugName;
        }
        else if (fullPath.StartsWith("/media") || fullPath.StartsWith("/mnt") || fullPath.StartsWith("/run/media"))
        {
            if (await IsSnapPlugConnectedAsync(SnapRemovableMediaPlugName, cancellationToken)) return true;
            missingConnection = SnapRemovableMediaPlugName;
        }
        
        Log.Error(message: $"The path '{path}' is not accessible for the {nameof(Flamenco)} process due to snap confinement.");

        if (missingConnection != null)
        {
            var snapName = Environment.GetEnvironmentVariable("SNAP_NAME");
            if (snapName != null)
            {
                Log.Info(message: "You can make this path accessible to the snap by running the following command: " + 
                                  $"'snap connect {snapName}:{missingConnection}'.");
            }
        }
        
        return false;
    }
    
    private static async ValueTask<bool> IsSnapPlugConnectedAsync(
        string interfaceName,
        CancellationToken cancellationToken = default)
    {
        // Note: I do not handle any exceptions here, because if this fails than there are a whole set of wrong
        // assumptions. It's better to fail fast and violently in this case.
        var snapctlProcess = new Process { 
            StartInfo = new ProcessStartInfo(
                fileName: $"/usr/bin/snapctl",
                arguments: $"is-connected {interfaceName}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
            
        snapctlProcess.Start();
        await snapctlProcess.WaitForExitAsync(cancellationToken);
        return snapctlProcess.ExitCode == 0;
    }
#endif
}
