using Microsoft.AspNetCore.Mvc;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class FileModelsController : Controller
    {
        private readonly FileStorageService _fileStorageService;
        private readonly HttpClient _httpClient;

        // Constructor to inject the services
        public FileModelsController(FileStorageService fileStorageService, HttpClient httpClient)
        {
            _fileStorageService = fileStorageService;
            _httpClient = httpClient;
        }

        // =========================
        // LIST FILES
        // =========================
        public async Task<IActionResult> Index()
        {
            try
            {
                var files = await _fileStorageService.ListFilesAsync("uploads");
                return View(files);
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error fetching files: {ex.Message}";
                return View(new List<FileModel>());
            }
        }

        // =========================
        // UPLOAD FILE
        // =========================
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "Please select a valid file.";
                return RedirectToAction(nameof(Index));
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            try
            {
                // First, upload the file to Azure File Storage
                string uploadResult = await _fileStorageService.UploadFileAsync("uploads", file.FileName, fileBytes);

                // Trigger the Azure Function to process the uploaded file
                var fileUploadRequest = new
                {
                    FileName = file.FileName,
                    FileContent = Convert.ToBase64String(fileBytes)
                };

                var content = new StringContent(JsonConvert.SerializeObject(fileUploadRequest), Encoding.UTF8, "application/json");

                var functionUrl = "https://st10445050-abcretails.azurewebsites.net"; 

                var response = await _httpClient.PostAsync(functionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = $"File '{file.FileName}' uploaded and processed successfully.";
                }
                else
                {
                    TempData["Message"] = $"Error triggering the Azure Function: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error uploading file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DOWNLOAD FILE
        // =========================
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest();

            try
            {
                var fileStream = await _fileStorageService.DownloadFileAsync("uploads", fileName);
                if (fileStream != null)
                {
                    return File(fileStream, "application/octet-stream", fileName);
                }

                TempData["Message"] = $"File '{fileName}' not found.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error downloading file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE FILE
        // =========================
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                TempData["Message"] = "Invalid file name.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                bool result = await _fileStorageService.DeleteFileAsync("uploads", fileName);
                TempData["Message"] = result ? $"File '{fileName}' deleted successfully!" : $"File '{fileName}' not found.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
