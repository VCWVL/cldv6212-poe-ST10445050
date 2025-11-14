using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ST10445050_CLDV6212_POE_Part1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ST10445050_CLDV6212_POE_Part1.Services
{
    public class CustomerService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly TableClient _tableClient;

        public CustomerService(IConfiguration configuration, ILogger<CustomerService> logger)
        {
            // Initialize the connection to Azure Table Storage
            _tableServiceClient = new TableServiceClient(configuration.GetConnectionString("AzureStorage"));
            _tableClient = _tableServiceClient.GetTableClient("Customers");
        }

        // Add customer to Azure Table Storage
        public async Task<string> AddCustomerAsync(Customer customer)
        {
            try
            {
                // Add the customer entity to Azure Table Storage
                await _tableClient.AddEntityAsync(customer);
                return "Customer added successfully";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // Get all customers from Azure Table Storage
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();

            // Use QueryAsync to fetch all customers from Azure Table Storage
            await foreach (var customer in _tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            return customers;
        }
    }
}
