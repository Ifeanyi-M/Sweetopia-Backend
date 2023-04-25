using System.ComponentModel.DataAnnotations;

namespace BlueAlmond.Models.DTO
{
    public class OrderDetailCreateDto
    {
        [Required]
        public int MenuItemId { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public string ItemName { get; set; }
        [Required]
        public double Price { get; set; }
    }
}
