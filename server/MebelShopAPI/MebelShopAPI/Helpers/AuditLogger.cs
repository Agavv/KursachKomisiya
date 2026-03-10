using MebelShopAPI.Models;

namespace MebelShop.Helpers
{
    public static class AuditLogger
    {
        public static async Task LogAsync(MebelShopContext context, int? userId, string description)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Description = description,
                    CreatedAt = DateTime.Now
                };

                context.AuditLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи лога: {ex.Message}");
            }
        }
    }
}
