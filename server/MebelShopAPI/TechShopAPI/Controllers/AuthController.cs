using API.DTOs.Auth;
using API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.CodeDom.Compiler;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechShop.Helpers;
using TechShopAPI.DTOs.Auth;
using TechShopAPI.Helpers;
using TechShopAPI.Models;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace TechShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TechShopContext _context;
        private readonly IConfiguration _config;
        private static readonly Random _rand = new Random();

        public AuthController(TechShopContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        #region Авторизация
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            string rootFilePath = Path.Combine(Directory.GetCurrentDirectory(), "root.json");
            if (System.IO.File.Exists(rootFilePath))
            {
                try
                {
                    var json = await System.IO.File.ReadAllTextAsync(rootFilePath);
                    var rootData = System.Text.Json.JsonSerializer.Deserialize<RootAccount>(json);

                    if (rootData != null &&
                        dto.Email.Equals(rootData.Username, StringComparison.OrdinalIgnoreCase) &&
                        HashHelper.ComputeSha256Hash(dto.Password) == rootData.PasswordHash)
                    {
                        await AuditLogger.LogAsync(_context, null, $"Root-пользователь {dto.Email} успешно вошёл в систему.");

                        var emergencyToken = GenerateEmergencyToken(dto.Email);
                        return Ok(new
                        {
                            Token = emergencyToken,
                            Email = rootData.Username,
                            Role = "Администратор"
                        });
                    }
                    else if (rootData != null && dto.Email.Equals(rootData.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        await AuditLogger.LogAsync(_context, null, $"Неудачная попытка входа root-пользователя {dto.Email} (неверный пароль).");
                    }
                }
                catch {}
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return Unauthorized(new { message = "Неверный логин или пароль" });

            string hash = HashHelper.ComputeSha256Hash(dto.Password);

            if (hash != user.PasswordHash)
            {
                if (user.Role.Name == "Директор" || user.Role.Name == "Администратор" || user.Role.Name == "Менеджер")
                {
                    await AuditLogger.LogAsync(_context, user.IdUser, $"Ошибка входа сотрудника {user.Email} (неверный пароль).");
                }

                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            string token = GenerateJwtToken(user);

            if (user.Role.Name == "Директор" || user.Role.Name == "Администратор" || user.Role.Name == "Менеджер")
            {
                await AuditLogger.LogAsync(_context, user.IdUser, $"Сотрудник {user.Email} ({user.Role.Name}) успешно вошёл в систему.");
            }

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.Name
            });
        }
        #endregion

        #region Регистрация
        [HttpPost("send-registration-code")]
        public async Task<IActionResult> SendRegistrationCode([FromBody] RegisterRequestDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Ok(new ApiResponseDto { Success = false, Message = "Email уже зарегистрирован" });

            string code = Generate6DigitCode();

            var emailHelper = new EmailHelper();
            try
            {
                await emailHelper.SendEmail(dto.Email,
                    "Код подтверждения MebelShop",
                    $"Ваш код подтверждения: {code}. Он действителен 5 минут.");
            }
            catch
            {
                return Ok(new ApiResponseDto { Success = false, Message = "Не удалось отправить письмо на этот email" });
            }

            var oldCodes = await _context.EmailCodes
                .Where(c => c.Email == dto.Email
                            && c.Purpose == "Registration"
                            && c.IsUsed == false)
                .ToListAsync();
            oldCodes.ForEach(c => c.IsUsed = true);

            var emailCode = new EmailCode
            {
                Email = dto.Email,
                Code = code,
                Purpose = "Registration",
                ExpirationTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };
            _context.EmailCodes.Add(emailCode);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponseDto { Success = true, Message = "Код отправлен на email" });
        }

        [HttpPost("verify-registration-code")]
        public async Task<IActionResult> VerifyRegistrationCode([FromBody] VerifyCodeDto dto)
        {
            var emailCode = await _context.EmailCodes
                .Where(c => c.Email == dto.Email && c.Purpose == "Registration" && c.IsUsed == false)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (emailCode == null)
                return Ok(new ApiResponseDto { Success = false, Message = "Код не найден" });

            if (emailCode.ExpirationTime < DateTime.UtcNow)
                return Ok(new ApiResponseDto { Success = false, Message = "Код истек" });

            if (emailCode.Code != dto.Code)
                return Ok(new ApiResponseDto { Success = false, Message = "Неверный код" });

            emailCode.IsUsed = true;

            await _context.SaveChangesAsync();

            return Ok(new ApiResponseDto { Success = true, Message = "Код подтвержден" });
        }

        [HttpPost("finalize-registration")]
        public async Task<IActionResult> FinalizeRegistration([FromBody] FinalizeRegistrationDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Ok(new ApiResponseDto { Success = false, Message = "Email уже зарегистрирован" });

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = HashHelper.ComputeSha256Hash(dto.Password),
                RoleId = 4 //роль покупателя
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var profile = new UserProfile
            {
                UserId = user.IdUser,
                FirstName = dto.FirstName,
                Surname = dto.LastName,
                Theme = "Light"
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponseDto { Success = true, Message = "Регистрация завершена" });
        }
        #endregion

        #region Восстановление пароля
        [HttpPost("send-reset-code")]
        public async Task<IActionResult> SendResetCode([FromBody] ResetPasswordRequestDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Ok(new ApiResponseDto { Success = false, Message = "Пользователь с таким Email не найден" });

            if (user.Role.Name == "Директор" || user.Role.Name == "Администратор" || user.Role.Name == "Менеджер")
            {
                await AuditLogger.LogAsync(_context, user.IdUser, $"Отправлен код восстановления пароля ({user.Email})");
            }

            var code = AuthController.Generate6DigitCode();

            try
            {
                var emailHelper = new EmailHelper();
                await emailHelper.SendEmail(dto.Email, "Код восстановления пароля", $"Ваш код: {code}. Действует 5 минут.");
            }
            catch
            {
                return Ok(new ApiResponseDto { Success = false, Message = "Не удалось отправить письмо" });
            }

            var oldCodes = await _context.EmailCodes
                .Where(c => (c.UserId == user.IdUser || c.Email == dto.Email)
                            && c.Purpose == "ResetPassword"
                            && c.IsUsed == false)
                .ToListAsync();
            oldCodes.ForEach(c => c.IsUsed = true);

            var emailCode = new EmailCode
            {
                UserId = user.IdUser,
                Email = dto.Email,
                Code = code,
                Purpose = "ResetPassword",
                ExpirationTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };
            _context.EmailCodes.Add(emailCode);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponseDto { Success = true, Message = "Код отправлен на Email" });
        }

        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyCodeDto dto)
        {
            var emailCode = await _context.EmailCodes
                .Where(c => c.Email == dto.Email && c.Purpose == "ResetPassword" && c.IsUsed == false)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (emailCode == null)
                return Ok(new ApiResponseDto { Success = false, Message = "Код не найден" });

            if (emailCode.ExpirationTime < DateTime.UtcNow)
                return Ok(new ApiResponseDto { Success = false, Message = "Код истек" });

            if (emailCode.Code != dto.Code)
                return Ok(new ApiResponseDto { Success = false, Message = "Неверный код" });

            emailCode.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponseDto { Success = true, Message = "Код подтвержден" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Ok(new ApiResponseDto { Success = false, Message = "Пользователь не найден" });

            user.PasswordHash = HashHelper.ComputeSha256Hash(dto.NewPassword);
            await _context.SaveChangesAsync();

            if (user.Role.Name == "Директор" || user.Role.Name == "Администратор" || user.Role.Name == "Менеджер")
            {
                await AuditLogger.LogAsync(_context, user.IdUser, $"Изменён пароль сотрудника ({user.Email})");
            }

            return Ok(new ApiResponseDto { Success = true, Message = "Пароль успешно изменён" });
        }
        #endregion

        private string GenerateJwtToken(User user)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["TokenLifetimeMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateEmergencyToken(string username)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Администратор"),
                new Claim("IsEmergency", "true")
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["TokenLifetimeMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string Generate6DigitCode()
        {
            return _rand.Next(100000, 999999).ToString();
        }

        private class RootAccount
        {
            public string Username { get; set; }
            public string PasswordHash { get; set; }
        }
    }
}
