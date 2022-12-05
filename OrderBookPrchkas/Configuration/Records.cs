namespace OrderBookPrchkas.Configuration;

/// <summary>
/// Coinfig = coin coinfig ;)
/// </summary>
public record Coinfig(string Symbol, int AmountPrecision, int PricePrecision)
{
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

