using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Stats;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;

namespace BITCORNService.Controllers
{
    [LockUser]
    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        [HttpPost("{rain}")]
        public async Task Rain([FromBody] IEnumerable<TxUser> txUsers)
        {
            //array of {amount, id}
            using (var dbContext = new BitcornContext())
            {
                foreach (var txUser in txUsers )
                {
                    var userIdentity = await dbContext.TwitchAsync(txUser.Id);
                    var userWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == userIdentity.UserId);
                    await UpdateStats.RainedOn(userIdentity.UserId, txUser.amount);
                    if (userWallet != null) userWallet.Balance += txUser.amount;

                }

                await dbContext.SaveAsync();
            }

            


            //recipient response TODO
        }

        [HttpPost("{payout}")]
        public void Payout([FromBody] string value)
        {
            //array of {amount, id}

            //senderresponses TODO
        }

        [HttpPost("{tipcorn}")]
        public void Tipcorn([FromBody] string value)
        {
            //sender twitchid, receiver twitchid, amount

            //senderresponse TODO
        }

        [HttpPost("{withdraw}")]
        public void Withdraw([FromBody] string value)
        {
            //sender twitchid, cornaddy, amount

            //tx id TODO
        }



    }
}
