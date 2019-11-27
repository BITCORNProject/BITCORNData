using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;

namespace BITCORNService.Utils
{
    public static class BITCORNLogger 
    {
        public static async Task LogError(Exception e, string code = null)
        {
            using (var dbContext = new BitcornContext())
            {
                var logEntry = new ErrorLogs();
                logEntry.Message = e.Message;
                logEntry.Application = "BITCORNService";
                logEntry.Code = code;
                logEntry.StackTrace = e.StackTrace;
                logEntry.Timestamp = DateTime.Now;
                dbContext.ErrorLogs.Add(logEntry);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
