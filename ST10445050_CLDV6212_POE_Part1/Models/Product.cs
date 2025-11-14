using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10445050_CLDV6212_POE_Part1.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "ProductsPartition";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public string ProductID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Product description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative number.")]
        public double Price { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number.")]
        public int Quantity { get; set; }

        public string? ImageUrl { get; set; }

        // For file uploads
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        // For base64 encoded image
        [NotMapped]
        public string? ImageBase64 { get; set; }

        //  ITableEntity members
        public DateTimeOffset? Timestamp { get; set; }
        [NotMapped]  public ETag ETag { get; set; }
    }
}
