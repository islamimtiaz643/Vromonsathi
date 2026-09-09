namespace Vromonsathi.Helpers
{
    public static class SeatMapHelper
    {
        // Generates seat codes in a 2+2 layout: A1, A2, [aisle], A3, A4, then B1... etc.
        public static List<string> GenerateSeatCodes(int totalSeats)
        {
            var seats = new List<string>();
            int seatsPerRow = 4;
            int rows = (int)Math.Ceiling(totalSeats / (double)seatsPerRow);
            char rowLetter = 'A';

            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c <= seatsPerRow; c++)
                {
                    if (seats.Count >= totalSeats) break;
                    seats.Add($"{rowLetter}{c}");
                }
                rowLetter++;
            }
            return seats;
        }
    }
}