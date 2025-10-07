using Microsoft.AspNetCore.Mvc;
using ST10445050_CLDV6212_POE_Part1.Models;
using ST10445050_CLDV6212_POE_Part1.Services;

namespace ST10445050_CLDV6212_POE_Part1.Controllers
{
    public class FileModelsController : Controller
    {
        // =========================
        // Dependencies & Constants
        // =========================

        private readonly UploadsService _uploadsService; // Handles file operations via Azure Function

        // Directory name in Azure File Share
        private const string DirectoryName = "uploads";

        // Constructor: receive UploadsService via DI
        public FileModelsController(UploadsService uploadsService)
        {
            _uploadsService = uploadsService;
        }

        // =========================
        // LIST FILES
        // =========================
        // GET: /Files
        public async Task<IActionResult> Index()
        {
            try
            {
                var files = await _uploadsService.GetFilesAsync();
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
        // POST: /Files/UploadFile
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
                string result = await _uploadsService.UploadFileAsync(file.FileName, fileBytes);
                TempData["Message"] = result;
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
        // GET: /Files/DownloadFile?fileName=xyz
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest();

            try
            {
                string connectionString = Environment.GetEnvironmentVariable("FileShareConnectionString") ?? "";
                var shareClient = new Azure.Storage.Files.Shares.ShareClient(connectionString, DirectoryName);
                var directoryClient = shareClient.GetDirectoryClient(DirectoryName);
                var fileClient = directoryClient.GetFileClient(fileName);

                // Download the file
                var downloadResponse = await fileClient.DownloadAsync();
                var stream = new MemoryStream();
                await downloadResponse.Value.Content.CopyToAsync(stream);
                stream.Position = 0;

                return File(stream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error downloading file: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }


        // =========================
        // DELETE FILE
        // =========================
        // GET: /Files/DeleteFile?fileName=xyz
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                TempData["Message"] = "Invalid file name.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                string connectionString = Environment.GetEnvironmentVariable("FileShareConnectionString") ?? "";
                var shareClient = new Azure.Storage.Files.Shares.ShareClient(connectionString, DirectoryName);
                var directoryClient = shareClient.GetDirectoryClient(DirectoryName);
                var fileClient = directoryClient.GetFileClient(fileName);

                bool deleted = await fileClient.DeleteIfExistsAsync();

                TempData["Message"] = deleted
                    ? $"File '{fileName}' deleted successfully!"
                    : $"File '{fileName}' not found.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
