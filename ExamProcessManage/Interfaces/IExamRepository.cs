using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.RequestModels;

namespace ExamProcessManage.Interfaces
{
    public interface IExamRepository : IBaseRepository
    {
        Task<PageResponse<ExamDTO>> GetListExamAsync(ExamRequestParams examRequest);
        Task<BaseResponse<ExamDTO>> GetExamAsync(int examId);
        Task<BaseResponse<int>> CreateExamAsync(ExamDTO examDTO);
        Task<BaseResponse<int>> UpdateExamAsync(ExamDTO examDTO);
        Task<BaseResponse<string>> DeleteExamAsync(int examId);
    }
}
