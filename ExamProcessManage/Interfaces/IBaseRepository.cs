using ExamProcessManage.Helpers;

namespace ExamProcessManage.Interfaces
{
    public interface IBaseRepository
    {
        Task<BaseResponseId> UpdateStateAsync(int id, string status, string? comment);
        Task<BaseResponseId> RemoveChildAsync(int parentId, int childId, string? comment);
    }
}
