using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ABCRetails_Function_App.Functions
{
    public class AddCustomerFunction
    {
        private readonly ILogger _logger;

        public AddCustomerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AddCustomerFunction>();
        }

        [Function("AddCustomerFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "customers/add")] HttpRequestData req)
        {
            _logger.LogInformation("Processing AddCustomerFunction HTTP request...");

            // Get connection string
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableClient = new TableClient(connectionString, "Customers");
            await tableClient.CreateIfNotExistsAsync();

            if (req.Method == "GET")
            {
                // Handle GET request ? return all customers
                try
                {
                    var customers = new List<Customer>();

                    await foreach (var entity in tableClient.QueryAsync<Customer>())
                    {
                        customers.Add(entity);
                    }

                    var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    await response.WriteStringAsync(JsonConvert.SerializeObject(customers));
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error fetching customers: {ex.Message}");
                    var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await response.WriteStringAsync("Error fetching customers.");
                    return response;
                }
            }

            // Handle POST request ? add a new customer
            if (req.Method == "POST")
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var customer = JsonConvert.DeserializeObject<Customer>(requestBody);

                var response = req.CreateResponse();

                if (customer == null || string.IsNullOrEmpty(customer.FirstName) || string.IsNullOrEmpty(customer.LastName))
                {
                    response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("Invalid customer data. Must include FirstName and LastName.");
                    return response;
                }

                try
                {
                    customer.PartitionKey ??= "CustomersPartition";
                    customer.RowKey ??= Guid.NewGuid().ToString();

                    // Generate incremental CustomerID
                    int maxId = 0;
                    await foreach (var c in tableClient.QueryAsync<Customer>())
                    {
                        if (int.TryParse(c.CustomerID, out var id) && id > maxId)
                            maxId = id;
                    }
                    customer.CustomerID = (maxId + 1).ToString();

                    await tableClient.AddEntityAsync(customer);

                    response.StatusCode = System.Net.HttpStatusCode.OK;
                    await response.WriteStringAsync($"Customer {customer.FirstName} {customer.LastName} added successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error adding customer: {ex.Message}");
                    response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                    await response.WriteStringAsync("Error adding customer.");
                }

                return response;
            }

            // If method is not GET or POST
            var methodNotAllowed = req.CreateResponse(System.Net.HttpStatusCode.MethodNotAllowed);
            await methodNotAllowed.WriteStringAsync("Only GET and POST methods are supported.");
            return methodNotAllowed;
        }
    }

    // Customer model
    public class Customer : ITableEntity
    {
        public string CustomerID { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
