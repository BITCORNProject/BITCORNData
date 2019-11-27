using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.LockUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BITCORNService.Controllers
{
    [LockUser]
    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        [HttpPost("{rain}")]
        public void Rain([FromBody] string value)
        {

        }

        [HttpPost("{tipcorn}")]
        public void Tipcorn([FromBody] string value)
        {

        }
        async Task HandleAnalytics(SendFromRequest request, decimal amount)
        {
            using (var db = new BitcornContext())
            {
                var user = PrepareUser(db, request.GetRequestUser());
                user.Tips += 1;

                if (user.Tiptotal == 0)
                {
                    user.Tiptotal = amount;
                }
                else
                {
                    user.Tiptotal += amount;
                }

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    BITCORNLogger.LogError(e, $"Tip or Tiptotal update failed to save to Database ");
                }

            }
        }

        private Users PrepareUser(BitcornContext db, Users old)
        {
            var entry = db.Attach(old);

            entry.Property(e => e.Tips).IsModified = true;
            entry.Property(e => e.Tiptotal).IsModified = true;

            return old;
        }

    }
}
