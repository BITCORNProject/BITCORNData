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
using BITCORNService.Utils.Tx;
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
            await TxUtils.ExecuteCreditTxs(txUsers);


            //recipient response TODO
        }

        [HttpPost("{payout}")]
        public async Task Payout([FromBody] IEnumerable<TxUser> txUsers)
        {
            //array of {amount, id}
            await TxUtils.ExecuteCreditTxs(txUsers);
            //senderresponses TODO
        }

        [HttpPost("{tipcorn}")]
        public async Task Tipcorn([FromBody] TxUser txUser)
        {
            //sender twitchid, receiver twitchid, amount
            await TxUtils.ExecuteCreditTx(txUser);

            //senderresponse TODO
        }

        [HttpPost("{withdraw}")]
        public async Task Withdraw([FromBody] WithdrawUser withdrawUser)
        {
            //sender twitchid, cornaddy, amount
            await TxUtils.ExecuteDebitTx(withdrawUser);

            //call to wallet to properly withdraw TODO

            //tx id TODO
        }



    }
}
