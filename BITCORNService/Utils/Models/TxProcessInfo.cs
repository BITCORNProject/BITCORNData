using BITCORNService.Models;
using BITCORNService.Utils.Tx;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BITCORNService.Utils.Models
{
    public class TxProcessInfo
    {
        public TxReceipt[] Transactions { get; set; }
        public User From { get; set; }
        public decimal TotalAmount { get; set; }
        public int[] ValidRecipients { get; set; }
        public bool WriteTransactionOutput(StringBuilder sql)
        {
            if (From == null) return false;

            decimal totalAmount = 0;
            decimal singleTx = 0;
            List<int> recipients = new List<int>();

            for (int i = 0; i < Transactions.Length; i++)
            {
                if (Transactions[i].Tx != null)
                {
                    totalAmount += Transactions[i].Tx.Amount.Value;
                    recipients.Add(Transactions[i].To.UserId);
                    singleTx = Transactions[i].Tx.Amount.Value;
                }
            }
            TotalAmount = totalAmount;
            ValidRecipients = recipients.ToArray();
            if (ValidRecipients.Length > 0)
            {
                sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), singleTx, '+', nameof(UserWallet.UserId), ValidRecipients));
                sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), totalAmount, '-', nameof(UserWallet.UserId), From.UserId));
                return true;
            }
            return false;
        }
    }
}
