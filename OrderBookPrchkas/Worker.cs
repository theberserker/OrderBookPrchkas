using Microsoft.Extensions.Options;
using NLog;
using OrderBookPrchkas.ApiClient;
using OrderBookPrchkas.Configuration;

namespace OrderBookPrchkas;

public class SatansWorker : Worker
{
    public SatansWorker(IOptions<WorkerConfig> config, BitstampApiClient api, BitstampRestClient unauthApi)
        : base(config, api, unauthApi)
    {
    }
}

public class Worker : BackgroundService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// (Market symbol, decimal precision) pairs
    /// </summary>
    public static readonly Coinfig[] Items =
    {
        new("aave_eur", 8, 2),
        new("bch_eur", 8, 2),
        new("ltc_eur", 8, 2),
        new("link_eur", 8, 2),
        new("uni_eur", 8, 5),
        new("sand_eur", 8, 5),
        new("ada_eur", 5, 5),
        new("xrp_eur", 5, 5),
        new("eth_eur", 8, 1),
    };

    private readonly WorkerConfig _config;
    private readonly BitstampApiClient _api;

    public Worker(IOptions<WorkerConfig> config, BitstampApiClient api, BitstampRestClient unauthApi)
    {
        _config = config.Value;
        _api = api;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(1, 100))); // poor-mans jitter

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

                await DoWork2(stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception occurred. Dafuq.");
            }
            finally
            {
                await Task.Delay(_config.Delay, stoppingToken);
            }
        }
    }

    private async Task DoWork2(CancellationToken stoppingToken)
    {
        var tickersTasks = Items
            .Select(x => new
            {
                Key = x.Symbol,
                Symbol = x,
                TickerTask = _api.GetTicker(x.Symbol)
            })
            .ToDictionary(x => x.Key);

        try
        {
            var rs = await Task.WhenAll(tickersTasks.Select(x => x.Value.TickerTask));
        }
        catch (Exception ex)
        {
            Logger.Warn("Failed to get one of the tickers:" + ex);

            return; // let's retry asap
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        var orderTasks = tickersTasks
            .Select(x =>
            {
                var ticker = x.Value.TickerTask.Result;
                var lowerBidPrice = ticker.Bid * _config.BidFactor;
                Logger.Info($"({x.Key}) Ticker bid: {ticker.Bid}. We will bid {lowerBidPrice}");

                return new { X = x, Task = _api.PlaceBuyLimitOrder(x.Value.Symbol, lowerBidPrice, _config.BuyEurAmount) };
            })
            .ToList();

        try
        {
            Logger.Info("Order placing tasks: Await");
            await Task.WhenAll(orderTasks.Select(x=>x.Task));
            Logger.Info("Order placing tasks: Finished");
        }
        catch (Exception ex)
        {
            var failed = orderTasks.Where(x => !x.Task.IsCompletedSuccessfully);
            var failedKeys = string.Join(",", failed.Select(x => x.X.Key));
            Logger.Error(ex, $"Some tasks failed: {failedKeys} .... continuing towards the cancellation.");
        }

        Logger.Info("Canceling all orders.");

        await Task.Delay(_config.PlaceAndCancelDelay);

        await _api.CancelAllOrders();

        Logger.Info("Orders canceled.");

    }
}
