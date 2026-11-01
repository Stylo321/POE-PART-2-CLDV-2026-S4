using Azure.Data.Tables;
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.Services
{
    public class UserProfileService
    {
        private readonly TableClient _tableClient;

        public UserProfileService(IConfiguration configuration)
        {
            string connectionString =
                configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "AzureStorage connection string is missing.");

            _tableClient = new TableClient(
                connectionString,
                "Users");
        }

        public async Task EnsureTableExistsAsync()
        {
            await _tableClient.CreateIfNotExistsAsync();
        }

        public async Task<UserProfile?> FindByEmailAsync(string email)
        {
            await EnsureTableExistsAsync();

            string normalizedEmail = email.Trim().ToLowerInvariant();

            // Escape apostrophes for the Azure Tables OData filter.
            string escapedEmail = normalizedEmail.Replace("'", "''");

            string filter = $"RowKey eq '{escapedEmail}'";

            await foreach (UserProfile user in
                _tableClient.QueryAsync<UserProfile>(filter: filter))
            {
                return user;
            }

            return null;
        }

        public async Task CreateAsync(UserProfile user)
        {
            await EnsureTableExistsAsync();

            await _tableClient.AddEntityAsync(user);
        }
    }
}