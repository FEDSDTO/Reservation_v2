namespace Reservation.Models.DB
{
    public class Restaurant
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string OpeningHours { get; set; } = "10:00-22:00";
        public int BranchId { get; set; }
        public int CategoryId { get; set; }
        public bool IsPopular { get; set; }
        public bool IsNew { get; set; }
    }
}


