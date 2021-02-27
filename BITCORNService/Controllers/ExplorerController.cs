using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExplorerController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private readonly IConfiguration _configuration;
        public ExplorerController(BitcornContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpGet("getnetworkhashps")]
        public async Task<ActionResult<object>> GetNetworkhashps()
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getnetworkhashps");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getrawtransaction/{txid}")]
        public async Task<ActionResult<object>> GetRawTransaction([FromRoute] string txid, [FromQuery] int? decrypt)
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);
            if (decrypt == null) decrypt = 0;
            var request = new RestRequest($"getrawtransaction/{txid}?decrypt={decrypt}");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("gettxoutsetinfo")]
        public async Task<ActionResult<object>> GetTxOutsetInfo()
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"gettxoutsetinfo");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getblockhash")]
        public async Task<ActionResult<object>> GetBlockHash([FromQuery] int index)
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getblockhash?index={index}");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getblock")]
        public async Task<ActionResult<object>> GetBlock([FromQuery] string block)
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getblock?block={block}");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getconnectioncount")]
        public async Task<ActionResult<object>> GetConnectionCount()
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getconnectioncount");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getdifficulty")]
        public async Task<ActionResult<object>> GetDifficulty()
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getdifficulty");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getblockcount")]
        public async Task<ActionResult<object>> GetBlockCount()
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getblockcount");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getdistribution")]
        public async Task<ActionResult<object>> GetDistribution()
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getdistribution");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getmoneysupply")]
        public async Task<ActionResult<object>> GetMoneySupply()
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getmoneysupply");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getaddress")]
        public async Task<ActionResult<object>> GetAddress([FromQuery] string address)
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getaddress?address={address}");
            return await client.GetAsync<object>(request);
        }


        [HttpGet("gettx")]
        public async Task<ActionResult<object>> GetTx([FromQuery] string txid, [FromQuery] int decrypt)
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"gettx/?txid={txid}&decrypt={decrypt}");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getbalance")]
        public async Task<ActionResult<object>> Getbalance([FromQuery] string address)
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getbalance?address={address}");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getlasttxsajax")]
        public async Task<ActionResult<object>> GetLastTxsAjax([FromQuery] int amount)
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getlasttxsajax?amount={amount}");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("getaddresstxsajax")]
        public async Task<ActionResult<object>> GetAddresstxsajax([FromQuery] string address)
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"getaddresstxsajax?address={address}");
            return await client.GetAsync<object>(request);
        }

        [HttpGet("masternodecount")]
        public async Task<ActionResult<object>> Masternodecount()
        {
            var server = await _dbContext.WalletServer.FirstOrDefaultAsync(x => x.Index == 1);

            if (server == null) return StatusCode(404);

            var url = server.Endpoint.Replace("llapi", "explorer");
            var client = new RestClient(url);

            var request = new RestRequest($"masternodecount");

            dynamic response = await client.GetAsync<object>(request);
            JObject obj = JObject.Parse(response["result"]);
            return new
            {
                total = (int)obj["total"],
                enabled = (int)obj["enabled"]
            };
        }
    }
}