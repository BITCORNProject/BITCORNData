using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BITCORNService.Utils
{
    public static class ProbitApi
    {
        static async Task<decimal> GetPrice(string market)
        {

            var client = new RestClient("https://api.probit.com");
            var request = new RestRequest("/api/exchange/v1/ticker", Method.POST);
            request.AddQueryParameter("market_ids", market);

            var response = await client.ExecuteGetTaskAsync(request);

            var tickers = JsonConvert.DeserializeObject<Tickers>(response.Content);

            return Convert.ToDecimal(tickers.data[0].last, CultureInfo.InvariantCulture);
        }

        public static async Task<decimal> GetCornPriceAsync()
        {
            try
            {
                var cornBtc = await GetPrice("CORN-BTC");
                var btcUsdt = await GetPrice("BTC-USDT");
                return cornBtc * btcUsdt;
             
            }
            catch
            {

                return -1;
            }

        }
    }

    public class Tickers
    {
        public Ticker[] data { get; set; }
    }

    public class Ticker
    {
        public string last { get; set; }
        public string low { get; set; }
        public string high { get; set; }
        public string change { get; set; }
        public string base_volume { get; set; }
        public string quote_volume { get; set; }
        public string market_id { get; set; }
        public DateTime time { get; set; }
    }

}
