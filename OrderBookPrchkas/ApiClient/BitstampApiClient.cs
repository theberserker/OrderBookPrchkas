using Microsoft.Extensions.Options;
using OrderBookPrchkas.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Rest;

namespace OrderBookPrchkas.ApiClient
{
    public class BitstampApiException : Exception
    {
        public BitstampApiException(HttpStatusCode httpStatusCode, string errorResponse) 
            : base($"{httpStatusCode}:{errorResponse}")
        {
        }
    }
    public class BitstampApiClient
    {
        public static readonly Uri BaseUri = new Uri("https://www.bitstamp.net/api/v2/");

        private readonly HttpClient _httpClient;

        public BitstampApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = BaseUri;
        }

        public async Task<string> GetBalance()
        {
            var response = await _httpClient.PostAsync(
                "balance/", 
                new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

            var responseString = await response.Content.ReadAsStringAsync();

            Check(response, responseString);
            
            return responseString;
        }

        private void Check(HttpResponseMessage response, string responseString)
        {
            if (!response.IsSuccessStatusCode || responseString.Contains("\"error\""))
            {
                throw new BitstampApiException(response.StatusCode, responseString);
            }
        }
    }

    public class BitstampApiClientAuthHandler : DelegatingHandler
    {
        private readonly BitstampConfig _config;

        public BitstampApiClientAuthHandler(IOptions<BitstampConfig> options)
        {
            _config = options.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //string urlParts = request.RequestUri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Scheme, UriFormat.SafeUnescaped);
            var urlParts = request.RequestUri.ToString().Replace("https://", string.Empty);
            var nonce = Guid.NewGuid().ToString().ToLower();

            var content = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var contentType = string.IsNullOrWhiteSpace(content)
                ? null
                : request.Content.Headers.ContentType.MediaType;

            var timestamp = Math.Round(DateTime.UtcNow.UnixTimeFromDateTime() * 1000).ToString();
            var version = "v2";

            string apiKey = "BITSTAMP" + " " + _config.Key;

            var stringToSign = apiKey
                               + request.Method.Method.ToUpperInvariant() 
                               + urlParts
                               + contentType
                               + nonce
                               + timestamp
                               + version
                               + content;


            string signature = BitConverter.ToString(GetHmacSha256Hash(stringToSign)).Replace("-", "").ToUpper();

            if (string.IsNullOrWhiteSpace(content))
            {
                request.Content.Headers.Remove("Content-Type");
            }

            //string urlQuery = "/" + ApiBasePath + "/" + path;
            //string nonce = Guid.NewGuid().ToString().ToLower();
            //string timestamp = Math.Round(DateTime.UtcNow.UnixTimeFromDateTime() * 1000).ToString();
            //string version = "v2";
            //string requestBody = httpContent.ReadAsStringAsync().Result;
            //if (string.IsNullOrEmpty(requestBody)) httpContent.Headers.Remove("Content-Type");
            //string contentType = httpContent.Headers.ContentType != null ? httpContent.Headers.ContentType.ToString() : "";
            //string stringToSign = apiKey + httpVerb + urlHost + urlQuery + contentType + nonce + timestamp + version + requestBody;
            //string signature = BitConverter.ToString(GetHmacSha256Hash(stringToSign)).Replace("-", "").ToUpper();

            request.Content.Headers.Add("X-Auth", apiKey);
            request.Content.Headers.Add("X-Auth-Signature", signature);
            request.Content.Headers.Add("X-Auth-Nonce", nonce);
            request.Content.Headers.Add("X-Auth-Timestamp", timestamp);
            request.Content.Headers.Add("X-Auth-Version", version);


            return await base.SendAsync(request, cancellationToken);
        }

        private byte[] GetHmacSha256Hash(string value)
        {
            using var hash = new HMACSHA256(Encoding.UTF8.GetBytes(_config.Secret));
            return hash.ComputeHash(Encoding.UTF8.GetBytes(value));
        }

    }


    public class BitstampRestClient
    {
        public static readonly Uri BaseUri = new Uri("https://www.bitstamp.net/api/v2/");

        private readonly HttpClient _client;

        public BitstampRestClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<string> GetOrderBook(string assetPair, CancellationToken ct)
        {
            // Returns a JSON dictionary with "bids" and "asks". Each is a list of open orders and
            // each order is represented as a list holding the price and the amount.
            // Using optional group parameter with value 2 response will also have "microtimestamp" -
            // when order book was generated and "bids" and "asks" list of orders will show price,
            // amount and order id for each order.

            var url = $"order_book/{assetPair.Replace("_", string.Empty)}?group=2";

            var request = CreateRequest(HttpMethod.Get, url);

            using (var response = await _client.SendAsync(request, ct))
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                EnsureSuccessStatusCode(request, response, responseContent);

                return responseContent;
            }
        }

        private HttpRequestMessage CreateRequest(
            HttpMethod method,
            string requestUri,
            HttpContent content = null)
        {
            var request = new HttpRequestMessage(method, UriCreate(BaseUri.ToString(), requestUri));

            if (content != null)
            {
                request.Content = content;
            }

            return request;
        }

        private void EnsureSuccessStatusCode(
            HttpRequestMessage request,
            HttpResponseMessage response,
            string responseContent)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Request failed with status code {response.StatusCode}.\n" +
                    $"{request}, {request.Content?.AsString() ?? "no-content"}\n" +
                    $"{response}, {responseContent ?? "no-content"}");
            }
        }

        public static Uri UriCreate(string baseUri, string relativeUri)
        {
            return new Uri(
                new Uri(baseUri + (baseUri.EndsWith("/") ? "" : "/")),
                relativeUri);
        }
    }


}
