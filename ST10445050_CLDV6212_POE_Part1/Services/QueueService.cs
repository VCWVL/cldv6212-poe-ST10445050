using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text.Json;

namespace ST10445050_CLDV6212_POE_Part1.Services
{
    public class QueueService
    {
        private readonly QueueClient _queueClient;

        public QueueService(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);

            // Ensure queue exists
            _queueClient.CreateIfNotExists();
        }

        // Send message (can be a string or serialized object like an Order)
        public async Task SendMessageAsync<T>(T message)
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            await _queueClient.SendMessageAsync(jsonMessage);
        }

        // Peek at next message without removing it
        public async Task<string?> PeekMessageAsync()
        {
            PeekedMessage[] peekedMessages = await _queueClient.PeekMessagesAsync(1);
            return peekedMessages.Length > 0 ? peekedMessages[0].MessageText : null;
        }

        // Receive and delete a message
        public async Task<string?> ReceiveMessageAsync()
        {
            QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(1);

            if (messages.Length > 0)
            {
                string messageText = messages[0].MessageText;

                // Delete after processing
                await _queueClient.DeleteMessageAsync(messages[0].MessageId, messages[0].PopReceipt);

                return messageText;
            }

            return null;
        }
    }
}
