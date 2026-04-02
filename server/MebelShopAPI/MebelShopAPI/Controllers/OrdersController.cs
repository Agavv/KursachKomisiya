using API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Dynamic;
using System.Security.Claims;
using MebelShop.Helpers;
using MebelShopAPI.Helpers;
using MebelShopAPI.Models;

namespace MebelShopAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly MebelShopContext _context;

        public OrdersController(MebelShopContext context)
        {
            _context = context;
        }


        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            if (orderDto == null)
                return BadRequest("Данные заказа отсутствуют.");

            var user = await GetUser();
            if (user == null) return Unauthorized();

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.IdUser);
            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = user.IdUser,
                    FirstName = orderDto.FirstName,
                    Surname = orderDto.LastName
                };
                _context.UserProfiles.Add(profile);
            }
            else
            {
                if (profile.FirstName != orderDto.FirstName)
                    profile.FirstName = orderDto.FirstName;
                if (profile.Surname != orderDto.LastName)
                    profile.Surname = orderDto.LastName;
            }

            var order = new Order
            {
                UserId = user.IdUser,
                CreatedAt = DateTime.UtcNow,
                Status = "Создан",
                DeliveryType = orderDto.DeliveryType,
                DeliveryAddress = orderDto.DeliveryAddress,
                PaymentType = orderDto.PaymentType
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.IdUser)
                .ToListAsync();

            foreach (var item in cartItems)
            {
                if (item.Quantity <= 0) continue;
                if (item.Product.StockQuantity <= 0) continue;

                var orderItem = new OrderItem
                {
                    OrderId = order.IdOrder,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };
                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();

            string cardInfo = string.Empty;
            if (orderDto.PaymentType.Contains("Онлайн"))
            {
                var payment = await _context.UserPayments
                    .Where(p => p.UserId == user.IdUser)
                    .FirstOrDefaultAsync();

                if (payment != null && !string.IsNullOrEmpty(payment.CardNumber))
                {
                    string decryptedCard = CryptoHelper.Decrypt(payment.CardNumber);

                    if (decryptedCard.Length >= 4)
                    {
                        string last4 = decryptedCard.Replace(" ", "").Substring(decryptedCard.Length - 4);
                        cardInfo = $"**** **** **** {last4}";
                    }
                }
            }

            var emailHelper = new EmailHelper();
            string emailBody = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        h2 {{ color: #0078D4; }}
        table {{ border-collapse: collapse; width: 100%; margin-top: 10px; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .total {{ font-weight: bold; font-size: 1.1em; }}
        .section {{ margin-top: 20px; }}
    </style>
</head>
<body>
    <h2>Спасибо за ваш заказ!</h2>
    <div class='section'>
        <p><strong>Имя:</strong> {orderDto.FirstName}</p>
        <p><strong>Фамилия:</strong> {orderDto.LastName}</p>
        <p><strong>Способ доставки:</strong> {orderDto.DeliveryType}</p>
        <p><strong>Адрес:</strong> {orderDto.DeliveryAddress}</p>
        <p><strong>Способ оплаты:</strong> {orderDto.PaymentType}";

            if (!string.IsNullOrEmpty(cardInfo))
                emailBody += $" (карта {cardInfo})";

            emailBody += @"</p>
    </div>
    <div class='section'>
        <h3>Состав заказа:</h3>
        <table>
            <thead>
                <tr>
                    <th>Товар</th>
                    <th>Количество</th>
                    <th>Цена за шт.</th>
                    <th>Сумма</th>
                </tr>
            </thead>
            <tbody>";

            foreach (var item in cartItems)
            {
                decimal itemTotal = item.Quantity * item.Product.Price;
                emailBody += $@"
        <tr>
            <td>{item.Product.ProductName}</td>
            <td>{item.Quantity}</td>
            <td>{item.Product.Price:C}</td>
            <td>{itemTotal:C}</td>
        </tr>";
            }

            decimal total = cartItems.Sum(i => i.Product.Price * i.Quantity);

            emailBody += $@"
            </tbody>
        </table>
        <p class='total'>Итого: {total:C}</p>
    </div>
</body>
</html>";

            // FIX: wrap SendEmail in try/catch — SMTP failure must NOT roll back the saved order
            try
            {
                await emailHelper.SendEmail(
                    user.Email,
                    "Ваш заказ MebelShop",
                    body: "Спасибо за ваш заказ! Просмотрите детали в HTML версии письма.",
                    htmlBody: emailBody
                );
            }
            catch (Exception emailEx)
            {
                // Log but continue — order is already persisted
                Console.WriteLine($"[Email warning] Не удалось отправить письмо о заказе: {emailEx.Message}");
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(true);
        }

        [HttpGet("My")]
        public async Task<IActionResult> GetMyOrders()
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var orders = await _context.Orders
                .Where(o => o.UserId == user.IdUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Select(o => new OrderDto
                {
                    OrderId = o.IdOrder,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalPrice = o.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
                    ProductImages = _context.ProductImages
                        .Where(img => o.OrderItems.Select(oi => oi.ProductId).Contains(img.ProductId) && img.IsMain == true)
                        .Select(img => img.ImageUrl)
                        .ToList()
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("Details/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var order = await _context.Orders
                .Where(o => o.UserId == user.IdUser && o.IdOrder == orderId)
                .Select(o => new OrderDetailsDto
                {
                    OrderId = o.IdOrder,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalPrice = o.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
                    Items = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.ProductName,
                        Price = oi.UnitPrice,
                        Quantity = oi.Quantity,
                        TotalPrice = oi.UnitPrice * oi.Quantity,
                        MainImageUrl = oi.Product.ProductImages.FirstOrDefault(pi => pi.IsMain == true).ImageUrl
                    }).ToList(),
                })
                .FirstOrDefaultAsync();

            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost("Repeat/{orderId}")]
        public async Task<IActionResult> RepeatOrder(int orderId)
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.IdOrder == orderId && o.UserId == user.IdUser);

            if (order == null)
                return NotFound("Заказ не найден");

            foreach (var item in order.OrderItems)
            {
                if (item.Quantity <= 0 || item.Product.StockQuantity <= 0)
                    continue;

                int quantityToAdd = Math.Min(item.Quantity, item.Product.StockQuantity);

                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == user.IdUser && c.ProductId == item.ProductId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity = Math.Min(existingCartItem.Quantity + quantityToAdd, item.Product.StockQuantity);
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = user.IdUser,
                        ProductId = item.ProductId,
                        Quantity = quantityToAdd
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Товары добавлены в корзину" });
        }

        [HttpGet("my-products")]
        public async Task<IActionResult> GetMyProducts()
        {
            var user = await GetUser();
            if (user == null) return Unauthorized();

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == user.IdUser)
                .Select(c => c.ProductId)
                .ToListAsync();

            var products = await _context.OrderItems
                .Where(oi => oi.Order.UserId == user.IdUser)
                .Select(oi => oi.Product)
                .Distinct()
                .Select(p => new MyProductViewModel
                {
                    ProductId = p.IdProduct,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    MainImageUrl = _context.ProductImages
                        .Where(img => img.ProductId == p.IdProduct && img.IsMain == true)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(),
                    IsInCart = cartItems.Contains(p.IdProduct)
                })
                .ToListAsync();

            return Ok(products);
        }

        [Authorize(Roles = "Менеджер,Администратор,Директор")]
        [HttpGet("All")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.ViewOrders
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders);
        }

        [Authorize(Roles = "Менеджер,Администратор,Директор")]
        [HttpGet("{orderId}/Items")]
        public async Task<IActionResult> GetOrderItems(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            var items = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => new OrderItemDetailViewModel
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    StockQuantity = oi.Product.StockQuantity
                })
                .ToListAsync();

            return Ok(items);
        }

        [Authorize(Roles = "Менеджер,Администратор,Директор")]
        [HttpPost("UpdateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.IdOrder == dto.OrderId);

            if (order == null)
                return NotFound("Заказ не найден");

            if (order.Status == "Отменен" || order.Status == "Получен")
                return BadRequest("Изменение статуса недоступно");

            string? oldStatus = order.Status;
            order.Status = dto.NewStatus;

            await _context.SaveChangesAsync();

            int? userId = User.FindFirstValue(ClaimTypes.NameIdentifier) is string uid && int.TryParse(uid, out int parsedId) ? parsedId : (int?)null;
            await AuditLogger.LogAsync(_context, userId, $"Статус заказа #{order.IdOrder} изменён с '{oldStatus}' на '{dto.NewStatus}'");

            string subject = $"Ваш заказ №{order.IdOrder} обновлен";
            string body = $"Статус вашего заказа изменен на: {dto.NewStatus}.";

            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                body += $"\nКомментарий от менеджера: {dto.Comment}";
            }

            try
            {
                var emailHelper = new EmailHelper();
                await emailHelper.SendEmail(order.User.Email, subject, body);
            }
            catch (Exception ex)
            {
                return Ok(new { success = true, warning = $"Статус обновлен, но письмо не отправлено: {ex.Message}" });
            }

            return Ok(true);
        }

        private async Task<User?> GetUser()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }

    public class UpdateOrderStatusDto
    {
        public int OrderId { get; set; }
        public string NewStatus { get; set; }
        public string Comment { get; set; }
    }

    public class OrderItemDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
    }

    public class MyProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string MainImageUrl { get; set; }
        public decimal Price { get; set; }

        public bool IsInCart { get; set; }
        public string CartButtonText => IsInCart ? "В корзине" : "Купить";
    }

    public class OrderDetailsDto
    {
        public int OrderId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string MainImageUrl { get; set; }
    }


    public class OrderDto
    {
        public int OrderId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public List<string> ProductImages { get; set; } = new();
    }

    public class CreateOrderDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DeliveryType { get; set; }
        public string DeliveryAddress { get; set; }
        public string PaymentType { get; set; }
    }
}
