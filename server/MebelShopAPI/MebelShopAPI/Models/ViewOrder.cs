namespace MebelShopAPI.Models
{
    public class ViewOrder
    {
        public int IdOrder { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }

        // Новые поля
        public string DeliveryType { get; set; }
        public string DeliveryAddress { get; set; }
        public string PaymentType { get; set; }
    }
}
