using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
using Vromonsathi.ViewModels;

namespace Vromonsathi.Services
{
    public class ChatBotService : IChatBotService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ChatBotService(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<string> AskAsync(string message, List<ChatTurn> history, int? currentUserId, string? role)
        {
            var apiKey = _config["Groq:ApiKey"];
            var model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("PASTE_YOUR"))
                return "The chatbot isn't configured yet — an admin needs to add a Groq API key in appsettings.json.";

            var context = await BuildLiveContextAsync(currentUserId, role);

            var systemPrompt = $@"You are the Vromonsathi assistant, a helpful travel planning assistant for a Bangladesh tourism booking platform.
Answer questions using ONLY the live data below when relevant. Be concise, friendly, and use ৳ for currency.
If asked about something not covered in the data (e.g. general travel tips), answer helpfully using your own knowledge, but clearly say if you're not certain about platform-specific details like exact prices.
Never invent destination names, prices, or booking statuses that aren't in the data below.

LIVE PLATFORM DATA:
{context}";

            var messages = new List<object> { new { role = "system", content = systemPrompt } };
            foreach (var turn in history.TakeLast(10))
                messages.Add(new { role = turn.Role, content = turn.Content });
            messages.Add(new { role = "user", content = message });

            var payload = new
            {
                model,
                messages,
                temperature = 0.4,
                max_tokens = 500
            };

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.groq.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("openai/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Sorry, the assistant is temporarily unavailable ({(int)response.StatusCode}). Please try again shortly.";
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return reply ?? "Sorry, I couldn't generate a response.";
        }

        private async Task<string> BuildLiveContextAsync(int? currentUserId, string? role)
        {
            var sb = new StringBuilder();

            var destinations = await _context.Destinations
                .Where(d => d.IsApproved)
                .Select(d => new { d.Name, d.District, d.Division, d.EntryFee, d.BestTimeToVisit })
                .Take(20)
                .ToListAsync();

            sb.AppendLine("Destinations:");
            foreach (var d in destinations)
                sb.AppendLine($"- {d.Name} ({d.District}, {d.Division}): entry fee ৳{d.EntryFee}, best time: {d.BestTimeToVisit}");

            var packages = await _context.TourPackages
                .Include(p => p.Destination)
                .Where(p => p.IsActive)
                .ToListAsync();

            sb.AppendLine("\nTour Packages:");
            foreach (var p in packages)
            {
                var booked = await Vromonsathi.Helpers.BookingHelper.GetBookedSlotsAsync(_context, p.Id);
                var remaining = Math.Max(p.MaxGroupSize - booked, 0);
                sb.AppendLine($"- '{p.Title}' ({p.Destination?.Name ?? "multi-destination"}): ৳{p.Price} per person, {p.DurationDays} days, {remaining} of {p.MaxGroupSize} spots left, advance required ৳1500 per person");
            }

            var facilities = await _context.Facilities.ToListAsync();
            sb.AppendLine("\nFacility baseline rates:");
            foreach (var f in facilities)
                sb.AppendLine($"- {f.NameEn}: ৳{f.DefaultPrice} ({f.Unit})");

            if (currentUserId != null && role == "Tourist")
            {
                var bookings = await _context.Bookings
                    .Include(b => b.TourPackage)
                    .Include(b => b.Listing)
                    .Where(b => b.TouristUserId == currentUserId)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                var user = await _context.Users.FindAsync(currentUserId);

                sb.AppendLine($"\nThis traveler's wallet balance: ৳{user?.WalletBalance ?? 0}");
                sb.AppendLine("This traveler's recent bookings:");
                if (bookings.Any())
                {
                    foreach (var b in bookings)
                    {
                        var title = b.TourPackage?.Title ?? b.Listing?.Title ?? "Unknown";
                        sb.AppendLine($"- '{title}': status {b.Status}, {b.NumberOfPeople} people, total ৳{b.TotalPrice}, advance paid: {b.AdvancePaid}");
                    }
                }
                else
                {
                    sb.AppendLine("- No bookings yet.");
                }
            }
            else
            {
                sb.AppendLine("\n(This visitor is not logged in as a Tourist, so no personal booking data is available. If they ask about their bookings, tell them to log in.)");
            }

            return sb.ToString();
        }
    }
}