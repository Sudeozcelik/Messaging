namespace MessagingAPI.Models
{
    public class UserGroup
    {
        public int Id { get; set; } // Sınırı aşmamak için eklediğimiz yeni anahtar
        
        public string UserId { get; set; }
        public AppUser User { get; set; }

        public int GroupId { get; set; }
        public MessageGroup Group { get; set; }
    }
}