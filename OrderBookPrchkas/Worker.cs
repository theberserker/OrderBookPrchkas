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

    public Worker(IOptions<WorkerConfig> config, BitstampService service, Coinfig item)
    {
        _config = config.Value;
        _service = service;
        _item = item;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception occurred.");
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

        await Task.Delay(_config.PlaceAndCancelDelay);
        await _service.CancelOrderAsync(order.OrderId, _item.Symbol);

        Logger.Info($"({_item.Symbol}) Order {order.OrderId} canceled.");   
    }
}
