using ExchangeSharp;
using Microsoft.Extensions.Options;
using OrderBookPrchkas.Configuration;
using Logger = ExchangeSharp.Logger;

namespace OrderBookPrchkas;

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

    public Worker(IOptions<WorkerConfig> config, BitstampService service)
    {
        _config = config.Value;
        _service = service;
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
        foreach (var item in Items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var ticker = await _service.GetTicker(item.Symbol);

            var lowerBidPrice = ticker.Bid * _config.BidFactor;

            Logger.Info($"Ticker: {ticker}. We will bid {lowerBidPrice}");

            var order = await _service.PlaceBuyLimitOrder(item, lowerBidPrice);

            Logger.Info($"Placed order {order.OrderId}, tradeId: {order.TradeId}.");

            await Task.Delay(_config.PlaceAndCancelDelay);
            await _service.CancelOrderAsync(order.OrderId, item.Symbol);

            Logger.Info($"Order {order.OrderId} canceled.");
        }
    }
}
