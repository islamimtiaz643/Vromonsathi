using Vromonsathi.Data;
using Vromonsathi.Models;

namespace Vromonsathi.Helpers
{
    public static class WalletHelper
    {
        public static bool HasSufficientBalance(User user, decimal amount) => user.WalletBalance >= amount;

        public static void Debit(ApplicationDbContext context, User user, decimal amount, string type, int? bookingId, string note)
        {
            user.WalletBalance -= amount;
            context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = user.Id,
                Amount = amount,
                Type = type,
                Status = "Completed",
                BookingId = bookingId,
                ReceiptNote = note
            });
        }

        public static void Credit(ApplicationDbContext context, User user, decimal amount, string type, int? bookingId, string note)
        {
            user.WalletBalance += amount;
            context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = user.Id,
                Amount = amount,
                Type = type,
                Status = "Completed",
                BookingId = bookingId,
                ReceiptNote = note
            });
        }
    }
}