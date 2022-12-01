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
    private static readonly Coinfig[] Items = 
    {
        new("aave_eur", 8, 2)
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
                await DoWork();
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

    private async Task DoWork()
    {
        foreach (var item in Items)
        {
            var ticker = await _service.Api.GetTickerAsync(item.Symbol);

            var lowerBidPrice = ticker.Bid * 0.98m;

            Logger.Info("We will bid " + lowerBidPrice);

            var orderDto = new ExchangeOrderRequest()
            {
                MarketSymbol = item.Symbol,
                IsBuy = true,
                OrderType = OrderType.Limit,
                Amount = Math.Round(11 / lowerBidPrice, item.AmountPrecision),
                Price = Math.Round(lowerBidPrice, item.PricePrecision),
                ShouldRoundAmount = false
            };

            var order = await _service.Api.PlaceOrderAsync(orderDto);

            Logger.Info("Placed order {OrderId} trade: {TradeId}", order.OrderId, order.TradeId);

            await _service.Api.CancelOrderAsync(order.OrderId, item.Symbol);
        }
    }
}
