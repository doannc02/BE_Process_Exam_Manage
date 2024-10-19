using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.RequestModels;

namespace ExamProcessManage.Interfaces
{
    public interface IExamSetRepository
    {
        Task<PageResponse<ExamSetDTO>> GetListExamSetAsync(int? userId, RequestParamsExamSets queryObject);
        Task<BaseResponse<ExamSetDTO>> GetDetailExamSetAsync(int? userId, int id);
        Task<BaseResponseId> CreateExamSetAsync(int userId, ExamSetDTO examSetDTO);
        Task<BaseResponseId> UpdateExamSetAsync(int userId, ExamSetDTO examSetDTO);
        Task<BaseResponseId> DeleteExamSetAsync(int userId, int examSetId, bool examSetOnly);
    }
}
