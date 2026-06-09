using MessagingAPI.Data;
using MessagingAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MessagingAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendGroupMessage(int groupId, string senderName, string messageContent)
        {
            try 
            {
                // 1. Arayüzden gelen ismin veritabanındaki gerçek kimliğini (Id) buluyoruz
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == senderName);
                
                if (user != null)
                {
                    // 2. Mesajı SQL'e kaydederken isim yerine o gerçek 'Id' numarasını veriyoruz
                    var groupMessage = new GroupMessage 
                    { 
                        GroupId = groupId, 
                        SenderId = user.Id, // Hatayı kökten çözen atama!
                        Content = messageContent, 
                        SentAt = DateTime.Now 
                    };
                    
                    _context.GroupMessages.Add(groupMessage);
                    await _context.SaveChangesAsync();
                }
            } 
            catch (Exception ex)
            { 
                // Eğer bir hata olursa en azından terminalde görelim
                Console.WriteLine("Mesaj Kaydedilemedi: " + ex.Message);
            }

            // 3. Mesaj başarıyla kaydedildikten sonra gruptaki herkesin ekranına (isimle birlikte) yansıtıyoruz
            await Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupMessage", groupId, senderName, messageContent);
        }

        public async Task JoinGroup(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
        }
    }
}