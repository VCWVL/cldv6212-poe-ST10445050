using Azure;
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

            string queueConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string queueName = Environment.GetEnvironmentVariable("AzureQueueName") ?? "order-messages";
            _queueClient = new QueueClient(queueConnectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        [Function("QueueOrderFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get", Route = "orders/send")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    var order = JsonSerializer.Deserialize<Order>(requestBody);

                    if (order == null)
                    {
                        response.StatusCode = HttpStatusCode.BadRequest;
                        await response.WriteStringAsync(JsonSerializer.Serialize(new { Error = "Invalid order data." }));
                        return response;
                    }

                    order.PartitionKey ??= "OrdersPartition";
                    order.RowKey ??= Guid.NewGuid().ToString();

                    string orderJson = JsonSerializer.Serialize(order);
                    await _queueClient.SendMessageAsync(orderJson);

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        message = $"Order for product {order.productID} queued successfully.",
                        orderJson
                    }));
                    return response;
                }

                if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    var messages = new List<object>();
                    // Use async receive
                    QueueMessage[] received = await _queueClient.ReceiveMessagesAsync(maxMessages: 32);

                    foreach (var msg in received)
                    {
                        messages.Add(new
                        {
                            MessageText = msg.MessageText,
                            InsertedOn = msg.InsertedOn,
                            ExpirationTime = msg.ExpiresOn
                        });
                    }

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        messageCount = messages.Count,
                        messages
                    }));
                    return response;
                }

                response.StatusCode = HttpStatusCode.MethodNotAllowed;
                await response.WriteStringAsync("Only GET and POST are supported.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing order queue: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { Error = "Error processing order." }));
                return response;
            }
        }

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
