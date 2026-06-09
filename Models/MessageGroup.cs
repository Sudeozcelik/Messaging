using System.Collections.Generic;

namespace MessagingAPI.Models
{
    public class MessageGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AdminId { get; set; } // Grubu kuran yönetici
        public AppUser Admin { get; set; }

        public ICollection<UserGroup> UserGroups { get; set; }
        public ICollection<GroupMessage> GroupMessages { get; set; }
    }
}