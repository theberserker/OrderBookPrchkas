using OrderBookPrchkas;
using OrderBookPrchkas.ApiClient;
using OrderBookPrchkas.Configuration;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddLogging();
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