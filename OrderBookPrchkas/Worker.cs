using ExchangeSharp;
using Microsoft.Extensions.Options;
using OrderBookPrchkas.Configuration;
using Logger = ExchangeSharp.Logger;

namespace OrderBookPrchkas;

public class WorkerAave : Worker
{
    public WorkerAave(IOptions<WorkerConfig> config, BitstampService service) 
        : base(config, service, new("aave_eur", 8, 2))
    {
    }
}
public class WorkerBch : Worker
{
    public WorkerBch(IOptions<WorkerConfig> config, BitstampService service) 
        : base(config, service, new("bch_eur", 8, 2))
    {
    }
}
public class WorkerLink : Worker
{
    public WorkerLink(IOptions<WorkerConfig> config, BitstampService service)
        : base(config, service, new("link_eur", 8, 2))
    {
    }
}
public class WorkerUni : Worker
{
    public WorkerUni(IOptions<WorkerConfig> config, BitstampService service) 
        : base(config, service, new("uni_eur", 8, 5))
    {
    }
}
public class WorkerSand : Worker
{
    public WorkerSand(IOptions<WorkerConfig> config, BitstampService service) 
        : base(config, service, new("sand_eur", 8, 5))
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
        new("sand_eur", 8, 5)
    };

    private readonly WorkerConfig _config;
    private readonly BitstampService _service;
    private readonly Coinfig _item;
    private readonly Random _random;
    private readonly Stack<string> _activeOrders;

    public Worker(IOptions<WorkerConfig> config, BitstampService service, Coinfig item)
    {
        _config = config.Value;
        _service = service;
        _item = item;
        _random = new Random(this.GetHashCode());
        _activeOrders = new Stack<string>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(1, 100))); // poor-mans jitter

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"({_item.Symbol}) Exception occurred.");
            }
            finally
            {
                await Task.Delay(_config.Delay, stoppingToken);
            }
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        var ticker = await _service.GetTicker(_item.Symbol);

        var lowerBidPrice = ticker.Bid * _config.BidFactor;

        Logger.Info($"({_item.Symbol}) Ticker: {ticker}. We will bid {lowerBidPrice}");

        var order = await _service.PlaceBuyLimitOrder(_item, lowerBidPrice, _config.BuyEurAmount);

        Logger.Info($"({_item.Symbol}) Placed order {order.OrderId}");

        _activeOrders.Push(order.OrderId);

        if (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_config.PlaceAndCancelDelay);
        }

        while(_activeOrders.Count > 0)
        {
            var orderId = _activeOrders.Peek();
            await _service.CancelOrderAsync(orderId, _item.Symbol);

            _activeOrders.Pop();

            Logger.Info($"({_item.Symbol}) Order {orderId} canceled.");
        }
    }
}
