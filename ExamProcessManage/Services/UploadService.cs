using ExamProcessManage.Interfaces;

namespace ExamProcessManage.Services
{
    public class UploadService : IUploadFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public UploadService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task<string> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return "No file was uploaded.";
            }

            // New upload path: /var/www/html/pdf-files
            var uploadPath = "/var/www/html/pdf-files";

            // Create the directory if it doesn't exist
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate a unique file name to avoid conflicts
            var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Save the file to the specified directory
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Console.WriteLine($"uploadPath: {uploadPath}");

            // Construct the file URL
            var fileUrl = $"/pdf-files/{uniqueFileName}";

            return fileUrl;
        }

        public Task<bool> DeleteFile(string fileName)
        {
            // Path to /var/www/html/pdf-files
            var filePath = Path.Combine("/var/www/html/pdf-files", fileName);

            if (!File.Exists(filePath)) return Task.FromResult(false);
            File.Delete(filePath);
            return Task.FromResult(true);
        }
    }
}