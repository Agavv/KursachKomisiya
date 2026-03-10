using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MebelShop.Helpers;
using MebelShopAPI.Models;

namespace MebelShopAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly MebelShopContext _context;

        public ReviewsController(MebelShopContext context)
        {
            _context = context;
        }

        // GET: api/Reviews
        [Authorize(Roles = "Менеджер,Администратор,Директор")]
        [HttpGet("All")]
        public async Task<ActionResult> GetReviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(r => r.User)
                    .ThenInclude(u => u.UserProfile)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewViewModel
                {
                    IdReview = r.IdReview,
                    ProductId = r.ProductId,
                    ProductImageUrl = r.Product.ProductImages
                                        .FirstOrDefault(pi => pi.IsMain == true).ImageUrl,
                    UserName = r.User.UserProfile.FirstName + " " + r.User.UserProfile.Surname,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    Text = r.ReviewText
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [Authorize(Roles = "Менеджер,Администратор,Директор")]
        [HttpPut("reply/{reviewId}")]
        public async Task<IActionResult> AddOrUpdateShopReply(int reviewId, [FromBody] string replyText)
        {
            if (string.IsNullOrWhiteSpace(replyText))
                return BadRequest("Ответ не может быть пустым.");

            try
            {
                var userId = await GetUserId();
                // Вызов хранимой процедуры
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_AddOrUpdateShopReplyToReview @ReviewID = {0}, @ShopReply = {1}",
                    reviewId, replyText
                );

                await AuditLogger.LogAsync(_context, userId, $"Добавлен/обновлён ответ магазина на отзыв ID {reviewId}");

                return Ok(new { message = "Ответ успешно добавлен или обновлён." });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = $"Ошибка при работе с базой данных: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Произошла ошибка: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Менеджер,Администратор,Директор")]
        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            try
            {
                var userId = await GetUserId();
                var review = await _context.Reviews.FindAsync(reviewId);
                if (review == null)
                    return NotFound(new { message = "Отзыв не найден." });

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                await AuditLogger.LogAsync(_context, userId, $"Удалён отзыв ID {reviewId}");

                return Ok(new { message = "Отзыв успешно удалён." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Ошибка при удалении отзыва: {ex.Message}" });
            }
        }

        private async Task<int?> GetUserId()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }
    }

    public class ReviewViewModel
    {
        public int IdReview { get; set; }
        public int ProductId { get; set; }
        public string ProductImageUrl { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Text { get; set; }
    }
}
