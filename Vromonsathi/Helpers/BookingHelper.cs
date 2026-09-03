using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.Models;

namespace Vromonsathi.Helpers
{
    public static class BookingHelper
    {
        public static async Task<int> GetBookedSlotsAsync(ApplicationDbContext context, int packageId)
        {
            return await context.Bookings
                .Where(b => b.TourPackageId == packageId && (b.Status == "Pending" || b.Status == "Confirmed"))
                .SumAsync(b => (int?)b.NumberOfPeople) ?? 0;
        }
    }
}