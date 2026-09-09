using Microsoft.AspNetCore.Mvc;
using Vromonsathi.Services;
using Vromonsathi.ViewModels;

namespace Vromonsathi.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly IChatBotService _chatBot;

        public ChatBotController(IChatBotService chatBot)
        {
            _chatBot = chatBot;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest("Message is required.");

            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            var reply = await _chatBot.AskAsync(request.Message, request.History ?? new List<ChatTurn>(), userId, role);

            return Json(new ChatResponse { Reply = reply });
        }
    }
}