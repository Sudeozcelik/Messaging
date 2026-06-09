using System;

namespace MessagingAPI.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        public string SenderId { get; set; }
        public AppUser Sender { get; set; }

        public string ReceiverId { get; set; }
        public AppUser Receiver { get; set; }

        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}