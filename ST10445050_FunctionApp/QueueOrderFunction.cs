using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABC_RETAILS_Function_App.Functions
{
    public class QueueOrderFunction
    {
        private readonly ILogger _logger;
        private readonly QueueClient _queueClient;

        public QueueOrderFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueOrderFunction>();

            // ? Read connection and queue settings
            string queueConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string queueName = Environment.GetEnvironmentVariable("AzureQueueName") ?? "order-messages";

            if (string.IsNullOrEmpty(queueConnectionString))
            {
                _logger.LogError("? AzureWebJobsStorage connection string is missing!");
                throw new ArgumentNullException("AzureWebJobsStorage");
            }

            _queueClient = new QueueClient(queueConnectionString, queueName);
            _queueClient.CreateIfNotExists();

            // ? Log actual queue endpoint (to confirm cloud vs local)
            _logger.LogInformation($"?? QueueClient URI: {_queueClient.Uri}");

            if (_queueClient.Exists())
                _logger.LogInformation($"? Connected to Azure Queue: {queueName}");
            else
                _logger.LogWarning($"?? Queue '{queueName}' could not be found or created.");
        }

        [Function("QueueOrderFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get", Route = "orders/send")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                // ======================
                // POST: Add message
                // ======================
                if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                    if (string.IsNullOrEmpty(requestBody))
                    {
                        _logger.LogWarning("?? Empty request body received.");
                        response.StatusCode = HttpStatusCode.BadRequest;
                        await response.WriteStringAsync("Invalid or empty order data.");
                        return response;
                    }

                    var order = JsonSerializer.Deserialize<Order>(requestBody);

                    if (order == null)
                    {
                        _logger.LogWarning("?? Unable to deserialize order data.");
                        response.StatusCode = HttpStatusCode.BadRequest;
                        await response.WriteStringAsync("Invalid order format.");
                        return response;
                    }

                    // ? Set defaults
                    order.PartitionKey ??= "OrdersPartition";
                    order.RowKey ??= Guid.NewGuid().ToString();

                    // ? Serialize safely (Base64 to ensure valid message)
                    string orderJson = JsonSerializer.Serialize(order);
                    string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(orderJson));

                    // ? Send message to Azure Queue
                    await _queueClient.SendMessageAsync(base64Message);

                    _logger.LogInformation($"? Order queued successfully. OrderID: {order.orderID}, ProductID: {order.productID}");
                    _logger.LogInformation($"?? Message sent to queue: {orderJson}");

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        message = $"Order for product {order.productID} queued successfully.",
                        orderJson
                    }));
                    return response;
                }

                // ======================
                // GET: Read queued messages (debugging)
                // ======================
                if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    var messages = new List<object>();
                    QueueMessage[] received = await _queueClient.ReceiveMessagesAsync(maxMessages: 10);

                    foreach (var msg in received)
                    {
                        string decodedMessage = Encoding.UTF8.GetString(Convert.FromBase64String(msg.MessageText));
                        messages.Add(new
                        {
                            MessageText = decodedMessage,
                            InsertedOn = msg.InsertedOn,
                            ExpiresOn = msg.ExpiresOn
                        });
                    }

                    _logger.LogInformation($"?? Retrieved {messages.Count} message(s) from the queue.");

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        messageCount = messages.Count,
                        messages
                    }));
                    return response;
                }

                // ======================
                // Unsupported methods
                // ======================
                response.StatusCode = HttpStatusCode.MethodNotAllowed;
                await response.WriteStringAsync("Only GET and POST methods are supported.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Error processing order queue: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { Error = ex.Message }));
                return response;
            }
        }

        // ======================
        // Order model
        // ======================
        public class Order
        {
            [Key]
            public int orderID { get; set; }

            public string? PartitionKey { get; set; } = "OrdersPartition";
            public string? RowKey { get; set; } = Guid.NewGuid().ToString();

            [Required(ErrorMessage = "Please select a customer.")]
            public int customerID { get; set; }

            [Required(ErrorMessage = "Please select a product.")]
            public string productID { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please enter the delivery date.")]
            public DateTime deliveryDate { get; set; } = DateTime.UtcNow.AddDays(1);

            [Required(ErrorMessage = "Please enter the delivery address.")]
            public string deliveryAddress { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please enter the order total.")]
            public double orderTotal { get; set; } = 0;
        }
    }
}
