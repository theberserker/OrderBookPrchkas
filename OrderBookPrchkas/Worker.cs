using System.Reflection.Metadata.Ecma335;
using ExchangeSharp;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OrderBookPrchkas.ApiClient;
using OrderBookPrchkas.Configuration;
using Logger = ExchangeSharp.Logger;

namespace OrderBookPrchkas;

//public class WorkerAave : Worker
//{
//    public WorkerAave(IOptions<WorkerConfig> config, BitstampService service, BitstampApiClient api, BitstampRestClient unauthApi)
//        : base(config, service, new("aave_eur", 8, 2), api, unauthApi)
//    {
//    }
//}
//public class WorkerBch : Worker
//{
//    public WorkerBch(IOptions<WorkerConfig> config, BitstampService service) 
//        : base(config, service, new("bch_eur", 8, 2))
//    {
//    }
//}
//public class WorkerLink : Worker
//{
//    public WorkerLink(IOptions<WorkerConfig> config, BitstampService service)
//        : base(config, service, new("link_eur", 8, 2))
//    {
//    }
//}
//public class WorkerUni : Worker
//{
//    public WorkerUni(IOptions<WorkerConfig> config, BitstampService service) 
//        : base(config, service, new("uni_eur", 8, 5))
//    {
//    }
//}
//public class WorkerSand : Worker
//{
//    public WorkerSand(IOptions<WorkerConfig> config, BitstampService service, BitstampApiClient api, BitstampRestClient unauthApi)
//        : base(config, service, new("sand_eur", 8, 5), api, unauthApi)
//    {
//    }
//}

public class SatansWorker : Worker
{
    public SatansWorker(IOptions<WorkerConfig> config, BitstampService service, BitstampApiClient api, BitstampRestClient unauthApi)
        : base(config, api, unauthApi)
    {
    }
}

public class Worker : BackgroundService
{
    /// <summary>
    /// (Market symbol, decimal precision) pairs
    /// </summary>
    public static readonly Coinfig[] Items =
    {
        new("aave_eur", 8, 2),
        new("bch_eur", 8, 2),
        new("link_eur", 8, 2),
        new("uni_eur", 8, 5),
        new("sand_eur", 8, 5),
        new("ada_eur", 5, 5),
        new("xrp_eur", 5, 5),
    };

    private readonly WorkerConfig _config;
    //private readonly BitstampService _service;
    //private readonly Coinfig _item;
    private readonly BitstampApiClient _api;
    private readonly BitstampRestClient _unauthApi;
    private readonly Random _random;
    private readonly Stack<string> _activeOrders;

    public Worker(IOptions<WorkerConfig> config, BitstampApiClient api, BitstampRestClient unauthApi)
    {
        _config = config.Value;
        //_service = service;
        //_item = item;
        _api = api;
        _unauthApi = unauthApi;
        _random = new Random(this.GetHashCode());
        _activeOrders = new Stack<string>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(1, 100))); // poor-mans jitter

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //var r = await _api.GetBalance();

                //var r2 = await _unauthApi.GetOrderBook(_item.Symbol, stoppingToken);

                await DoWork2(stoppingToken);

                //await DoWork(stoppingToken);
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
                //TickerTask = _service.GetTicker(x.Symbol),
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

                //return new { X = x, Task = _service.PlaceBuyLimitOrder(x.Value.Symbol, lowerBidPrice, _config.BuyEurAmount) };
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

    // old method that accepts Coinfig
    //private async Task DoWork(CancellationToken cancellationToken)
    //{
    //    var ticker = await _service.GetTicker(_item.Symbol);

    //    var lowerBidPrice = ticker.Bid * _config.BidFactor;

    //    Logger.Info($"({_item.Symbol}) Ticker: {ticker}. We will bid {lowerBidPrice}");

    //    var order = await _service.PlaceBuyLimitOrder(_item, lowerBidPrice, _config.BuyEurAmount);

    //    Logger.Info($"({_item.Symbol}) Placed order {order.OrderId}");

    //    _activeOrders.Push(order.OrderId);

    //    if (!cancellationToken.IsCancellationRequested)
    //    {
    //        await Task.Delay(_config.PlaceAndCancelDelay);
    //    }

    //    while (_activeOrders.Count > 0)
    //    {
    //        var orderId = _activeOrders.Peek();
    //        await _service.CancelOrderAsync(orderId, _item.Symbol);

    //        _activeOrders.Pop();

    //        Logger.Info($"({_item.Symbol}) Order {orderId} canceled.");
    //    }
    //}
}
