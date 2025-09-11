using System.ComponentModel.DataAnnotations;

namespace ChatQueue.API.Models.Chat
{
    public class ChatSessionResponse
    {
        [Required]
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
