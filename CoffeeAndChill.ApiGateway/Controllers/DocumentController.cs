
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;

namespace CoffeeAndChill.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/documents")]
    [Authorize(Roles = "Admin,Staff")]
    public class DocumentController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DocumentController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Uploads a staff document to Azure Blob Storage.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select a file."
                });
            }

            var client = _httpClientFactory.CreateClient("Functions");

            using var multipartContent = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType);

            multipartContent.Add(
                fileContent,
                "file",
                file.FileName);

            var response = await client.PostAsync(
                "api/staff-documents/upload",
                multipartContent);

            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString()
                    ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        /// <summary>
        /// Retrieves a list of uploaded staff documents.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            var client = _httpClientFactory.CreateClient("Functions");

            var response = await client.GetAsync(
                "api/staff-documents");

            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString()
                    ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        /// <summary>
        /// Downloads a specific staff document.
        /// </summary>
        [HttpGet("{fileName}")]
        public async Task<IActionResult> DownloadDocument(string fileName)
        {
            var client = _httpClientFactory.CreateClient("Functions");

            var response = await client.GetAsync(
                $"api/staff-documents/{Uri.EscapeDataString(fileName)}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                return new ContentResult
                {
                    Content = error,
                    ContentType = response.Content.Headers.ContentType?.ToString()
                        ?? "application/json",
                    StatusCode = (int)response.StatusCode
                };
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();

            var contentType =
                response.Content.Headers.ContentType?.ToString()
                ?? "application/octet-stream";

            return File(bytes, contentType, fileName);
        }

        /// <summary>
        /// Deletes a specific staff document (Admin only).
        /// </summary>
        [HttpDelete("{fileName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDocument(string fileName)
        {
            var client = _httpClientFactory.CreateClient("Functions");

            var response = await client.DeleteAsync(
                $"api/staff-documents/{Uri.EscapeDataString(fileName)}");

            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString()
                    ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
    }
}

