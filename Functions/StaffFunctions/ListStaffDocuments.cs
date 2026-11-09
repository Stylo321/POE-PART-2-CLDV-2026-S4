
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoffeeAndChill.Functions.StaffFunctions
{
    public class ListStaffDocuments
    {
        [Function("ListStaffDocuments")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "staff-documents")]
            HttpRequest req)
        {
            string connectionString =
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "AzureWebJobsStorage is missing.");

            BlobServiceClient blobServiceClient =
                new BlobServiceClient(connectionString);

            BlobContainerClient containerClient =
                blobServiceClient.GetBlobContainerClient("staff-docs");

            // Create the container if it does not exist.
            await containerClient.CreateIfNotExistsAsync();

            var documents = new List<object>();

            // Retrieve all documents stored in the container.
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                documents.Add(new
                {
                    fileName = blobItem.Name,
                    size = blobItem.Properties.ContentLength,
                    lastModified = blobItem.Properties.LastModified
                });
            }

            return new OkObjectResult(documents);
        }
    }
}
