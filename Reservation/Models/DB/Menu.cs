namespace Reservation.Models.DB
{
    public class Menu
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string Title { get; set; } = string.Empty; // 用於 alt 屬性
        public string ImageUrl { get; set; } = string.Empty; // 菜單圖片
    }
}

