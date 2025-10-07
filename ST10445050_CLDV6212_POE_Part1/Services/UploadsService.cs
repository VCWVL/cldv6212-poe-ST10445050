using Newtonsoft.Json;
using ST10445050_CLDV6212_POE_Part1.Models;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ST10445050_CLDV6212_POE_Part1.Services
{
    public class UploadsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _functionKey;

        public UploadsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["FunctionApi:UploadsBaseUrl"]
                       ?? throw new InvalidOperationException("UploadsBaseUrl missing.");
            _functionKey = configuration["FunctionApi:UploadsFunctionKey"]
                           ?? throw new InvalidOperationException("UploadsFunctionKey missing.");
        }

        public async Task<string> UploadFileAsync(string fileName, byte[] fileBytes)
        {
            var request = new
            {
                FileName = fileName,
                FileContent = Convert.ToBase64String(fileBytes)
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", _functionKey);

            var response = await _httpClient.PostAsync(_baseUrl, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<FileModel>> GetFilesAsync()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", _functionKey);

            var response = await _httpClient.GetAsync(_baseUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(json);
            var files = new List<FileModel>();

            foreach (var f in result.Files)
            {
                files.Add(new FileModel
                {
                    fileName = f.fileName,
                    fileSize = f.fileSize,
                    LastModified = f.LastModified
                });
            }

            return files;
        }
    }
}
