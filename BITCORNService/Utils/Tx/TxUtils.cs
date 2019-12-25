using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Stats;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils.Tx
{
    public static class TxUtils
    {
        public static async Task AppendTxs(TxReceipt[] transactions, BitcornContext dbContext, string[] appendColumns)
        {
            if (appendColumns.Length > 0)
            {
                int[] participants = new int[transactions.Length + 1];
                participants[0] = transactions[0].From.UserId;
                for (int i = 0; i < transactions.Length; i++)
                {
                    participants[i + 1] = transactions[i].To.UserId;
                }
                var columns = await UserReflection.GetColumns(dbContext, appendColumns, participants);
                var fromColumns = columns[participants[0]];
                foreach (var tx in transactions)
                {
                    foreach (var from in fromColumns)
                    {
                        tx.From.Add(from.Key, from.Value);
                    }
                    foreach (var to in columns[tx.To.UserId])
                    {
                        tx.To.Add(to.Key, to.Value);
                    }
                }
            }
        }
     
        public static async Task<TxReceipt[]> ProcessRequest(ITxRequest req, BitcornContext dbContext)
        {
           // Dictionary<string, User> loadedUsers = new Dictionary<string, User>();
            HashSet<PlatformId> platformIds = new HashSet<PlatformId>();

            var fromPlatformId = BitcornUtils.GetPlatformId(req.From);
         
            var fromUser = await BitcornUtils.GetUserForPlatform(fromPlatformId,dbContext).FirstOrDefaultAsync();
            if (fromUser == null)
                return null;
            if (fromUser.IsBanned)
                return null;
            
            var toArray = req.To.ToArray();
         
            decimal totalAmountRequired = toArray.Length * req.Amount;
            if (fromUser.UserWallet.Balance < totalAmountRequired)
                return null;
            
            foreach (var to in toArray)
            {
                var toPlatformId = BitcornUtils.GetPlatformId(to);
                if (!platformIds.Contains(toPlatformId))
                {
                    platformIds.Add(toPlatformId);
                }
            }
            
            var users = await BitcornUtils.GetUsersForPlatform(platformIds.ToArray(),dbContext).ToArrayAsync();
           
            var output = new List<TxReceipt>();
         
            foreach (var user in users)
            {
                var receipt = new TxReceipt();
                receipt.From = new SelectableUser(fromUser.UserId);
                receipt.To = new SelectableUser(user.UserId);
                receipt.Tx = MoveCorn(fromUser, user, req.Amount, req.Platform,req.TxType);
            
                if (receipt.Tx != null)
                {
                    dbContext.CornTx.Add(receipt.Tx);
                }
                output.Add(receipt);
            }
           
            return output.ToArray();
        }
       
        public static CornTx MoveCorn(User from, User to, decimal amount,string platform,string txType)
        {
            if (from.IsBanned || to.IsBanned)
                return null;
            if (from.UserWallet.Balance >= amount)
            {
                from.UserWallet.Balance -= amount;
                to.UserWallet.Balance += amount;

                var tx = new CornTx();
                tx.TxType = txType;
                tx.Amount = amount;
                tx.ReceiverId = to.UserId;
                tx.SenderId = from.UserId;
                tx.Timestamp = DateTime.Now;
                tx.Platform = platform;
                return tx;
            }
            return null;
        }
      
    }
}
