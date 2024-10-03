using ExamProcessManage.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Interfaces
{
    public interface IBaseRepository
    {
        Task<BaseResponse<int>> UpdateState(int examId, string status, string? comment);
    }
}
