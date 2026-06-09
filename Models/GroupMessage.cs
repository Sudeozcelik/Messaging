using System;

namespace MessagingAPI.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }
        
        public int GroupId { get; set; }
        public MessageGroup Group { get; set; }

        public string SenderId { get; set; }
        public AppUser Sender { get; set; }

        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}