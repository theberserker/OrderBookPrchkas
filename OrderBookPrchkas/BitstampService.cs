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


    public void Dispose()
    {
        Api.Dispose();
    }
}
