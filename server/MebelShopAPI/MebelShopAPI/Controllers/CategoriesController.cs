using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MebelShop.Helpers;
using MebelShopAPI.Models;

namespace MebelShopAPI.Controllers
{
    [Authorize(Roles = "Администратор,Директор")]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly MebelShopContext _context;

        public CategoriesController(MebelShopContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<CategoryAdminViewModel>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryAdminViewModel
                {
                    IdCategory = c.IdCategory,
                    NameCategory = c.NameCategory,
                    CharacteristicsCount = c.Characteristics.Count(),
                    ProductsCount = c.Products.Count()
                })
                .OrderBy(c => c.IdCategory)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] CategoryDto dto)
        {
            var category = new Category { NameCategory = dto.NameCategory };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var userId = GetUserId();
            await AuditLogger.LogAsync(_context, userId, $"Добавлена новая категория: {dto.NameCategory}");

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            string oldName = category.NameCategory;
            category.NameCategory = dto.NameCategory;
            await _context.SaveChangesAsync();

            var userId = GetUserId();
            await AuditLogger.LogAsync(_context, userId, $"Изменена категория: {oldName} → {dto.NameCategory}");

            return Ok();
        }

        [HttpGet("{id}/DeleteInfo")]
        public async Task<IActionResult> GetDeleteInfo(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            var productIds = await _context.Products
                .Where(p => p.CategoryId == id)
                .Select(p => p.IdProduct)
                .ToListAsync();

            var productsCount = productIds.Count;
            var characteristicsCount = await _context.Characteristics.CountAsync(c => c.CategoryId == id);
            var productImagesCount = await _context.ProductImages.CountAsync(pi => productIds.Contains(pi.ProductId));
            var reviewsCount = await _context.Reviews.CountAsync(r => productIds.Contains(r.ProductId));
            var cartItemsCount = await _context.CartItems.CountAsync(c => productIds.Contains(c.ProductId));
            var orderItemsCount = await _context.OrderItems.CountAsync(oi => productIds.Contains(oi.ProductId));
            var productCharacteristicsCount = await _context.ProductCharacteristics.CountAsync(pc => productIds.Contains(pc.ProductId));

            return Ok(new
            {
                CategoryName = category.NameCategory,
                Products = productsCount,
                Characteristics = characteristicsCount,
                ProductImages = productImagesCount,
                Reviews = reviewsCount,
                CartItems = cartItemsCount,
                OrderItems = orderItemsCount,
                ProductCharacteristics = productCharacteristicsCount
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            var products = await _context.Products.Where(p => p.CategoryId == id).ToListAsync();
            var productIds = products.Select(p => p.IdProduct).ToList();

            _context.Reviews.RemoveRange(_context.Reviews.Where(r => productIds.Contains(r.ProductId)));
            _context.CartItems.RemoveRange(_context.CartItems.Where(c => productIds.Contains(c.ProductId)));
            _context.OrderItems.RemoveRange(_context.OrderItems.Where(oi => productIds.Contains(oi.ProductId)));
            _context.ProductImages.RemoveRange(_context.ProductImages.Where(pi => productIds.Contains(pi.ProductId)));
            _context.ProductCharacteristics.RemoveRange(_context.ProductCharacteristics.Where(pc => productIds.Contains(pc.ProductId)));
            _context.Characteristics.RemoveRange(_context.Characteristics.Where(ch => ch.CategoryId == id));
            _context.Products.RemoveRange(products);

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            var userId = GetUserId();
            await AuditLogger.LogAsync(_context, userId, $"Удалена категория '{category.NameCategory}' со всеми связанными данными");

            return Ok();
        }

        [HttpGet("{categoryId}/Characteristics")]
        public async Task<IActionResult> GetCategoryCharacteristics(int categoryId)
        {
            var list = await _context.Characteristics
                .Where(c => c.CategoryId == categoryId)
                .Select(c => new
                {
                    c.IdCharacteristic,
                    c.Name,
                    c.ValueType
                }).ToListAsync();

            return Ok(list);
        }

        [HttpPost("{categoryId}/Characteristics")]
        public async Task<IActionResult> AddCharacteristic(int categoryId, [FromBody] CharacteristicDto dto)
        {
            var characteristic = new Characteristic
            {
                Name = dto.Name,
                ValueType = dto.ValueType,
                CategoryId = categoryId
            };

            _context.Characteristics.Add(characteristic);
            await _context.SaveChangesAsync();

            var category = await _context.Categories.FindAsync(categoryId);
            var userId = GetUserId();
            await AuditLogger.LogAsync(_context, userId, $"Добавлена характеристика '{dto.Name}' в категорию '{category?.NameCategory}'");

            return Ok();
        }

        [HttpPut("Characteristics/{id}")]
        public async Task<IActionResult> UpdateCharacteristic(int id, [FromBody] CharacteristicDto dto)
        {
            var c = await _context.Characteristics.FindAsync(id);
            if (c == null) return NotFound();

            string oldName = c.Name;
            c.Name = dto.Name;
            c.ValueType = dto.ValueType;
            await _context.SaveChangesAsync();

            var userId = GetUserId();
            await AuditLogger.LogAsync(_context, userId, $"Изменена характеристика '{oldName}' → '{dto.Name}' в категории '{c.Category?.NameCategory}'");

            return Ok();
        }

        [HttpGet("Characteristics/{id}/DeleteInfo")]
        public async Task<IActionResult> GetCharacteristicDeleteInfo(int id)
        {
            var characteristic = await _context.Characteristics
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.IdCharacteristic == id);

            if (characteristic == null)
                return NotFound();

            var productsUsing = await _context.ProductCharacteristics
                .CountAsync(pc => pc.CharacteristicId == id);

            var info = new
            {
                CharacteristicName = characteristic.Name,
                CategoryName = characteristic.Category.NameCategory,
                ProductsUsing = productsUsing
            };

            return Ok(info);
        }


        [HttpDelete("Characteristics/{id}")]
        public async Task<IActionResult> DeleteCharacteristic(int id)
        {
            var c = await _context.Characteristics.FindAsync(id);
            if (c == null) return NotFound();

            _context.Characteristics.Remove(c);
            await _context.SaveChangesAsync();

            var userId = GetUserId();
            await AuditLogger.LogAsync(_context, userId, $"Удалена характеристика '{c.Name}' из категории '{c.Category?.NameCategory}'");

            return Ok();
        }

        private int? GetUserId()
        {
            // FIX: Sub claim is set to email (not int ID), so int.TryParse always failed.
            // Use Identity.Name (also the email) and look up the user ID from DB.
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return null;

            // Synchronous lookup to keep non-async helper signature
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user?.IdUser;
        }
    }

    public class CategoryDto
    {
        public string NameCategory { get; set; }
    }

    public class CategoryAdminViewModel
    {
        public int IdCategory { get; set; }
        public string NameCategory { get; set; }
        public int CharacteristicsCount { get; set; }
        public int ProductsCount { get; set; }
    }
}
