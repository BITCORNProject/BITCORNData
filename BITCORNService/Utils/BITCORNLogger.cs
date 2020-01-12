using System;
using System.Threading.Tasks;
using BITCORNService.Models;

namespace BITCORNService.Utils
{
    public static class BITCORNLogger 
    {
        public static async Task<ErrorLogs> LogError(BitcornContext dbContext, Exception e, string code = null)
        {

            var logEntry = new ErrorLogs();
            logEntry.Message = e.Message;
            logEntry.Application = "BITCORNService";
            logEntry.Code = code;
            logEntry.StackTrace = e.StackTrace;
            logEntry.Timestamp = DateTime.Now;
            dbContext.ErrorLogs.Add(logEntry);
            await dbContext.SaveChangesAsync();

            return logEntry;

        }
    }
}
