using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace ST10445050_CLDV6212_POE_Part1.Models
{
    public class Product : ITableEntity
    {
        // =========================
        // Azure Table Storage Keys
        // =========================
        public string PartitionKey { get; set; } = "ProductsPartition";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        // ProductID should not be bound by forms
        [BindNever]
        public string ProductID { get; set; } = string.Empty;

        // =========================
        // Product Details
        // =========================

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        // Description is now optional
        [StringLength(500, ErrorMessage = "Product description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative number.")]
        public double Price { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number.")]
        public int Quantity { get; set; }

        // Image URL is optional
        [Url(ErrorMessage = "Invalid URL format.")]
        public string? ImageUrl { get; set; }

        // =========================
        // Azure Table Metadata
        // =========================
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
