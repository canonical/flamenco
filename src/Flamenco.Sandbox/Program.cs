using System.CommandLine;
using System.CommandLine.Invocation;
using Canonical.Launchpad;
using Canonical.Launchpad.Endpoints;
using Canonical.Launchpad.Entities;
using Flamenco.Sandbox.Commands;
using Flamenco.Sandbox.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Flamenco.Sandbox;

public static class Program
{
    public static async Task Main()
    {   
        using var client = new HttpClient();
        

        /*
         var endpoint = ApiEntryPoints.Production
           .With(ApiVersion.OnePointZero)
           .Distribution("ubuntu")
           .Series("mantic");
        var queue = await endpoint.GetPackageUploadsAsync(client, name: "dotnet", status: PackageUploadStatus.Rejected);

        foreach (var entry in queue.CurrentFragment.Entries)
        {
            Console.WriteLine($"{entry.DisplayName} {entry.Pocket} {entry.SelfLink.EndpointRoot}");
        }*/


        var endpoint = ApiEntryPoints.Production
            .With(ApiVersion.OnePointZero)
            .People("dotnet")
            .Ppa("backports");
        
        var history = await endpoint.GetPublishedSourcesAsync(client, sourcePackageName: "dotnet", exactNameMatch: false);
        
        foreach (var entry in history.CurrentFragment.Entries)   
        {
            Console.WriteLine($"{entry.SourcePackageName} {entry.SourcePackageVersion} {entry.Pocket} {entry.ComponentName} {entry.DistroSeriesLink.Name}");
            //Console.WriteLine($"{entry.DisplayName} {entry.Pocket} {entry.SelfLink.EndpointRoot} {entry.CreatorLink?.Name}");
        }
        
        var collection = history;
        Console.WriteLine("Fragment Count: " + collection.CurrentFragment.Entries.Count);
        Console.WriteLine("Total Count: " + collection.Count);
        Console.WriteLine("Has next: " + collection.CurrentFragment.HasNextFragment);
        Console.WriteLine("Has previous: " + collection.CurrentFragment.HasPreviousFragment);
    }
    
    public static async Task<int> _Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        await using var serviceProvider = services.BuildServiceProvider();
        var rootCommand = BuildRootCommand(serviceProvider);
        return await rootCommand.InvokeAsync(args);
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(typeof(LazyCommandHandler<>));
        services.AddTransient<GreetingCommandHandler>();
        services.AddTransient<Command, GreetingCommand>();
        services.AddTransient<GreetingService>();
    }

    public static RootCommand BuildRootCommand(IServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand();
        foreach (var command in serviceProvider.GetServices<Command>()) rootCommand.AddCommand(command);
        return rootCommand;
    }
}

public abstract class CommandBase<TCommandHandler> : Command where TCommandHandler : ICommandHandler
{
    public CommandBase(string name, LazyCommandHandler<TCommandHandler> commandHandler, string? description = null) : base(name, description)
    {
        Handler = commandHandler;
    }
}

public class LazyCommandHandler<TCommandHandler> : ICommandHandler where TCommandHandler : ICommandHandler
{
    private readonly IServiceProvider _services;

    public LazyCommandHandler(IServiceProvider services)
    {
        _services = services;
    }

    public int Invoke(InvocationContext context)
    {
        var commandHandler = _services.GetRequiredService<TCommandHandler>();
        return commandHandler.Invoke(context);
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        var commandHandler = _services.GetRequiredService<TCommandHandler>();
        return commandHandler.InvokeAsync(context);
    }
}

public class GreetingCommandHandler : ICommandHandler
{
    private readonly GreetingService _greetingService;

    public GreetingCommandHandler(GreetingService greetingService)
    {
        Console.WriteLine("GreetingCommandHandler");
        _greetingService = greetingService;
    }
    
    public int Invoke(InvocationContext context) => InvokeAsync(context).GetAwaiter().GetResult();
    
    public Task<int> InvokeAsync(InvocationContext context)
    {
        _greetingService.Greet();
        return Task.FromResult(0);
    }
}

public class Greeting2CommandHandler : ICommandHandler
{
    private readonly GreetingService _greetingService;

    public Greeting2CommandHandler(GreetingService greetingService)
    {
        Console.WriteLine("Greeting2CommandHandler");
        _greetingService = greetingService;
    }
    
    public int Invoke(InvocationContext context) => InvokeAsync(context).GetAwaiter().GetResult();
    
    public Task<int> InvokeAsync(InvocationContext context)
    {
        _greetingService.Greet();
        return Task.FromResult(0);
    }
}
