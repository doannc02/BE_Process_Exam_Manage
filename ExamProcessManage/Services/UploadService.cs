using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using ExamProcessManage.Interfaces;

namespace ExamProcessManage.Services
{
    public class UploadService : IUploadFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public UploadService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
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

            // Đường dẫn đến thư mục wwwroot/files
            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "files");

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Tạo tên file duy nhất (để tránh trùng lặp)
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadPath, uniqueFileName);

            // Lưu file vào wwwroot/files
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
           // var settingValue = _configuration["ngrok"];  // "Value1"

            // Lấy thông tin về Request để tạo URL cho file
            var request = _httpContextAccessor.HttpContext.Request;
            string fileUrl = $"{request.Scheme}://{request.Host}/files/{uniqueFileName}";
            //string fileUrl = $"{settingValue}/files/{uniqueFileName}";
            //string fileUrl = Path.Combine(settingValue,"files", uniqueFileName);

            // return fileUrl;
            return new string(fileUrl);
        }

        public async Task<bool> DeleteFile(string fileName)
        {
            // Đường dẫn đến thư mục files trong wwwroot
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "files", fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }

            return false;
        }
    }
}
