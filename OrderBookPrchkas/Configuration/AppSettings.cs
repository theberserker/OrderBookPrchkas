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
}

