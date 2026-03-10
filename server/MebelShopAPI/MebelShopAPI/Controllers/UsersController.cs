using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MebelShop.Helpers;
using MebelShopAPI.DTOs.Auth;
using MebelShopAPI.Helpers;
using MebelShopAPI.Models;

namespace MebelShopAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly MebelShopContext _context;

        public UsersController(MebelShopContext context)
        {
            _context = context;
        }

        [HttpGet("Current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var currentUser = await GetUser();
            if (currentUser == null)
                return Unauthorized();

            var userDto = await _context.Users
                .Where(u => u.IdUser == currentUser.IdUser)
                .Select(u => new
                {
                    FirstName = u.UserProfile.FirstName ?? "",
                    LastName = u.UserProfile.Surname ?? ""
                })
                .FirstOrDefaultAsync();

            if (userDto == null)
                return NotFound();

            return Ok(userDto);
        }

        [HttpGet("CheckoutInfo")]
        public async Task<IActionResult> GetCheckoutInfo()
        {
            var user = await GetUser();
            if (user == null)
                return Unauthorized();

            // Загружаем профиль и платежную информацию
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == user.IdUser);

            var payment = await _context.UserPayments
                .FirstOrDefaultAsync(p => p.UserId == user.IdUser);

            var dto = new UserCheckoutDto
            {
                FirstName = userProfile?.FirstName ?? "",
                LastName = userProfile?.Surname ?? "",
                DeliveryType = "Самовывоз",
                DeliveryAddress = "",
                PaymentType = "Оплата при получении",
                CardNumber = payment != null ? CryptoHelper.Decrypt(payment.CardNumber) : "",
                Expiry = payment != null ? CryptoHelper.Decrypt(payment.Expiry) : "",
                Cvv = payment != null ? CryptoHelper.Decrypt(payment.CVV) : ""
            };

            var lastOrder = await _context.Orders
                .Where(o => o.UserId == user.IdUser)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOrder != null)
            {
                dto.DeliveryType = lastOrder.DeliveryType;
                dto.DeliveryAddress = lastOrder.DeliveryAddress;
                dto.PaymentType = lastOrder.PaymentType;
            }

            return Ok(dto);
        }


        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserDto dto)
        {
            if (dto == null)
                return BadRequest("Данные не переданы");

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);

            if (user == null)
                return Unauthorized();

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile
                {
                    UserId = user.IdUser
                };
                _context.UserProfiles.Add(user.UserProfile);
            }

            user.UserProfile.FirstName = dto.FirstName?.Trim();
            user.UserProfile.Surname = dto.LastName?.Trim();

            await _context.SaveChangesAsync();

            return Ok(new ApiResponseDto
            {
                Success = true,
                Message = "Профиль успешно обновлён"
            });
        }

        public class ThemeDto
        {
            public string Theme { get; set; } = string.Empty;
        }

        [HttpPost("update-theme")]
        public async Task<IActionResult> UpdateTheme([FromBody] ThemeDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Theme))
                return BadRequest("Тема не передана");

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);

            if (user == null)
                return Unauthorized();

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = user.IdUser };
                _context.UserProfiles.Add(user.UserProfile);
            }

            user.UserProfile.Theme = dto.Theme.Trim();

            await _context.SaveChangesAsync();

            return Ok(new ApiResponseDto
            {
                Success = true,
                Message = "Тема успешно обновлена"
            });
        }

        [HttpGet("current-theme")]
        public async Task<IActionResult> GetCurrentTheme()
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);

            if (user == null)
                return Unauthorized();

            string theme = user.UserProfile?.Theme ?? "Light";

            return Ok(new { Theme = theme });
        }

        [HttpGet("PaymentInfo")]
        public async Task<IActionResult> GetPaymentInfo()
        {
            var user = await GetUser();
            if (user == null)
                return Unauthorized();

            var payment = await _context.UserPayments
                .FirstOrDefaultAsync(p => p.UserId == user.IdUser);

            if (payment == null)
            {
                return Ok(new UserPaymentDto
                {
                    CardNumber = "",
                    Expiry = "",
                    Cvv = ""
                });
            }

            var dto = new UserPaymentDto
            {
                CardNumber = CryptoHelper.Decrypt(payment.CardNumber),
                Expiry = CryptoHelper.Decrypt(payment.Expiry),
                Cvv = CryptoHelper.Decrypt(payment.CVV)
            };

            return Ok(dto);
        }

        [HttpPost("PaymentInfo")]
        public async Task<IActionResult> SavePaymentInfo([FromBody] UserPaymentDto dto)
        {
            var user = await GetUser();
            if (user == null)
                return Unauthorized();

            var entity = await _context.UserPayments
                .FirstOrDefaultAsync(p => p.UserId == user.IdUser);

            if (entity == null)
            {
                entity = new UserPayment
                {
                    UserId = user.IdUser
                };
                _context.UserPayments.Add(entity);
            }

            entity.CardNumber = CryptoHelper.Encrypt(dto.CardNumber);
            entity.Expiry = CryptoHelper.Encrypt(dto.Expiry);
            entity.CVV = CryptoHelper.Encrypt(dto.Cvv);

            await _context.SaveChangesAsync();

            return Ok(true);
        }

        private async Task<User?> GetUser()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        [Authorize(Roles = "Директор")]
        [HttpGet("list")]
        public async Task<IActionResult> GetUsers()
        {
            var currentUser = await GetUser();
            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Where(u => u.IdUser != currentUser.IdUser)
                .OrderBy(u => u.IdUser)
                .Select(u => new
                {
                    u.IdUser,
                    u.Email,
                    Role = u.Role.Name,
                    u.UserProfile.FirstName,
                    u.UserProfile.Surname
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Roles = "Директор")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdUser == id);

            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var currentUser = await GetUser();

            user.Email = dto.Email ?? user.Email;
            user.UserProfile.FirstName = dto.FirstName ?? user.UserProfile.FirstName;
            user.UserProfile.Surname = dto.Surname ?? user.UserProfile.Surname;

            if (!string.IsNullOrEmpty(dto.Role))
            {
                var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role);
                if (newRole == null)
                    return BadRequest(new { message = $"Роль '{dto.Role}' не найдена" });

                user.Role = newRole;
            }

            if (!string.IsNullOrEmpty(dto.Password))
                user.PasswordHash = HashHelper.ComputeSha256Hash(dto.Password);

            await _context.SaveChangesAsync();

            await AuditLogger.LogAsync(_context, currentUser?.IdUser, $"Директор {currentUser?.Email} обновил пользователя {user.Email}.");

            return Ok(new { message = "Пользователь обновлён" });
        }

        [Authorize(Roles = "Директор")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Пользователь с таким email уже существует" });

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role);
            if (role == null)
                return BadRequest(new { message = $"Роль '{dto.Role}' не найдена" });

            var newUser = new User
            {
                Email = dto.Email,
                PasswordHash = HashHelper.ComputeSha256Hash(dto.Password),
                Role = role,
                UserProfile = new UserProfile
                {
                    FirstName = dto.FirstName,
                    Surname = dto.Surname
                }
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var currentUser = await GetUser();
            await AuditLogger.LogAsync(_context, currentUser?.IdUser,
                $"Директор {currentUser?.Email} создал нового пользователя {newUser.Email} с ролью {role.Name}");

            return Ok(new
            {
                message = "Пользователь успешно создан",
                idUser = newUser.IdUser
            });
        }

        [Authorize(Roles = "Директор")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.IdUser == id);

            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            int reviewsCount = await _context.Reviews.CountAsync(r => r.UserId == id);
            int ordersCount = await _context.Orders.CountAsync(o => o.UserId == id);
            int cartCount = await _context.CartItems.CountAsync(c => c.UserId == id);
            int paymentsCount = await _context.UserPayments.CountAsync(p => p.UserId == id);
            int auditCount = await _context.AuditLogs.CountAsync(a => a.UserId == id);
            int codesCount = await _context.EmailCodes.CountAsync(c => c.UserId == id);

            int totalRelations = reviewsCount + ordersCount + cartCount + paymentsCount + auditCount + codesCount;

            return Ok(new
            {
                confirm = true,
                message = $"Пользователь '{user.Email}' будет удалён.\n" +
                          $"Также будут удалены:\n" +
                          $"- Отзывов: {reviewsCount}\n" +
                          $"- Заказов: {ordersCount}\n" +
                          $"- Товаров в корзине: {cartCount}\n" +
                          $"- Платёжных данных: {paymentsCount}\n" +
                          $"- Логов: {auditCount}\n" +
                          $"- Код подтверждения: {codesCount}\n\n" +
                          $"Всего связанных записей: {totalRelations}"
            });
        }

        [Authorize(Roles = "Директор")]
        [HttpDelete("confirm-delete/{id}")]
        public async Task<IActionResult> ConfirmDeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.IdUser == id);

            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            // Удаляем зависимости вручную
            var reviews = _context.Reviews.Where(r => r.UserId == id);
            var cartItems = _context.CartItems.Where(c => c.UserId == id);
            var payments = _context.UserPayments.Where(p => p.UserId == id);
            var logs = _context.AuditLogs.Where(a => a.UserId == id);
            var codes = _context.EmailCodes.Where(e => e.UserId == id);

            var orders = _context.Orders
                .Where(o => o.UserId == id)
                .Select(o => o.IdOrder)
                .ToList();

            var orderItems = _context.OrderItems.Where(oi => orders.Contains(oi.OrderId));
            _context.OrderItems.RemoveRange(orderItems);
            _context.Orders.RemoveRange(_context.Orders.Where(o => o.UserId == id));

            _context.Reviews.RemoveRange(reviews);
            _context.CartItems.RemoveRange(cartItems);
            _context.UserPayments.RemoveRange(payments);
            _context.AuditLogs.RemoveRange(logs);
            _context.EmailCodes.RemoveRange(codes);

            if (user.UserProfile != null)
                _context.UserProfiles.Remove(user.UserProfile);

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            var currentUser = await GetUser();
            await AuditLogger.LogAsync(_context, currentUser?.IdUser,$"Директор {currentUser?.Email} удалил пользователя {user.Email} и все связанные данные");

            return Ok(new { message = $"Пользователь '{user.Email}' и все связанные данные удалены." });
        }

        public class CreateUserDto
        {
            public string Email { get; set; }
            public string Role { get; set; }
            public string FirstName { get; set; }
            public string Surname { get; set; }
            public string Password { get; set; }
        }

        public class UpdateUserDto
        {
            public string? Email { get; set; }
            public string? Role { get; set; }
            public string? FirstName { get; set; }
            public string? Surname { get; set; }
            public string? Password { get; set; }
        }

        public class UserCheckoutDto
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }

            public string DeliveryType { get; set; }
            public string DeliveryAddress { get; set; }

            public string PaymentType { get; set; }
            public string CardNumber { get; set; }
            public string Expiry { get; set; }
            public string Cvv { get; set; }
        }

        public class UserPaymentDto
        {
            public string CardNumber { get; set; }
            public string Expiry { get; set; }
            public string Cvv { get; set; }
        }

        public class UserDto
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}
