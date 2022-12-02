using ExchangeSharp;
using OrderBookPrchkas.Configuration;
using Polly;
using Polly.Retry;

namespace OrderBookPrchkas;

public class BitstampService : IDisposable
{
    private readonly IExchangeAPI _api;

    private readonly AsyncRetryPolicy _cancelationRetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            4,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, span) =>
            {
                Logger.Warn($"Retry attempt after {span} due to the error. {exception}");
            });

    private BitstampService(IExchangeAPI api)
    {
        _api = api;
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
        return _api.GetTickerAsync(symbol);
    }

    public Task<ExchangeOrderResult> PlaceBuyLimitOrder(Coinfig coinfig, decimal bidPrice, decimal eurAmount)
    {
        var orderDto = new ExchangeOrderRequest
        {
            MarketSymbol = coinfig.Symbol,
            IsBuy = true,
            OrderType = OrderType.Limit,
            Amount = Math.Round(eurAmount / bidPrice, coinfig.AmountPrecision),
            Price = Math.Round(bidPrice, coinfig.PricePrecision),
            ShouldRoundAmount = false
        };

        return _api.PlaceOrderAsync(orderDto);
    }

    public async Task CancelOrderAsync(string orderId, string symbol)
    {
        await _cancelationRetryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                await _api.CancelOrderAsync(orderId, symbol);
            }
            catch (APIException ex) when (IsOkToProceed(ex))
            {
                Logger.Info($"Non-existent order tried to be cancelled. Details: {ex.Message}");
            }
        });

        bool IsOkToProceed(APIException ex)
            => ex.Message.ToLowerInvariant().Contains("order not found") ||
               ex.Message.ToLowerInvariant().Contains("Invalid order id");
    }

    public void Dispose()
    {
        _api.Dispose();
    }
}
