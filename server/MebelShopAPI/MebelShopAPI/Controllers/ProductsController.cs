using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Threading.Tasks;
using MebelShop.Helpers;
using MebelShopAPI.DTOs.Auth;
using MebelShopAPI.DTOs.Catalog;
using MebelShopAPI.Models;

namespace MebelShopAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly MebelShopContext _context;

        public ProductsController(MebelShopContext context)
        {
            _context = context;
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog([FromQuery] ProductFilterDto filter, int page = 1, int pageSize = 10)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return Unauthorized("Пользователь не найден");

            int userId = user.IdUser;

            var query = _context.Products.Where(p => p.StockQuantity > 0 && p.Price > 0 && p.ProductImages.Count > 0);

            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                string search = filter.SearchQuery.ToLower();
                query = query.Where(p => EF.Functions.Like(p.ProductName.ToLower(), $"%{search}%"));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            foreach (var kv in filter.ListFilters)
            {
                int charId = kv.Key;
                string value = kv.Value;
                query = query.Where(p => _context.ProductCharacteristics
                    .Any(pc => pc.ProductId == p.IdProduct && pc.CharacteristicId == charId && pc.Value == value));
            }

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = filter.SortBy switch
                {
                    "price_asc" => query.OrderBy(p => p.Price),
                    "price_desc" => query.OrderByDescending(p => p.Price),
                    "rating_desc" => query.OrderByDescending(p => _context.Reviews.Where(r => r.ProductId == p.IdProduct).Average(r => (double?)r.Rating) ?? 0),
                    "reviews_desc" => query.OrderByDescending(p => _context.Reviews.Count(r => r.ProductId == p.IdProduct)),
                    _ => query
                };
            }

            var totalCount = await query.CountAsync();

            var tempProducts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    Id = p.IdProduct,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    MainImageUrl = _context.ProductImages
                        .Where(img => img.ProductId == p.IdProduct && img.IsMain == true)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(),
                    AverageRating = _context.Reviews
                        .Where(r => r.ProductId == p.IdProduct)
                        .Select(r => (double?)r.Rating)
                        .Average() ?? 0,
                    ReviewsCount = _context.Reviews.Count(r => r.ProductId == p.IdProduct),
                    IsInCart = _context.CartItems.Any(ci => ci.UserId == userId && ci.ProductId == p.IdProduct)
                })
                .ToListAsync();

            var rangeIds = filter.MinRanges.Keys.Union(filter.MaxRanges.Keys).ToHashSet();
            if (rangeIds.Any())
            {
                var productIds = tempProducts.Select(p => p.Id).ToList();
                var characteristics = await _context.ProductCharacteristics
                    .Where(pc => productIds.Contains(pc.ProductId) && rangeIds.Contains(pc.CharacteristicId))
                    .Select(pc => new { pc.ProductId, pc.CharacteristicId, pc.Value })
                    .ToListAsync();

                var groupedChars = characteristics.GroupBy(c => c.ProductId)
                    .ToDictionary(g => g.Key, g => g.ToDictionary(c => c.CharacteristicId, c => c.Value));

                tempProducts = tempProducts.Where(p =>
                {
                    if (!groupedChars.TryGetValue(p.Id, out var charDict))
                        return false;

                    foreach (int charId in rangeIds)
                    {
                        if (!charDict.TryGetValue(charId, out string valStr))
                            return false;

                        if (!decimal.TryParse(valStr, out decimal value))
                            return false;

                        decimal? min = filter.MinRanges.GetValueOrDefault(charId);
                        decimal? max = filter.MaxRanges.GetValueOrDefault(charId);

                        if (min.HasValue && value < min.Value)
                            return false;

                        if (max.HasValue && value > max.Value)
                            return false;
                    }

                    return true;
                }).ToList();
            }

            var products = tempProducts.Select(p => new ProductViewModel
            {
                Id = p.Id,
                ProductName = p.ProductName,
                Price = p.Price,
                MainImageUrl = p.MainImageUrl,
                AverageRating = p.AverageRating,
                ReviewsCount = p.ReviewsCount,
                IsInCart = p.IsInCart
            }).ToList();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Products = products
            });
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetProductDetail(int productId)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Unauthorized("Пользователь не найден");

            var product = await _context.Products
                .Where(p => p.IdProduct == productId)
                .Select(p => new ProductDetailViewModel
                {
                    Id = p.IdProduct,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Description = p.Description,
                    ImageUrls = _context.ProductImages
                        .Where(img => img.ProductId == p.IdProduct)
                        .Select(img => img.ImageUrl)
                        .ToList(),
                    Characteristics = _context.ProductCharacteristics
                        .Where(pc => pc.ProductId == p.IdProduct)
                        .Select(pc => new CharacteristicDetailDto
                        {
                            Name = pc.Characteristic.Name,
                            Value = pc.Value
                        }).ToList(),
                    Reviews = _context.Reviews
                        .Where(r => r.ProductId == p.IdProduct)
                        .OrderByDescending(r => r.CreatedAt)
                        .Select(r => new ReviewDto
                        {
                            UserName = r.User.UserProfile.FirstName +
                               (r.UserId == user.IdUser ? " (Вы)" : ""),
                            Comment = r.ReviewText,
                            Rating = r.Rating,
                            CreatedAt = r.CreatedAt.Value,
                            IsMine = r.UserId == user.IdUser
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new { c.IdCategory, c.NameCategory })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("categories/{categoryId}/characteristics")]
        public async Task<IActionResult> GetCharacteristics(int categoryId)
        {
            var characteristics = await _context.Characteristics
                .Where(c => c.CategoryId == categoryId)
                .ToListAsync();

            var result = new List<CharacteristicDto>();

            foreach (var c in characteristics)
            {
                var dto = new CharacteristicDto
                {
                    Id = c.IdCharacteristic,
                    Name = c.Name,
                    ValueType = c.ValueType
                };

                var values = await _context.ProductCharacteristics
                    .Where(pc => pc.CharacteristicId == c.IdCharacteristic)
                    .Select(pc => pc.Value)
                    .ToListAsync();

                if (c.ValueType == "list")
                {
                    dto.Values = values.Distinct().ToList();
                }
                else if (c.ValueType == "range")
                {
                    var decimalValues = values
                        .Select(v => decimal.TryParse(v, out var val) ? val : 0)
                        .ToList();

                    dto.MinValue = decimalValues.DefaultIfEmpty(0).Min();
                    dto.MaxValue = decimalValues.DefaultIfEmpty(0).Max();
                }

                result.Add(dto);
            }

            return Ok(result);
        }

        [HttpGet("{productId}/can-review")]
        public async Task<IActionResult> CanReview(int productId)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Unauthorized("Пользователь не найден");

            bool bought = await _context.Orders
                .AnyAsync(o => o.UserId == user.IdUser &&
                               o.OrderItems.Any(oi => oi.ProductId == productId) &&
                               (o.Status == "Отправлен" || o.Status == "Получен"));

            return Ok(new { canReview = bought });
        }

        [HttpPost("{productId}/reviews")]
        public async Task<IActionResult> AddReview(int productId, [FromBody] AddReviewDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Comment))
                return BadRequest("Комментарий не может быть пустым");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId");
            if (userIdClaim == null)
                return Unauthorized("Не удалось определить пользователя");

            var email = userIdClaim.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Unauthorized("Пользователь не найден");

            int userId = user.IdUser;

            bool hasOrder = await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi =>
                    oi.ProductId == productId &&
                    oi.Order.UserId == userId &&
                    (oi.Order.Status == "Отправлен" || oi.Order.Status == "Получен")
                );

            if (!hasOrder)
                return BadRequest("Вы не можете оставить отзыв, так как не покупали этот товар.");

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview != null)
                return Conflict("Отзыв уже существует. Используйте PUT для обновления.");

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                ReviewText = dto.Comment,
                Rating = dto.Rating,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Отзыв добавлен" });
        }

        [HttpPut("{productId}/reviews")]
        public async Task<IActionResult> UpdateReview(int productId, [FromBody] AddReviewDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Comment))
                return BadRequest("Комментарий не может быть пустым");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId");
            if (userIdClaim == null)
                return Unauthorized("Не удалось определить пользователя");

            var email = userIdClaim.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Unauthorized("Пользователь не найден");

            int userId = user.IdUser;

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview == null)
                return NotFound("Отзыв не найден");

            existingReview.ReviewText = dto.Comment;
            existingReview.Rating = dto.Rating;
            existingReview.CreatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new ApiResponseDto { Success = true, Message = "Отзыв обновлён" });
        }

        [HttpDelete("{productId}/reviews")]
        public async Task<IActionResult> DeleteReview(int productId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId");
            if (userIdClaim == null)
                return Unauthorized("Не удалось определить пользователя");

            var email = userIdClaim.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Unauthorized("Пользователь не найден");

            int userId = user.IdUser;

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview == null)
                return NotFound("Отзыв не найден");

            _context.Reviews.Remove(existingReview);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Отзыв удален" });
        }

        [HttpGet("my-reviews")]
        public async Task<IActionResult> GetMyReviews()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Unauthorized("Пользователь не найден");

            var reviews = await _context.Reviews
                .Where(r => r.UserId == user.IdUser)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new MyReviewDto
                {
                    ProductId = r.ProductId,
                    ProductName = r.Product.ProductName,
                    ProductImageUrl = _context.ProductImages
                                        .Where(img => img.ProductId == r.ProductId && img.IsMain == true)
                                        .Select(img => img.ImageUrl)
                                        .FirstOrDefault(),
                    Comment = r.ReviewText,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpGet("All")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Select(p => new
                    {
                        IdProduct = p.IdProduct,
                        CategoryId = p.CategoryId,
                        ProductName = p.ProductName,
                        CategoryName = p.Category.NameCategory,
                        Description = p.Description,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity
                    })
                    .OrderBy(p => p.IdProduct)
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при получении списка товаров", error = ex.Message });
            }
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpGet("{id}/DeleteInfo")]
        public async Task<IActionResult> GetDeleteInfo(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.IdProduct == id);

            if (product == null)
                return NotFound();

            var info = new
            {
                ProductName = product.ProductName,
                ImagesCount = await _context.ProductImages.CountAsync(i => i.ProductId == id),
                ReviewsCount = await _context.Reviews.CountAsync(r => r.ProductId == id),
                CartItemsCount = await _context.CartItems.CountAsync(c => c.ProductId == id),
                OrderItemsCount = await _context.OrderItems.CountAsync(o => o.ProductId == id),
            };

            return Ok(info);
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Товар не найден" });

            var images = _context.ProductImages.Where(i => i.ProductId == id);
            var characteristics = _context.ProductCharacteristics.Where(c => c.ProductId == id);
            var reviews = _context.Reviews.Where(r => r.ProductId == id);
            var cartItems = _context.CartItems.Where(ci => ci.ProductId == id);
            var orderItems = _context.OrderItems.Where(oi => oi.ProductId == id);

            string imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            foreach (var img in images)
            {
                if (!string.IsNullOrEmpty(img.ImageUrl))
                {
                    var filePath = Path.Combine(imagesPath, img.ImageUrl);
                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            System.IO.File.Delete(filePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка удаления файла {filePath}: {ex.Message}");
                        }
                    }
                }
            }

            string productName = product.ProductName;

            _context.ProductImages.RemoveRange(images);
            _context.ProductCharacteristics.RemoveRange(characteristics);
            _context.Reviews.RemoveRange(reviews);
            _context.CartItems.RemoveRange(cartItems);
            _context.OrderItems.RemoveRange(orderItems);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            User? currentUser = await GetCurrentUser();
            await AuditLogger.LogAsync(_context, currentUser.IdUser, $"Удален продукт '{productName}' (ID: {id}) и все связанные данные");

            return Ok(new { message = "Товар и связанные данные успешно удалены" });
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpPost]
        public async Task<ActionResult<int>> CreateProduct([FromBody] ProductCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Пустые данные.");

            if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.ProductName.Length < 2)
                return BadRequest("Название товара должно содержать минимум 2 символа.");

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length < 2)
                return BadRequest("Описание должно содержать минимум 2 символа.");

            var product = new Product
            {
                ProductName = dto.ProductName,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.Category_ID
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            User? currentUser = await GetCurrentUser();
            await AuditLogger.LogAsync(_context, currentUser.IdUser, $"Создан продукт '{product.ProductName}' (ID: {product.IdProduct})");

            return Ok(product.IdProduct);
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage([FromBody] ProductImageDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ImageBase64))
                return BadRequest("Некорректные данные изображения.");

            var product = await _context.Products.FindAsync(dto.Product_ID);
            if (product == null)
                return NotFound("Товар не найден.");

            try
            {
                byte[] bytes = Convert.FromBase64String(dto.ImageBase64);

                string fileName = $"{Guid.NewGuid()}.jpg";
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string filePath = Path.Combine(folderPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                var image = new ProductImage
                {
                    ProductId = dto.Product_ID,
                    ImageUrl = fileName,
                    IsMain = dto.IsMain
                };

                if (dto.IsMain)
                {
                    var existingMain = _context.ProductImages
                        .FirstOrDefault(i => i.ProductId == dto.Product_ID && i.IsMain == true);
                    if (existingMain != null)
                        existingMain.IsMain = false;
                }

                _context.ProductImages.Add(image);
                await _context.SaveChangesAsync();


                User? currentUser = await GetCurrentUser();
                await AuditLogger.LogAsync(_context, currentUser.IdUser, $"Загружено изображение '{fileName}' для продукта '{product.ProductName}' (ID: {product.IdProduct})");

                return Ok("Изображение сохранено.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при сохранении изображения: {ex.Message}");
            }
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpPost("AddCharacteristic")]
        public async Task<IActionResult> AddCharacteristic([FromBody] ProductCharacteristicDto dto)
        {
            if (dto == null)
                return BadRequest("Некорректные данные характеристики.");

            var product = await _context.Products.FindAsync(dto.Product_ID);
            if (product == null)
                return NotFound("Товар не найден.");

            var characteristic = await _context.Characteristics.FindAsync(dto.Characteristic_ID);
            if (characteristic == null)
                return NotFound("Характеристика не найдена.");

            if (characteristic.ValueType == "range" && !decimal.TryParse(dto.Value, out _))
                return BadRequest($"Характеристика \"{characteristic.Name}\" должна быть числом.");

            var productCharacteristic = new ProductCharacteristic
            {
                ProductId = dto.Product_ID,
                CharacteristicId = dto.Characteristic_ID,
                Value = dto.Value
            };

            _context.ProductCharacteristics.Add(productCharacteristic);
            await _context.SaveChangesAsync();

            User? currentUser = await GetCurrentUser();
            await AuditLogger.LogAsync(_context, currentUser.IdUser, $"Добавлена характеристика '{characteristic.Name}' = '{dto.Value}' для продукта '{product.ProductName}' (ID: {product.IdProduct})");

            return Ok("Характеристика добавлена.");
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpGet("{id}/Edit")]
        public async Task<IActionResult> GetProductForEdit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.IdProduct == id);

            if (product == null)
                return NotFound();

            var images = await _context.ProductImages
                .Where(i => i.ProductId == id)
                .Select(i => new
                {
                    Id = i.IdImage,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain
                })
                .ToListAsync();

            var characteristics = await _context.ProductCharacteristics
                .Where(pc => pc.ProductId == id)
                .Select(pc => new
                {
                    Id = pc.IdProductCharacteristic,
                    CharacteristicId = pc.CharacteristicId,
                    Value = pc.Value
                })
                .ToListAsync();

            return Ok(new
            {
                Product = new
                {
                    IdProduct = product.IdProduct,
                    ProductName = product.ProductName,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    CategoryId = product.CategoryId
                },
                Images = images,
                Characteristics = characteristics
            });
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Пустые данные.");

            if (string.IsNullOrWhiteSpace(dto.ProductName) || dto.ProductName.Length < 2)
                return BadRequest("Название товара должно содержать минимум 2 символа.");

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length < 2)
                return BadRequest("Описание должно содержать минимум 2 символа.");

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound("Товар не найден.");

            string oldName = product.ProductName;

            product.ProductName = dto.ProductName;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.Category_ID;

            await _context.SaveChangesAsync();

            User? currentUser = await GetCurrentUser();
            await AuditLogger.LogAsync(_context, currentUser.IdUser, $"Изменен продукт ID {id}: '{oldName}' -> '{product.ProductName}'");

            return Ok();
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpDelete("{productId}/Characteristics")]
        public async Task<IActionResult> DeleteProductCharacteristics(int productId)
        {
            var chars = _context.ProductCharacteristics.Where(pc => pc.ProductId == productId);
            _context.ProductCharacteristics.RemoveRange(chars);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpDelete("Images/{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
                return NotFound("Изображение не найдено.");

            var product = await _context.Products.FindAsync(image.ProductId);

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", image.ImageUrl);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();


            User? currentUser = await GetCurrentUser();
            await AuditLogger.LogAsync(_context, currentUser.IdUser, $"Удалено изображение '{image.ImageUrl}' для продукта '{product?.ProductName}' (ID: {product?.IdProduct})");

            return Ok();
        }

        [Authorize(Roles = "Администратор,Директор")]
        [HttpPut("Images/{imageId}")]
        public async Task<IActionResult> UpdateImage(int imageId, [FromBody] UpdateImageDto dto)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
                return NotFound("Изображение не найдено.");

            var product = await _context.Products.FindAsync(image.ProductId);

            if (dto.IsMain)
            {
                var currentMain = await _context.ProductImages.FirstOrDefaultAsync(i => i.ProductId == image.ProductId && i.IsMain == true);
                if (currentMain != null && currentMain.IdImage != imageId)
                    currentMain.IsMain = false;
            }

            image.IsMain = dto.IsMain;
            await _context.SaveChangesAsync();


            User? currentUser = await GetCurrentUser();
            await AuditLogger.LogAsync(_context, currentUser.IdUser, $"Обновлено изображение '{image.ImageUrl}' для продукта '{product?.ProductName}' (ID: {product?.IdProduct}). IsMain = {dto.IsMain}");

            return Ok();
        }

        [Authorize(Roles = "Директор,Администратор")]
        [HttpPost("import")]
        public async Task<IActionResult> ImportProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не загружен.");

            using var reader = new StreamReader(file.OpenReadStream());
            var lines = new List<string>();
            while (!reader.EndOfStream)
                lines.Add(await reader.ReadLineAsync());

            int successCount = 0;
            var invalidRows = new List<string>();

            foreach (var line in lines.Skip(1)) // Пропускаем заголовок
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                if (parts.Length < 5)
                {
                    invalidRows.Add(line);
                    continue;
                }

                string productName = parts[0].Trim();
                string description = parts[1].Trim();

                if (!decimal.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ||
                    !int.TryParse(parts[3].Trim(), out var stockQuantity) ||
                    price <= 0 || stockQuantity < 0)
                {
                    invalidRows.Add(line);
                    continue;
                }

                string categoryName = parts[4].Trim();

                var category = await _context.Categories.FirstOrDefaultAsync(c => c.NameCategory == categoryName);
                if (category == null)
                {
                    category = new Category { NameCategory = categoryName };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }

                // Create and save product once
                var product = new Product
                {
                    ProductName = productName,
                    Description = description,
                    Price = price,
                    StockQuantity = stockQuantity,
                    CategoryId = category.IdCategory
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Process characteristics
                bool hasInvalidChar = false;
                for (int i = 5; i < parts.Length; i++)
                {
                    string charField = parts[i].Trim();
                    var charParts = charField.Split(':');
                    if (charParts.Length != 2)
                    {
                        hasInvalidChar = true;
                        break;
                    }

                    string characteristicName = charParts[0].Trim();
                    string value = charParts[1].Trim();

                    var characteristic = await _context.Characteristics
                        .FirstOrDefaultAsync(c => c.Name == characteristicName && c.CategoryId == category.IdCategory);

                    if (characteristic == null)
                    {
                        characteristic = new Characteristic
                        {
                            Name = characteristicName,
                            CategoryId = category.IdCategory,
                            ValueType = "list"
                        };
                        _context.Characteristics.Add(characteristic);
                        await _context.SaveChangesAsync();
                    }

                    var productCharacteristic = new ProductCharacteristic
                    {
                        ProductId = product.IdProduct,
                        CharacteristicId = characteristic.IdCharacteristic,
                        Value = value
                    };
                    _context.ProductCharacteristics.Add(productCharacteristic);
                    await _context.SaveChangesAsync();
                }

                if (hasInvalidChar)
                {
                    // Rollback product if characteristics invalid? Or just flag.
                    _context.Products.Remove(product); // Optional: remove if strict
                    await _context.SaveChangesAsync();
                    invalidRows.Add(line);
                }
                else successCount++;
            }

            User? currentUser = await GetCurrentUser();
            await AuditLogger.LogAsync(_context, currentUser.IdUser, $"Импорт продуктов: успешно {successCount}, ошибки {invalidRows.Count}");

            if (invalidRows.Any())
            {
                return BadRequest(new { message = "Некоторые строки не прошли проверку: " + string.Join(", ", invalidRows) });
            }

            return Ok(new { message = "Товары и категории успешно импортированы." });
        }

        private async Task<User?> GetCurrentUser()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return null;
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }

    public class UpdateImageDto
    {
        public bool IsMain { get; set; }
    }

    public class ProductCharacteristicDto
    {
        public int Product_ID { get; set; }
        public int Characteristic_ID { get; set; }
        public string Value { get; set; }
    }

    public class ProductImageDto
    {
        public int Product_ID { get; set; }
        public string ImageBase64 { get; set; }
        public bool IsMain { get; set; }
    }

    public class ProductCreateDto
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int Category_ID { get; set; }
    }

    public class ProductFilterDto
    {
        public string? SearchQuery { get; set; }
        public int? CategoryId { get; set; }
        public Dictionary<int, string> ListFilters { get; set; } = new();
        public Dictionary<int, decimal?> MinRanges { get; set; } = new();
        public Dictionary<int, decimal?> MaxRanges { get; set; } = new();
        public string? SortBy { get; set; }
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string MainImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public bool IsInCart { get; set; }
        public string CartButtonText => IsInCart ? "В корзине" : "Купить";
    }

    public class MyReviewDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AddReviewDto
    {
        public string Comment { get; set; }
        public int Rating { get; set; }
    }

    public class ReviewDto
    {
        public string UserName { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsMine { get; set; }
    }

    public class ProductDetailViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public List<CharacteristicDetailDto> Characteristics { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    public class CharacteristicDetailDto
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class CharacteristicDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ValueType { get; set; }
        public List<string> Values { get; set; } = new();
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
    }
}