using Microsoft.Extensions.Options;
using OrderBookPrchkas;
using OrderBookPrchkas.ApiClient;
using OrderBookPrchkas.Configuration;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton(sp => BitstampService.Create(sp.GetRequiredService<IOptions<BitstampConfig>>().Value).GetAwaiter().GetResult());
        //services.AddHostedService<Worker>();

        //services.AddHostedService<WorkerAave>();
        //services.AddHostedService<WorkerBch>();
        //services.AddHostedService<WorkerLink>();
        //services.AddHostedService<WorkerUni>();
        //services.AddHostedService<WorkerSand>();
        services.AddHostedService<SatansWorker>();

        services
            .AddTransient<BitstampApiClientAuthHandler>()
            .AddHttpClient<BitstampApiClient>()
            .AddHttpMessageHandler<BitstampApiClientAuthHandler>();

        services
            .AddHttpClient<BitstampRestClient>();

        services.Configure<BitstampConfig>(ctx.Configuration.GetSection("Bitstamp"));
        services.Configure<WorkerConfig>(ctx.Configuration.GetSection("WorkerConfig"));
        
    });

var host = hostBuilder.Build();
host.Run();