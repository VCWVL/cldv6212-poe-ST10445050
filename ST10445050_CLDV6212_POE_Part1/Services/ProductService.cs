using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ST10445050_CLDV6212_POE_Part1.Services
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private readonly string _functionBaseUrl;

        public ProductService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var baseUrl = configuration["FunctionApi:ProductsBaseUrl"];
            var key = configuration["FunctionApi:ProductsFunctionKey"];

            _httpClient.DefaultRequestHeaders.Add("x-functions-key", key);
            _functionBaseUrl = baseUrl;
        }

        public async Task UploadProductAsync(ProductUploadModel product)
        {
            var json = JsonConvert.SerializeObject(product);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_functionBaseUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var respContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload product: {respContent}");
            }
        }

        public async Task<List<ProductUploadModel>> GetAllProductsAsync()
        {
            var response = await _httpClient.GetAsync(_functionBaseUrl);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to get products: {response.ReasonPhrase}");

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ProductUploadModel>>(content) ?? new List<ProductUploadModel>();
        }
    }

    public class ProductUploadModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageBase64 { get; set; }
    }
}
