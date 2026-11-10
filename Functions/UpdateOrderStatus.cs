using Azure;
using CoffeeAndChill.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;

namespace CoffeeNChill.Functions
{
    public class UpdateOrderStatusRequest
    {
        public string OrderDate { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateOrderStatus
    {
        private static readonly string[] AllowedStatuses =
        {
            "Received",
            "Preparing",
            "Ready",
            "Collected"
        };

        private readonly OrderTableService _orderTableService;

        public UpdateOrderStatus(OrderTableService orderTableService)
        {
            _orderTableService = orderTableService;
        }

        [Function("UpdateOrderStatus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "put",
                Route = "orders/status")]
            HttpRequest req)
        {
            UpdateOrderStatusRequest? request;

            try
            {
                request = await JsonSerializer.DeserializeAsync<UpdateOrderStatusRequest>(
                    req.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Invalid JSON format."
                });
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.OrderDate) ||
                string.IsNullOrWhiteSpace(request.OrderId) ||
                string.IsNullOrWhiteSpace(request.Status))
            {
                return new BadRequestObjectResult(new
                {
                    error = "Order date, order ID and status are required."
                });
            }

            string? validStatus = AllowedStatuses.FirstOrDefault(
                s => s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));

            if (validStatus == null)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Invalid status. Allowed statuses: Received, Preparing, Ready, Collected."
                });
            }

            var order = await _orderTableService.GetOrderAsync(
                request.OrderDate,
                request.OrderId);

            if (order == null)
            {
                return new NotFoundObjectResult(new
                {
                    error = "Order not found."
                });
            }

            try
            {
                await _orderTableService.UpdateOrderStatusAsync(
                    request.OrderDate,
                    request.OrderId,
                    validStatus);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return new NotFoundObjectResult(new
                {
                    error = "Order not found."
                });
            }

            return new OkObjectResult(new
            {
                message = "Order status updated successfully.",
                orderId = request.OrderId,
                status = validStatus
            });
        }
    }
}