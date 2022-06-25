using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Serilog;
using Sphere.Shared;

Console.CancelKeyPress += (o, e) =>
{
    e.Cancel = true;
};

Log.Logger = SphericalLogger.GetLoggerConfiguration().CreateBootstrapLogger();

Log.Information("Starting up");

var registration = Services.Server.GetServiceRegistration();

try
{
    var result = await Services.RegisterService(registration);

    using var host = Host
        .CreateDefaultBuilder()
        .UseOrleans(siloBuilder =>
        {
            siloBuilder.ConfigureLogging(l => l.AddSerilog(Log.Logger));
            siloBuilder.UseLocalhostClustering();
        })
        .Build();

    // Start the host
    await host.StartAsync();

    var client = host.Services.GetRequiredService<IGrainFactory>();

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
    await Services.UnregisterService(registration);

    Log.Information("Shutting down");
    Log.CloseAndFlush();
}
