using Vromonsathi.ViewModels;

namespace Vromonsathi.Services
{
    public interface IChatBotService
    {
        Task<string> AskAsync(string message, List<ChatTurn> history, int? currentUserId, string? role);
    }
}