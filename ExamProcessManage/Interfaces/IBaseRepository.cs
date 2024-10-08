using ExamProcessManage.Helpers;

namespace ExamProcessManage.Interfaces
{
    public interface IBaseRepository
    {
        Task<BaseResponseId> UpdateStateAsync(int examId, string status, string? comment);
        Task<BaseResponseId> RemoveChildAsync(int examSetId, int examId, string? comment);
    }
}
