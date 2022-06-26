using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Serilog;
using Sphere.Shared;
using Sphere.Interfaces;
using Sphere.Grains;
using Sphere.Server.Options;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Consul;
using Orleans.Statistics;

Console.CancelKeyPress += (o, e) =>
{
    e.Cancel = true;
};

// Setting this allows us to get some benefits all over the place.
Services.Current = Services.Server;

Log.Logger = SphericalLogger.StartupLogger(Services.Current);

try
{
    var host = Host
        .CreateDefaultBuilder(args)
        .UseSerilog(Log.Logger)
        .ConfigureLogging(logging => logging.AddSerilog(Log.Logger))
        .ConfigureServices((context, sc) =>
        {
            sc.Configure<MainOptions>(context.Configuration);
            sc.AddOptions();
        })
        .UseOrleans(siloBuilder =>
        {
            siloBuilder.ConfigureLogging(l => l.AddSerilog(Log.Logger));
            siloBuilder.UseLocalhostClustering();
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.UsePerfCounterEnvironmentStatistics();
            siloBuilder.UseLinuxEnvironmentStatistics();

            siloBuilder.ConfigureApplicationParts(parts => parts
                .AddApplicationPart(typeof(IServiceDiscovery).Assembly)
                .AddApplicationPart(typeof(ServiceDiscoveryGrain).Assembly));
            siloBuilder.UseDashboard(options =>
            {
                options.Port = 9000;
            });
        })
        .Build();

    // Start the host
    await host.StartAsync();
    var client = host.Services.GetRequiredService<IGrainFactory>();

    await RegisterServices(host, client);

    Console.WriteLine("Press any key to exit.");
    Console.ReadLine();

    await host.StopAsync();
}
catch (Exception ex)
{
    if (ex.GetType().Name != "StopTheHostException")
    {
        Log.Fatal(ex, "Unhandled exception");
    }
}
finally
{
    Log.Information("Shutting down");
    Log.CloseAndFlush();
}

static async Task RegisterServices(IHost host, IGrainFactory client)
{
    var options = host.Services.GetRequiredService<IOptions<MainOptions>>();

    List<Task> registrationTasks = new();

    foreach (var serviceDefinition in options.Value.Services)
    {
        var serviceDiscoveryGrain = client.GetGrain<IServiceDiscovery>(serviceDefinition.Name);
        registrationTasks.Add(serviceDiscoveryGrain.SetServiceDefinition(serviceDefinition));
    }

    await Task.WhenAll(registrationTasks);
}
