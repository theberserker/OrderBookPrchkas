using ExchangeSharp;
using OrderBookPrchkas.Configuration;

namespace OrderBookPrchkas;

public class BitstampService : IDisposable
{
    public readonly IExchangeAPI Api;

    private BitstampService(IExchangeAPI api)
    {
        Api = api;
    }

    public static async Task<BitstampService> Create(BitstampConfig config)
    {
        var api = await ExchangeAPI.GetExchangeAPIAsync<ExchangeBitstampAPI>();

        api.Passphrase = config.CustomerId.ToSecureString();
        api.PrivateApiKey = config.Secret.ToSecureString();
        api.PublicApiKey = config.Key.ToSecureString();

        return new BitstampService(api);
    }

    public Task<ExchangeTicker> GetTicker(string symbol)
    {
        return Api.GetTickerAsync(symbol);
    }

    public Task<ExchangeOrderResult> PlaceBuyLimitOrder(Coinfig coinfig, decimal bidPrice)
    {
        var orderDto = new ExchangeOrderRequest
        {
            MarketSymbol = coinfig.Symbol,
            IsBuy = true,
            OrderType = OrderType.Limit,
            Amount = Math.Round(11 / bidPrice, coinfig.AmountPrecision),
            Price = Math.Round(bidPrice, coinfig.PricePrecision),
            ShouldRoundAmount = false
        };
        
        return Api.PlaceOrderAsync(orderDto);
    }


    public void Dispose()
    {
        Api.Dispose();
    }
}
