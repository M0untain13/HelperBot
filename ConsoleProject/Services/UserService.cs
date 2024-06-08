using ConsoleProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleProject.Services
{
    public class UserService
    {
        private readonly ApplicationContext _context;

        public UserService(ApplicationContext context)
        {
            _context = context;
        }
        
        public string? GetUserRole(long telegramId)
        {
            var access = _context.Accesses
                .Include(a => a.Position)
                .FirstOrDefault(a => a.TelegramId == telegramId);
        
            return access?.Position.Name;
        }
    }
}