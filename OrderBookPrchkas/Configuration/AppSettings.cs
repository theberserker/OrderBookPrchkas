namespace OrderBookPrchkas.Configuration;

public class BitstampConfig
{
    public string CustomerId { get; set; }

    public string Key { get; set; }

    public string Secret { get; set; }
}

public class WorkerConfig
{
    public TimeSpan Delay { get; set; }
    public TimeSpan PlaceAndCancelDelay { get; set; } = TimeSpan.FromSeconds(1);

    public decimal BidFactor { get; set; } = 0.98m;
    public decimal BuyEurAmount { get; set; } = 10.55m;
}

