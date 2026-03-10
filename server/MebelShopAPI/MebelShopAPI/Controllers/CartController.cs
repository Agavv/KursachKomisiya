using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MebelShopAPI.Models;

namespace MebelShopAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly MebelShopContext _context;

        public CartController(MebelShopContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var invalidItems = await _context.CartItems
                .Where(ci => ci.UserId == user.IdUser && ci.Quantity <= 0)
                .ToListAsync();

            if (invalidItems.Any())
            {
                _context.CartItems.RemoveRange(invalidItems);
                await _context.SaveChangesAsync();
            }

            var items = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == user.IdUser && ci.Quantity > 0)
                .Select(ci => new
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.ProductName,
                    Price = ci.Product.Price,
                    Quantity = ci.Quantity,
                    ImageUrl = _context.ProductImages
                        .Where(img => img.ProductId == ci.ProductId && img.IsMain == true)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { message = "Товар не найден" });

            if (product.StockQuantity <= 0)
                return BadRequest(new { message = "Товара нет в наличии" });

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ProductId == productId && ci.UserId == user.IdUser);

            if (item != null)
            {
                item.Quantity++;
            }
            else
            {
                item = new CartItem
                {
                    UserId = user.IdUser,
                    ProductId = productId,
                    Quantity = 1
                };
                _context.CartItems.Add(item);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Товар добавлен в корзину" });
        }

        [HttpPost("{productId}/increase")]
        public async Task<IActionResult> IncreaseQuantity(int productId)
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ProductId == productId && ci.UserId == user.IdUser);

            if (item == null) return NotFound("Товар не найден в корзине");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.IdProduct == productId);

            if (product == null) return NotFound("Товар не найден");

            if (item.Quantity >= product.StockQuantity)
            {
                return BadRequest("Невозможно добавить больше товара, чем есть на складе");
            }

            item.Quantity++;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Количество увеличено" });
        }


        [HttpPost("{productId}/decrease")]
        public async Task<IActionResult> DecreaseQuantity(int productId)
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ProductId == productId && ci.UserId == user.IdUser);

            if (item == null) return NotFound("Товар не найден в корзине");

            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                _context.CartItems.Remove(item);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Количество уменьшено" });
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ProductId == productId && ci.UserId == user.IdUser);

            if (item == null) return NotFound("Товар не найден в корзине");

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Товар удален из корзины" });
        }

        private async Task<User?> GetUser()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
