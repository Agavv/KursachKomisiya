using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MebelShopAPI.Models;

namespace MebelShopAPI.Controllers
{
    [Authorize(Roles = "Директор")]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly MebelShopContext _context;

        public ReportsController(MebelShopContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить отчёт по продажам и выручке
        /// </summary>
        [HttpGet("sales")]
        public async Task<IActionResult> GetSalesReport(int? year = null, int? month = null)
        {
            try
            {
                var ordersQuery = _context.Orders.AsQueryable();

                // фильтр по дате
                if (year.HasValue)
                    ordersQuery = ordersQuery.Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Year == year.Value);

                if (month.HasValue && month.Value >= 1 && month.Value <= 12)
                    ordersQuery = ordersQuery.Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == month.Value);

                var orders = await ordersQuery.ToListAsync();

                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .ToListAsync();

                // группировка продаж по дням
                var grouped = orderItems
                    .Join(orders, oi => oi.OrderId, o => o.IdOrder, (oi, o) => new
                    {
                        CreatedAt = o.CreatedAt,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.Product
                    })
                    .Where(x => x.CreatedAt.HasValue)
                    .GroupBy(x => x.CreatedAt.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Orders = g.Count(),
                        Revenue = g.Sum(x => x.UnitPrice * x.Quantity),
                        AvgOrder = g.Sum(x => x.UnitPrice * x.Quantity) / g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // самый продаваемый товар
                var topProduct = orderItems
                    .GroupBy(x => x.ProductId)
                    .Select(g => new { ProductId = g.Key, Count = g.Sum(x => x.Quantity) })
                    .OrderByDescending(g => g.Count)
                    .Take(1)
                    .Join(_context.Products, g => g.ProductId, p => p.IdProduct, (g, p) => p.ProductName)
                    .FirstOrDefault();

                // самая популярная категория
                var topCategory = orderItems
                    .Join(_context.Products, oi => oi.ProductId, p => p.IdProduct, (oi, p) => new { oi.Quantity, p.CategoryId })
                    .GroupBy(x => x.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Count = g.Sum(x => x.Quantity) })
                    .OrderByDescending(g => g.Count)
                    .Take(1)
                    .Join(_context.Categories, g => g.CategoryId, c => c.IdCategory, (g, c) => c.NameCategory)
                    .FirstOrDefault();

                return Ok(new
                {
                    topProduct = topProduct ?? "Нет данных",
                    topCategory = topCategory ?? "Нет данных",
                    items = grouped.Select(x => new
                    {
                        date = x.Date.ToString("dd.MM.yyyy"),
                        orders = x.Orders,
                        revenue = Math.Round(x.Revenue, 0, MidpointRounding.AwayFromZero),
                        avgOrder = Math.Round(x.AvgOrder, 2, MidpointRounding.AwayFromZero)
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка формирования отчёта", error = ex.Message });
            }
        }

        [HttpGet("stock")]
        public async Task<IActionResult> GetStockReport()
        {
            try
            {
                // Получаем товары
                var products = await _context.Products.ToListAsync();

                // Все продажи в период
                var sales = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .ToListAsync();

                var report = products.Select(p =>
                {
                    int soldQty = sales
                        .Where(s => s.ProductId == p.IdProduct)
                        .Sum(s => s.Quantity);

                    return new
                    {
                        idProduct = p.IdProduct,
                        productName = p.ProductName,
                        stock = p.StockQuantity,
                        sold = soldQty
                    };
                }).ToList();

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка формирования отчёта по остаткам", error = ex.Message });
            }
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviewsReport()
        {
            var data = await _context.Products
                .Select(p => new
                {
                    idProduct = p.IdProduct,
                    productName = p.ProductName,
                    avgRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                    reviewCount = p.Reviews.Count()
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}
