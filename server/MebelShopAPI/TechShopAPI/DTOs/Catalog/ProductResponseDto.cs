namespace TechShopAPI.DTOs.Catalog
{
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string MainImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }


        public Dictionary<int, List<string>> Characteristics { get; set; } = new();
        public Dictionary<int, decimal> NumericCharacteristics { get; set; } = new();
    }
}
