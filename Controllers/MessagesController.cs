using MessagingAPI.Data;
using MessagingAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    
    {

        public class AddMemberDto
{  
    public int GroupId { get; set; }
    public string Username { get; set; }
}
// Uygulama çalışırken üye kayıtlarını hafızada tutacak sihirli sözlük
private static readonly Dictionary<int, List<string>> _groupMembers = new();

[HttpPost("AddMember")]
public IActionResult AddMember([FromBody] AddMemberDto request)
{
    if (!_groupMembers.ContainsKey(request.GroupId))
    {
        _groupMembers[request.GroupId] = new List<string>();
    }
    
    if (!_groupMembers[request.GroupId].Contains(request.Username))
    {
        _groupMembers[request.GroupId].Add(request.Username);
    }
    
    return Ok("Kullanıcı başarıyla gruba dahil edildi.");
}
        private readonly AppDbContext _context;

        public MessagesController(AppDbContext context)
        {
            _context = context;
        }

      
        public class CreateGroupDto
        {
            public string Name { get; set; }
            public string AdminId { get; set; }
        }

   
        [HttpPost("CreateGroup")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto request)
        {
            try
            {
               
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.AdminId);
                
                if (user == null)
                {
                    return BadRequest("Sisteme kayıtlı böyle bir kullanıcı bulunamadı.");
                }

              
                var group = new MessageGroup 
                {
                    Name = request.Name,
                    AdminId = user.Id  
                }; 
                
                _context.MessageGroups.Add(group);
                await _context.SaveChangesAsync();
                
                return Ok(group);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("History/{user1Id}/{user2Id}")]
        public async Task<IActionResult> GetMessageHistory(string user1Id, string user2Id)
        {
            var messages = await _context.Messages
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) || 
                            (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("GroupHistory/{groupId}")]
        public async Task<IActionResult> GetGroupMessageHistory(int groupId)
        {
            try
            {
                var messages = await (from m in _context.GroupMessages
                                      where m.GroupId == groupId
                                      join u in _context.Users on m.SenderId equals u.Id
                                      orderby m.SentAt
                                      select new 
                                      {
                                          senderId = u.UserName, 
                                          content = m.Content
                                      }).ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return BadRequest("Geçmiş mesajlar yüklenemedi: " + ex.Message);
            }
        }

      
        [HttpDelete("DeleteGroup/{groupId}")]
public async Task<IActionResult> DeleteGroup(int groupId)
{
    var username = User.Identity?.Name;
    if (string.IsNullOrEmpty(username)) return Unauthorized("Giriş yapmalısınız!");

    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
    var group = await _context.MessageGroups.FindAsync(groupId);

    if (group == null) return NotFound("Grup bulunamadı.");

    if (groupId == 1) return BadRequest("Sistem odası silinemez!");
    if (group.AdminId != user.Id) return Forbid("Sadece kendi oluşturduğunuz grubu silebilirsiniz!");

    _context.MessageGroups.Remove(group);
    await _context.SaveChangesAsync();
    return Ok("Grup başarıyla silindi.");
}

        [HttpDelete("DeleteMessage/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound("Silinecek mesaj bulunamadı.");

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            return Ok("Mesaj kalıcı olarak silindi.");
        }

        [HttpGet("MyGroups/{username}")]
public async Task<IActionResult> GetMyGroups(string username)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
    if (user == null) return BadRequest("Kullanıcı bulunamadı");

    // Veritabanındaki tüm grupları çekiyoruz
    var allGroups = await _context.MessageGroups.ToListAsync();

    // Filtreleme: Ortak Oda (Id=1) VEYA Kurucusu olduğumuz oda VEYA hafızada üyesi olduğumuz oda!
    var filteredGroups = allGroups.Where(g => 
        g.Id == 1 || 
        g.AdminId == user.Id || 
        (_groupMembers.ContainsKey(g.Id) && _groupMembers[g.Id].Contains(username))
    ).ToList();

    return Ok(filteredGroups);
}
    }
}