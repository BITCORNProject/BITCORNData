using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    //[ServiceFilter(typeof(LockUserAttribute))]
    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public TxController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost("rain")]
        public async Task Rain([FromBody] RainBody rainBody)
        {
            try
            {
                var txUsers = rainBody.TxUsers;
                if (txUsers == null || !txUsers.Any()) throw new ArgumentNullException();
                //array of {amount, id}
                await TxUtils.ExecuteRainTxs(txUsers, _dbContext);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            //recipient response TODO
        }

        [HttpPost("payout")]
        public async Task Payout([FromBody] IEnumerable<TxUser> txUsers)
        {
            if (txUsers == null) throw new ArgumentNullException();
            //array of {amount, id}
            await TxUtils.ExecuteRainTxs(txUsers, _dbContext);
            //senderresponses TODO
        }

        [HttpPost("tipcorn")]
        public async Task<Dictionary<string, object>[]> Tipcorn([FromBody] TipRequest tipRequest)
        {
            if (tipRequest == null) throw new ArgumentNullException();
            if (tipRequest.From == null) throw new ArgumentNullException();
            if (tipRequest.To == null) throw new ArgumentNullException();
            if (tipRequest.Amount == 0) throw new ArgumentNullException();

            //sender twitchid, receiver twitchid, amount


            var transactions = await TxUtils.ProcessRequest(tipRequest, _dbContext);
            await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
            if (transactions != null && transactions.Length > 0)
            {
                int[] participants = new int[transactions.Length + 1];
                participants[0] = transactions[0].From.UserId;
                for (int i = 0; i < transactions.Length; i++)
                {
                    participants[i + 1] = transactions[i].To.UserId;
                }

                return await UserReflection.GetColumns(_dbContext, tipRequest.Columns, participants);
            }
            else return null;
        }

        

        [HttpPost("withdraw")]
        public async Task Withdraw([FromBody] WithdrawUser withdrawUser)
        {
            //sender twitchid, cornaddy, amount
            await TxUtils.ExecuteDebitTx(withdrawUser, _dbContext);

            //call to wallet to properly withdraw TODO

            //tx id TODO
        }



    }
}
