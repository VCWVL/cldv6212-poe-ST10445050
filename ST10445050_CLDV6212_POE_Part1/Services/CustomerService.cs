using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ST10445050_CLDV6212_POE_Part1.Models;

namespace ST10445050_CLDV6212_POE_Part1.Services
{
    public class CustomerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CustomerService> _logger;

        // Replace this with your actual Function URL
        private readonly string _functionBaseUrl = "https://st10445050-abcretails.azurewebsites.net/api/customers/add?code=sUM7f0tG309Gx04vG-oZ9ky2Iz5TF53X37mLm3fdCTBlAzFuUGIemw==";


        public CustomerService(HttpClient httpClient, ILogger<CustomerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> AddCustomerAsync(Customer customer)
        {
            try
            {
                string json = JsonConvert.SerializeObject(customer);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Replace 'FunctionBaseUrl' with the correct field name '_functionBaseUrl'
                var response = await _httpClient.PostAsync(_functionBaseUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to add customer: {error}");
                return $"Error: {error}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in AddCustomerAsync: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }
    }
}
