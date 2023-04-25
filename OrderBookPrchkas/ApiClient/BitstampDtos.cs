using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OrderBookPrchkas.ApiClient
{
    // resp dtos

    public sealed class ExchangeTicker
    {
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("open")]
        public string Open { get; set; }

        [JsonProperty("high")]
        public string High { get; set; }

        [JsonProperty("low")]
        public string Low { get; set; }

        [JsonProperty("last")]
        public string Last { get; set; }

        [JsonProperty("volume")]
        public string Volume { get; set; }

        [JsonProperty("vwap")]
        public string Vwap { get; set; }

        [JsonProperty("bid")]
        public decimal Bid { get; set; }

        [JsonProperty("ask")]
        public decimal Ask { get; set; }

        [JsonProperty("open_24")]
        public string Open24 { get; set; }

        [JsonProperty("percent_change_24")]
        public string PercentChange24 { get; set; }
    }


    public class ExchangeOrderResult
    {
        public string Id { get; set; }
    }


    // req

}
