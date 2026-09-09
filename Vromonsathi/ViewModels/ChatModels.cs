namespace Vromonsathi.ViewModels
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatTurn> History { get; set; } = new();
    }

    public class ChatTurn
    {
        public string Role { get; set; } = "user"; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;
    }
}