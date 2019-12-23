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
using BITCORNService.Utils.Stats;
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
        public async Task<object> Tipcorn([FromBody] TipRequest tipRequest)
        {
            if (tipRequest == null) throw new ArgumentNullException();
            if (tipRequest.From == null) throw new ArgumentNullException();
            if (tipRequest.To == null) throw new ArgumentNullException();
            if (tipRequest.Amount == 0) throw new ArgumentNullException();
            
            try
            {
                var transactions = await TxUtils.ProcessRequest(tipRequest, _dbContext);



                if (transactions != null && transactions.Length > 0)
                {
                    var tx = transactions[0];
                    var to = await _dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == tx.To.UserId);

                    var from = await _dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == tx.From.UserId);
                    UpdateStats.Tip(from, tipRequest.Amount);
                    UpdateStats.Tipped(to, tipRequest.Amount);
                    await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                    await TxUtils.AppendTxs(transactions, _dbContext, tipRequest.Columns);
                    
                }
                return transactions;
            }
            catch(Exception e)
            {
                throw e;
            }
     
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
