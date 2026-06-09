using MessagingAPI.Data;
using MessagingAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Kimlik doğrulama kurallarını tamamen esnetiyoruz (Sude Özçelik yazabilmen için)
builder.Services.AddIdentity<AppUser, IdentityRole>(options => 
{
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ öçşığıÖÇŞİĞÜ";
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 3;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MessagingAPI.Hubs.ChatHub>("/chathub");
app.MapGet("/", () => Results.Content("""
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Toplu Mesajlaşma Platformu</title>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js"></script>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
        body { display: flex; height: 100vh; background-color: #f4f7f6; overflow: hidden; }
        
        .sidebar { width: 260px; background-color: #232d3f; color: #fff; display: flex; flex-direction: column; position: relative; }
        .brand { padding: 25px 20px; font-size: 18px; font-weight: bold; border-bottom: 1px solid rgba(255,255,255,0.1); display: flex; align-items: center; gap: 10px; }
        .brand i { background: #5c67f2; padding: 8px; border-radius: 8px; }
        .user-profile { padding: 20px; display: flex; align-items: center; gap: 15px; border-bottom: 1px solid rgba(255,255,255,0.1); }
        .user-profile img { width: 45px; height: 45px; border-radius: 50%; background: #ccc; }
        .user-info { display: flex; flex-direction: column; }
        .user-info span.name { font-weight: bold; font-size: 14px; text-transform: capitalize; }
        .user-info span.status { font-size: 12px; color: #4ade80; }
        .nav-menu { flex: 1; padding: 20px 0; overflow-y: auto; }
        .nav-item { padding: 15px 25px; display: flex; align-items: center; gap: 15px; color: #a1a1aa; text-decoration: none; transition: 0.3s; cursor: pointer; user-select: none; }
        .nav-item:hover, .nav-item.active { background-color: rgba(255,255,255,0.05); color: #fff; border-left: 4px solid #5c67f2; }
        .logout-btn { padding: 15px 25px; display: flex; align-items: center; gap: 15px; color: #ef4444; cursor: pointer; border-top: 1px solid rgba(255,255,255,0.1); transition: 0.3s; }
        .logout-btn:hover { background-color: rgba(239, 68, 68, 0.1); }

        .middle-panel { width: 320px; background: #fff; border-right: 1px solid #e5e7eb; display: flex; flex-direction: column; }
        .middle-header { padding: 20px; font-weight: bold; font-size: 18px; border-bottom: 1px solid #e5e7eb; }
        .search-bar { padding: 15px 20px; }
        .search-bar input { width: 100%; padding: 10px 15px; border: 1px solid #e5e7eb; border-radius: 8px; outline: none; background: #f9fafb; }
        .group-list { flex: 1; overflow-y: auto; }
        .group-item { padding: 15px 20px; display: flex; align-items: center; gap: 15px; cursor: pointer; transition: 0.2s; border-bottom: 1px solid #f3f4f6; }
        .group-item:hover, .group-item.active { background: #f3f4f6; border-left: 4px solid #5c67f2; }
        .group-icon { width: 45px; height: 45px; border-radius: 50%; background: #e0e7ff; color: #5c67f2; display: flex; align-items: center; justify-content: center; font-size: 20px; }
        
        .chat-panel { flex: 1; display: flex; flex-direction: column; background: #f9fafb; position: relative; }
        .chat-header { padding: 20px 30px; background: #fff; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center; }
        .chat-header h2 { font-size: 16px; color: #1f2937; }
        .header-actions { display: flex; gap: 20px; color: #6b7280; font-size: 18px; }
        .header-actions i { cursor: pointer; transition: 0.2s; }
        .header-actions i:hover { color: #5c67f2; }
        .chat-messages { flex: 1; padding: 30px; overflow-y: auto; display: flex; flex-direction: column; gap: 20px; }
        
        .msg-box { max-width: 60%; display: flex; flex-direction: column; }
        .msg-box.other { align-self: flex-start; }
        .msg-box.me { align-self: flex-end; align-items: flex-end; }
        
        /* Herkesin ismini göstermek için düzenlendi */
        .msg-sender { font-size: 12px; font-weight: bold; text-transform: capitalize; margin-bottom: 5px; }
        .msg-box.other .msg-sender { color: #6b7280; margin-left: 5px; }
        .msg-box.me .msg-sender { color: #818cf8; margin-right: 5px; }

        .msg-bubble { padding: 15px 20px; border-radius: 12px; font-size: 14px; line-height: 1.5; box-shadow: 0 1px 2px rgba(0,0,0,0.05); }
        .msg-box.other .msg-bubble { background: #fff; color: #1f2937; border-top-left-radius: 0; border: 1px solid #e5e7eb; }
        .msg-box.me .msg-bubble { background: #5c67f2; color: #fff; border-top-right-radius: 0; }
        
        .chat-input-area { padding: 20px 30px; background: #fff; border-top: 1px solid #e5e7eb; display: flex; gap: 15px; align-items: center; }
        .chat-input-area input { flex: 1; padding: 15px 20px; border: 1px solid #e5e7eb; border-radius: 10px; outline: none; font-size: 14px; background: #f9fafb; transition: 0.2s; }
        .chat-input-area input:focus { border-color: #5c67f2; background: #fff; }
        .send-btn { background: #5c67f2; color: white; border: none; padding: 15px 25px; border-radius: 10px; cursor: pointer; font-weight: bold; display: flex; align-items: center; gap: 10px; transition: 0.2s; }
        .send-btn:hover { background: #4a54c4; transform: translateY(-1px); }

        #loginOverlay { position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: rgba(35, 45, 63, 0.95); display: flex; align-items: center; justify-content: center; z-index: 1000; transition: opacity 0.3s; }
        .login-box { background: white; padding: 40px; border-radius: 15px; width: 400px; text-align: center; box-shadow: 0 10px 25px rgba(0,0,0,0.2); }
        .login-box i { font-size: 40px; color: #5c67f2; margin-bottom: 20px; }
        .login-box h3 { margin-bottom: 10px; color: #1f2937; }
        .login-box input { width: 100%; padding: 15px; border: 1px solid #d1d5db; border-radius: 8px; margin-bottom: 15px; outline: none; font-size: 14px; transition: 0.2s; }
        .auth-buttons { display: flex; gap: 10px; margin-top: 10px; }
        .auth-buttons button { flex: 1; padding: 15px; border: none; border-radius: 8px; cursor: pointer; font-weight: bold; font-size: 14px; transition: 0.2s; }
        .btn-login { background: #5c67f2; color: white; }
        .btn-register { background: #16a34a; color: white; }
    </style>
</head>
<body>

    <div id="loginOverlay">
        <div class="login-box">
            <i class="fa-solid fa-shield-halved"></i>
            <h3>Sisteme Giriş</h3>
            <input type="text" id="usernameInput" placeholder="Kullanıcı Adı (Örn: sude özçelik)">
            <input type="email" id="emailInput" placeholder="E-Posta (Örn: sude@test.com)">
            <input type="password" id="passwordInput" placeholder="Şifre (Örn: 123)">
            <div class="auth-buttons">
                <button class="btn-login" onclick="loginAPI()">Giriş Yap</button>
                <button class="btn-register" onclick="registerAPI()">Kayıt Ol</button>
            </div>
        </div>
    </div>

    <div class="sidebar">
        <div class="brand"><i class="fa-solid fa-comment-dots"></i> Mesaj Platformu</div>
        <div class="user-profile">
            <div class="user-info">
                <span class="name" id="displayUsername">Yükleniyor...</span>
                <span class="status">● Çevrimiçi</span>
            </div>
        </div>
       <div class="nav-menu">
            <a class="nav-item active"><i class="fa-solid fa-users"></i> Gruplar & Mesajlar</a>
            <a class="nav-item" onclick="createGroupAPI()"><i class="fa-solid fa-plus"></i> Yeni Grup Oluştur</a>
            <a class="nav-item" onclick="createPrivateChat()"><i class="fa-solid fa-user"></i> Kişiye Özel Mesaj</a>
        </div>
        <div class="logout-btn" onclick="logout()">
            <i class="fa-solid fa-arrow-right-from-bracket"></i> Çıkış Yap
        </div>
    </div>

    <div class="middle-panel">
        <div class="middle-header">Mesaj Grupları</div>
        <div class="search-bar"><input type="text" placeholder="Grup ara..."></div>
        <div class="group-list" id="groupListArea">
            </div>
    </div>

    <div class="chat-panel">
        <div class="chat-header">
            <h2 id="currentRoomTitle">Lütfen Bir Grup Seçin</h2>
            <div class="header-actions">
                <i class="fa-solid fa-user-plus" title="Gruba Üye Ekle" onclick="addMemberUI()"></i>
                <i class="fa-solid fa-trash" title="Grubu Sil" onclick="deleteGroupAPI()"></i>
            </div>
        </div>
        
        <div class="chat-messages" id="chatMessages">
            <div style="text-align:center; color:#9ca3af; font-size:14px; margin-top:50px;">Sohbete başlamak için sol taraftan bir grup seçin veya yeni bir grup oluşturun.</div>
        </div>

        <div class="chat-input-area">
            <input type="text" id="msgInput" placeholder="Gruba mesajınızı yazın..." onkeypress="handleEnter(event)">
            <button class="send-btn" onclick="sendMessage()">Gönder <i class="fa-solid fa-paper-plane"></i></button>
        </div>
    </div>

    <script>
        const conn = new signalR.HubConnectionBuilder().withUrl("/chathub").build();
        let currentUser = "";
        let currentGroupId = null; 

        window.onload = async () => {
            const savedUser = localStorage.getItem("chatAppUser");
            if (savedUser) {
                currentUser = savedUser;
                document.getElementById("displayUsername").innerText = currentUser;
                document.getElementById("loginOverlay").style.display = "none";
                
                await loadAllGroupsFromDB(); 
                try {
                    await conn.start();
                } catch(err) { console.log("SignalR Başlatılamadı:", err); }
            }
        };

  async function loadAllGroupsFromDB() {
            try {
                const res = await fetch(`/api/Messages/MyGroups/${currentUser}`);
                if (res.ok) {
                    const groups = await res.json();
                    const groupList = document.getElementById("groupListArea");
                    groupList.innerHTML = `
                        <div class="group-item" onclick="switchGroup(1, 'Tüm Üyeler (Bütünleme Ortak Alan)', this)">
                            <div class="group-icon"><i class="fa-solid fa-globe"></i></div>
                            <div><div style="font-weight: bold;">Tüm Üyeler (Bütünleme)</div><div style="font-size: 12px; color: #16a34a;">Sistem Odası</div></div>
                        </div>
                    `;

                    groups.forEach(g => {
                        if(g.id !== 1) {
                            // YENİ: Eğer grup adı "Özel Sohbet:" ile başlıyorsa ikonunu tekil kişi yapıyoruz
                            const isPrivate = g.name.startsWith("Özel Sohbet");
                            const icon = isPrivate ? '<i class="fa-solid fa-user"></i>' : '<i class="fa-solid fa-users"></i>';
                            const badgeTxt = isPrivate ? 'Birebir Mesaj' : 'Özel Grubunuz';

                            groupList.innerHTML += `
                                <div class="group-item" onclick="switchGroup(${g.id}, '${g.name}', this)">
                                    <div class="group-icon" style="background:#e0e7ff; color:#5c67f2;">${icon}</div>
                                    <div><div style="font-weight: bold;">${g.name}</div><div style="font-size: 12px; color: #5c67f2;">${badgeTxt}</div></div>
                                </div>
                            `;
                        }
                    });
                }
            } catch(e) { console.log("Gruplar çekilemedi."); }
        }

        // YENİ: Birebir mesajlaşma için otomatik 2 kişilik gizli oda kuran fonksiyon
        async function createPrivateChat() {
            const targetUser = prompt("Özel mesaj atmak istediğiniz kişinin kullanıcı adını girin:");
            if(!targetUser) return;
            if(targetUser.toLowerCase() === currentUser.toLowerCase()) return alert("Kendinize mesaj atamazsınız!");

            const dmName = `Özel Sohbet: ${targetUser}`;

            try {
                // 1. Adım: İki kişilik özel grubu kur
                const res = await fetch('/api/Messages/CreateGroup', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ name: dmName, adminId: currentUser })
                });

                if(res.ok) {
                    const createdGroup = await res.json();
                    
                    // 2. Adım: Karşı tarafı otomatik olarak bu gruba (odaya) davet et
                    await fetch('/api/Messages/AddMember', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ groupId: createdGroup.id, username: targetUser })
                    });

                    alert(`Harika! '${targetUser}' ile birebir özel mesajlaşma odanız oluşturuldu.`);
                    loadAllGroupsFromDB(); // Listeyi yenile
                } else {
                    alert("Kullanıcı bulunamadı veya işlem başarısız: " + await res.text());
                }
            } catch(e) {
                alert("Bağlantı hatası: " + e.message);
            }
        }

        async function switchGroup(id, name, element) {
            currentGroupId = id;
            document.querySelectorAll('.group-item').forEach(el => el.classList.remove('active'));
            element.classList.add('active');
            document.getElementById("currentRoomTitle").innerText = name;
            document.getElementById("chatMessages").innerHTML = '<div style="text-align:center; color:#9ca3af; font-size:12px; margin-top:10px;">Geçmiş Mesajlar Yükleniyor...</div>';
            
            try {
                if(conn.state !== "Connected") await conn.start();
                await conn.invoke("JoinGroup", currentGroupId);
            } catch(e) { console.log("Odaya girilemedi:", e); }
            
            await loadMessageHistory();
        }

        async function addMemberUI() {
            if(!currentGroupId) return alert("Önce sol taraftan bir grup seçmelisiniz!");
            if(currentGroupId === 1) return alert("Bu grup Sistem Odasıdır, herkes otomatik dahil olur. Manuel üye eklenemez.");
            
            const memberName = prompt("Bu gruba eklemek istediğiniz kişinin kullanıcı adını girin:");
            if(!memberName) return;

            try {
                const res = await fetch('/api/Messages/AddMember', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ groupId: currentGroupId, username: memberName })
                });

                if(res.ok) {
                    alert(`Harika! '${memberName}' adlı kullanıcı başarıyla '${document.getElementById("currentRoomTitle").innerText}' grubuna eklendi.`);
                } else {
                    alert("Üye eklenirken hata oldu: " + await res.text());
                }
            } catch(e) {
                alert("Bağlantı hatası: " + e.message);
            }
        }

        async function registerAPI() {
            const user = document.getElementById("usernameInput").value.trim();
            const email = document.getElementById("emailInput").value.trim();
            const pass = document.getElementById("passwordInput").value.trim();
            if(!user || !email || !pass) return alert("Alanları doldurun!");
            try {
                const res = await fetch('/api/Auth/Register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username: user, email: email, password: pass })
                });
                if(res.ok) alert("Kayıt başarılı. 'Giriş Yap' butonuna bas.");
                else alert("Kayıt hatası: " + await res.text());
            } catch(e) {}
        }

        async function loginAPI() {
            const user = document.getElementById("usernameInput").value.trim();
            const pass = document.getElementById("passwordInput").value.trim();
            if(!user || !pass) return alert("Kullanıcı adı ve şifre girin.");
            try {
                const res = await fetch('/api/Auth/Login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username: user, password: pass })
                });
                if(res.ok) {
                    localStorage.setItem("chatAppUser", user);
                    location.reload(); 
                } else alert("Giriş başarısız!");
            } catch(e) {}
        }

        async function loadMessageHistory() {
            try {
                const res = await fetch(`/api/Messages/GroupHistory/${currentGroupId}`);
                if(res.ok) {
                    const messages = await res.json();
                    document.getElementById("chatMessages").innerHTML = '<div style="text-align:center; color:#9ca3af; font-size:12px; margin-top:10px;">--- Geçmiş Mesajlar ---</div>';
                    messages.forEach(m => renderMessage(m.senderId, m.content));
                }
            } catch (e) { }
        }

        function logout() {
            localStorage.removeItem("chatAppUser");
            location.reload();
        }

        async function createGroupAPI() {
            const groupName = prompt("Oluşturmak istediğiniz grubun adını girin:");
            if(!groupName) return;
            
            const res = await fetch('/api/Messages/CreateGroup', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: groupName, adminId: currentUser })
            });
            
            if(res.ok) {
                alert(`Grup oluşturuldu! Sizi grubun Admini olarak atadık.`);
                loadAllGroupsFromDB(); 
            } else {
                alert("Grup oluşturulamadı. Hata: " + await res.text());
            }
        }

        async function deleteGroupAPI() {
            if(!currentGroupId) return alert("Lütfen silmek istediğiniz grubu seçin!");
            if(currentGroupId === 1) return alert("Sistem Odası silinemez!");
            
            if(!confirm("Dikkat! Grup silindiğinde içindeki tüm mesajlar da silinir. Emin misiniz?")) return;
            const res = await fetch(`/api/Messages/DeleteGroup/${currentGroupId}`, { method: 'DELETE' });
            if(res.ok) {
                alert("Grup ve içindeki mesajlar başarıyla silindi!");
                location.reload(); 
            } else if(res.status === 403) {
                alert("Yetki Hatası: Sadece grubun kurucusu (Admin) silme işlemi yapabilir!");
            } else {
                alert("Hata: " + await res.text());
            }
        }

        conn.on("ReceiveGroupMessage", (incomingGroupId, sender, text) => {
            if(incomingGroupId === currentGroupId) renderMessage(sender, text);
        });

        function renderMessage(sender, text) {
            const container = document.getElementById("chatMessages");
            const safeSender = String(sender).trim();
            const safeUser = String(currentUser).trim();
            const isMe = safeSender.localeCompare(safeUser, undefined, { sensitivity: 'accent' }) === 0;

            const msgHtml = `
                <div class="msg-box ${isMe ? 'me' : 'other'}">
                    ${!isMe ? `<span class="msg-sender">${safeSender}</span>` : `<span class="msg-sender" style="color:#c7d2fe; margin-right:5px;">Sen</span>`}
                    <div class="msg-bubble">${text}</div>
                </div>
            `;
            container.insertAdjacentHTML('beforeend', msgHtml);
            container.scrollTop = container.scrollHeight;
        }

        async function sendMessage() {
            if(!currentGroupId) return alert("Lütfen sol taraftan mesaj göndermek istediğiniz grubu seçin.");
            const input = document.getElementById("msgInput");
            const text = input.value.trim();
            if(!text) return;
            
            try {
                // MUHTEŞEM DETAY: Eğer sunucu kapanıp açıldığı için bağlantı koptuysa, kendi kendine tekrar bağlanır!
                if(conn.state !== "Connected") {
                    await conn.start();
                    await conn.invoke("JoinGroup", currentGroupId);
                }

                await conn.invoke("SendGroupMessage", currentGroupId, currentUser, text);
                input.value = "";
                input.focus();
            } catch(err) {
                // Hata olduğunda sessizce durmak yerine ekrana ne olduğunu basar.
                alert("Bağlantı hatası (Lütfen sayfayı yenileyin): " + err.message);
            }
        }

        function handleEnter(e) { if(e.key === "Enter") sendMessage(); }
    </script>
</body>
</html>
""", "text/html"));
app.Run();