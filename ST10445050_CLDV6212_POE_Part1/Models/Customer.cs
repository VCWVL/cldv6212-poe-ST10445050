using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10445050_CLDV6212_POE_Part1.Models
{
    public class Customer : ITableEntity
    {
        public string CustomerID { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        // Required by Azure Table
        public string PartitionKey { get; set; } = "CustomersPartition";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        [NotMapped]
        public ETag ETag { get; set; }
    }
}
