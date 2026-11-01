using System.Text.Json;
using Azure.Storage.Queues;
using CoffeeAndChill.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoffeeAndChill.Functions
{
    public class QueueOrder
    {
        [Function("QueueOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "orders/queue")]
            HttpRequest req)
        {
            try
            {
                var order =
                    await JsonSerializer.DeserializeAsync<Order>(
                        req.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (order == null)
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "Invalid order data."
                    });
                }

                if (string.IsNullOrWhiteSpace(order.OrderId))
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "OrderId is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(order.CustomerName))
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "CustomerName is required."
                    });
                }

                if (order.SelectedItemSKUs == null ||
                    order.SelectedItemSKUs.Count == 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        message = "At least one item SKU is required."
                    });
                }

                string connectionString =
                    Environment.GetEnvironmentVariable(
                        "AzureWebJobsStorage")
                    ?? throw new InvalidOperationException(
                        "AzureWebJobsStorage is missing.");


     QueueClient queueClient = new QueueClient(
    connectionString,
    "order-processing-queue",
    new QueueClientOptions
    {
        MessageEncoding = QueueMessageEncoding.Base64
    });

await queueClient.CreateIfNotExistsAsync();
                string message =
                    JsonSerializer.Serialize(order);

                await queueClient.SendMessageAsync(message);

                return new OkObjectResult(new
                {
                    message = "Order successfully added to the queue.",
                    queue = "order-processing-queue",
                    orderId = order.OrderId
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new
                {
                    message = "Failed to queue order.",
                    error = ex.Message
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}