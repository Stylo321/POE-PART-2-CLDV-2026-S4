using Azure.Data.Tables;
using CoffeeAndChill.Models;
using System.Text.Json;
using Azure;

namespace CoffeeAndChill.Services
{
    public class OrderTableService
    {
        private readonly TableClient _tableClient;

        public OrderTableService()
        {
            string connectionString =
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException("AzureWebJobsStorage is missing.");

            _tableClient = new TableClient(connectionString, "Orders");
        }

        public async Task CreateTableAsync() =>
            await _tableClient.CreateIfNotExistsAsync();

        public async Task AddOrderAsync(Order order)
        {
            await CreateTableAsync();

            order.PartitionKey = order.OrderTimestamp.UtcDateTime.ToString("yyyy-MM-dd");
            order.RowKey = order.OrderId;

            var entity = new TableEntity(order.PartitionKey, order.RowKey)
            {
                ["OrderId"] = order.OrderId,
                ["CustomerName"] = order.CustomerName,
                ["SelectedItemSKUs"] = JsonSerializer.Serialize(order.SelectedItemSKUs),
                ["TotalPrice"] = order.TotalPrice,
                ["OrderTimestamp"] = order.OrderTimestamp,
                ["Status"] = order.Status
            };


            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        public async Task UpdateOrderStatusAsync(string orderDate, string orderId, string status)
        {
            var entity = await _tableClient.GetEntityAsync<TableEntity>(orderDate, orderId);
            entity.Value["Status"] = status;
            await _tableClient.UpdateEntityAsync(entity.Value, entity.Value.ETag, TableUpdateMode.Replace);
        }

        public async Task<TableEntity?> GetOrderAsync(string orderDate, string orderId)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<TableEntity>(orderDate, orderId);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;   
            }
        }
    }
}