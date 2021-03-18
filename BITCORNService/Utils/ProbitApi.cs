using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BITCORNService.Utils
{
    public static class ProbitApi
    {
        static async Task<decimal> GetPrice(string market)
        {
            IRestResponse response = null;
            try
            {
                var client = new RestClient("https://api.probit.com");
                var request = new RestRequest("/api/exchange/v1/ticker", Method.POST);
                request.AddQueryParameter("market_ids", market);

                response = await client.ExecuteGetTaskAsync(request);

                var tickers = JsonConvert.DeserializeObject<Tickers>(response.Content);

                return Convert.ToDecimal(tickers.data[0].last, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("failed to fetch price from market " + market);
                throw ex;
            }
        }

        public static async Task<decimal> GetCornPriceAsync(BitcornContext dbContext)
        {
            var cornPrice = dbContext.Price.FirstOrDefault(p => p.Symbol == "CORN");
            string sql = null;
            try
            {

                var cornBtc = await GetPrice("CORN-BTC");
                var btcUsdt = await GetPrice("BTC-USDT");
                var price = cornBtc * btcUsdt;
                //cornPrice.LatestPrice = price;
                await UpdatePrices(dbContext, price, btcUsdt, cornBtc);
                return price;
            }
            catch (Exception e)
            {
                if (cornPrice != null)
                    return cornPrice.LatestPrice;
                return -1;
            }

        }

        static async Task UpdatePrices(BitcornContext dbContext, decimal cornPrice, decimal btcUsd, decimal cornBtc)
        {
            try
            {
                var time = DateTime.Now;
                var dateStr = $"'{time.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";
                var sql = $" update [{nameof(Price)}] set [{nameof(Price.LatestPrice)}] = {cornPrice.ToString(CultureInfo.InvariantCulture)} where [{nameof(Price.Symbol)}] = 'CORN' ";
                sql += $" update [{nameof(Price)}] set [{nameof(Price.LatestPrice)}] = {btcUsd.ToString(CultureInfo.InvariantCulture)} where [{nameof(Price.Symbol)}] = 'BTC-USD' ";
                sql += $" update [{nameof(Price)}] set [{nameof(Price.LatestPrice)}] = {cornBtc.ToString(CultureInfo.InvariantCulture)} where [{nameof(Price.Symbol)}] = 'CORN-BTC' ";
                sql += $" update [{nameof(Price)}] set [{nameof(Price.UpdateTime)}] = {dateStr} ";
                int count = await DbOperations.ExecuteSqlRawAsync(dbContext, sql);
                System.Diagnostics.Debug.WriteLine(count);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<(decimal, decimal, decimal)> GetPricesAsync(BitcornContext dbContext)
        {
            var cornPriceCache = dbContext.Price.FirstOrDefault(p => p.Symbol == "CORN");

            try
            {
                if (cornPriceCache.UpdateTime == null || DateTime.Now > cornPriceCache.UpdateTime.Value.AddSeconds(20))
                {
                    var cornBtc = await GetPrice("CORN-BTC");
                    var btcUsdt = await GetPrice("BTC-USDT");
                    var cornPrice = cornBtc * btcUsdt;
                    await UpdatePrices(dbContext, cornPrice, btcUsdt, cornBtc);
                    return (cornBtc, btcUsdt, cornPrice);
                }
                else
                {
                    var btcUsdCache = dbContext.Price.FirstOrDefault(p => p.Symbol == "BTC-USD");
                    var cornBtcCache = dbContext.Price.FirstOrDefault(p => p.Symbol == "CORN-BTC");
                    return (cornBtcCache.LatestPrice, btcUsdCache.LatestPrice, cornPriceCache.LatestPrice);
                }
            }
            catch
            {

                var btcUsdCache = dbContext.Price.FirstOrDefault(p => p.Symbol == "BTC-USD");
                var cornBtcCache = dbContext.Price.FirstOrDefault(p => p.Symbol == "CORN-BTC");
                return (cornBtcCache.LatestPrice, btcUsdCache.LatestPrice, cornPriceCache.LatestPrice);
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
