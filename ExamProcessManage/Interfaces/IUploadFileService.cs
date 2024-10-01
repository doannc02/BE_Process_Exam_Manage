namespace ExamProcessManage.Interfaces
{
    public interface IUploadFileService
    {
        Task<string> UploadFile(IFormFile file);
        Task<bool> DeleteFile(string url);
    }
}
