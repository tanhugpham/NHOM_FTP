using FileTransfer.Server.Database;
using FileTransfer.Server.Entities;
using FileTransfer.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace FileTransfer.Server.Services
{
    public class AuthService
    {
        public async Task<BaseResponseDto> RegisterAsync(string username, string password)
        {
            using (var db = new AppDbContext())
            {
                bool exists = await db.Users.AnyAsync(u => u.Username == username);

                if (exists)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Username đã tồn tại"
                    };
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                var user = new User
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.Now
                };

                db.Users.Add(user);

                await db.SaveChangesAsync();

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Đăng ký thành công"
                };
            }
        }

        public async Task<BaseResponseDto> LoginAsync(string username, string password, string clientIp)
        {
            using (var db = new AppDbContext())
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return new BaseResponseDto { Success = false, Message = "Tài khoản không tồn tại" };

                bool passwordOk = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (!passwordOk)
                    return new BaseResponseDto { Success = false, Message = "Sai mật khẩu" };

                // Lưu session
                var session = new ClientSession
                {
                    UserId = user.Id,
                    ClientIp = clientIp,
                    ConnectedAt = DateTime.Now,
                    IsOnline = true
                };
                db.ClientSessions.Add(session);
                await db.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = "Đăng nhập thành công" };
            }
        }
    }
}