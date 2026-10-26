namespace WAPP_EduCore.Models
{
    public class ChatTurn
    {
        public string Role { get; set; }   // "user" or "model"
        public string Text { get; set; }
    }
}