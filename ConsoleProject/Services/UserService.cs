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

        public bool IsUserRegistered(long telegramId)
        {
            return _context.Employees.Any(e => e.TelegramId == telegramId);
        }
        
        public string? GetUserRole(long telegramId)
        {
            var access = _context.Accesses
                .Include(a => a.Position)
                .FirstOrDefault(a => a.TelegramId == telegramId);
        
            return access?.Position.Name;
        }

        public void RegisterUser(long telegramId, string login, string name, string surname)
        {
            var user = new Employee(telegramId, login, name, surname);
            var access = new Access(telegramId, 1);
            _context.Employees.Add(user);
            _context.Accesses.Add(access);
            _context.SaveChanges();
        }
    }
}