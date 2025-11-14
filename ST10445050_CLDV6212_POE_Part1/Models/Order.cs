using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10445050_CLDV6212_POE_Part1.Models
{
    public class Order : ITableEntity
    {
        [Key]
        public int orderID { get; set; }

        public string? PartitionKey { get; set; } = "OrdersPartition";
        public string? RowKey { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Please select a customer.")]
        public int customerID { get; set; }

        [Required(ErrorMessage = "Please select a product.")]
        public string productID { get; set; }

        [Required(ErrorMessage = "Please enter the order quantity.")]
        public int quantity { get; set; } = 1;

        [Required(ErrorMessage = "Please enter the order total.")]
        [BindNever]
        public double orderTotal { get; set; } = 0;

        [Required]
        public string orderStatus { get; set; } = "Pending";

        // Order date set to the current date when the order is placed
        public DateTime orderDate { get; set; } = DateTime.UtcNow;

        // ITableEntity properties
        [NotMapped]
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}
