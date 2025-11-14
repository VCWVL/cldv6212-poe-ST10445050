using Azure;
using Azure.Data.Tables;
using ST10445050_CLDV6212_POE_Part1.Models;

namespace ST10445050_CLDV6212_POE_Part1.Services
{
    public class TableService
    {
        private readonly TableClient _productTableClient;
        private readonly TableClient _customerTableClient;
        private readonly TableClient _orderTableClient;

        // Partition keys help organize and query efficiently
        private const string ProductPartitionKey = "ProductsPartition";
        private const string CustomerPartitionKey = "CustomersPartition";
        private const string OrderPartitionKey = "OrdersPartition";

        public TableService(string connectionString)
        {
            _productTableClient = new TableClient(connectionString, "products");
            _customerTableClient = new TableClient(connectionString, "customers");
            _orderTableClient = new TableClient(connectionString, "orders");

            // Ensure tables exist
            _productTableClient.CreateIfNotExists();
            _customerTableClient.CreateIfNotExists();
            _orderTableClient.CreateIfNotExists();
        }

        // ================= PRODUCT METHODS =================
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var item in _productTableClient.QueryAsync<Product>())
            {
                products.Add(item);
            }
            return products;
        }

        public async Task AddProductAsync(Product product)
        {
            // Ensure partition & row keys are set
            product.PartitionKey ??= ProductPartitionKey;
            product.RowKey ??= Guid.NewGuid().ToString();

            await _productTableClient.AddEntityAsync(product);
        }

        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _productTableClient.UpsertEntityAsync(product, TableUpdateMode.Replace);
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ================= CUSTOMER METHODS =================
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            await foreach (var customer in _customerTableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            // Ensure partition & row keys are set
            customer.PartitionKey ??= CustomerPartitionKey;
            customer.RowKey ??= Guid.NewGuid().ToString();

            await _customerTableClient.AddEntityAsync(customer);
        }

        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _customerTableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _customerTableClient.UpsertEntityAsync(customer, TableUpdateMode.Replace);
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ================= ORDER METHODS =================
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (var order in _orderTableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }

        public async Task AddOrderAsync(Order order)
        {
            // Ensure partition & row keys are set
            order.PartitionKey ??= OrderPartitionKey;
            order.RowKey ??= Guid.NewGuid().ToString();

            await _orderTableClient.AddEntityAsync(order);
        }

        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _orderTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            await _orderTableClient.UpsertEntityAsync(order, TableUpdateMode.Replace);
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await _orderTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task SeedInitialDataAsync()
        {
            // Seed a default customer if none exist
            if (!(await GetAllCustomersAsync()).Any())
            {
                await AddCustomerAsync(new Customer
                {
                    CustomerID = "1",
                    FirstName = "Default Customer",
                    Email = "customer@example.com"
                });
            }

            // Seed a default product if none exist
            if (!(await GetAllProductsAsync()).Any())
            {
                await AddProductAsync(new Product
                {
                    ProductID = "P001",
                    Name = "Default Product",
                    Price = 100
                });
            }
        }
    }
}

    
