using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace CoffeeAndChill.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/menu")]
    public class MenuController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MenuController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMenuItems()
        {
            var client = _httpClientFactory.CreateClient("Functions");

            var response = await client.GetAsync("api/menu");

            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString()
                    ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetMenuItemsByCategory(string category)
        {
            var client = _httpClientFactory.CreateClient("Functions");

            var response = await client.GetAsync(
                $"api/menu/category/{Uri.EscapeDataString(category)}");

            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString()
                    ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> CreateMenuItem(
            [FromBody] JsonElement requestBody)
        {
            var client = _httpClientFactory.CreateClient("Functions");

            var json = JsonSerializer.Serialize(requestBody);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("api/menu", content);

            var responseBody = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = responseBody,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        
        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{category}/{id}")]
        public async Task<IActionResult> UpdateMenuItem(
            string category,
            string id,
            [FromBody] JsonElement requestBody)
        {
            var client = _httpClientFactory.CreateClient("Functions");

            var json = JsonSerializer.Serialize(requestBody);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(
                $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}",
                content);

            var responseBody = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = responseBody,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
       
        [Authorize(Roles = "Admin,Staff")]
        [HttpDelete("{category}/{id}")]
        public async Task<IActionResult> DeleteMenuItem(
            string category,
            string id)
        {
            var client = _httpClientFactory.CreateClient("Functions");

            var response = await client.DeleteAsync(
                $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}");

            var responseBody = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                Content = responseBody,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
    }
}