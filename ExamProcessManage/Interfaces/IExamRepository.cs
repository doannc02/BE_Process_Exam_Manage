using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.RequestModels;

namespace ExamProcessManage.Interfaces
{
    public interface IExamRepository : IBaseRepository
    {
        Task<PageResponse<ExamDTO>> GetListExamsAsync(ExamRequestParams examRequest, int? userId);
        Task<BaseResponse<ExamDTO>> GetDetailExamAsync(int examId);
        Task<BaseResponse<List<DetailResponse>>> CreateExamsAsync(List<ExamDTO> examDTOs, int userId);
        Task<BaseResponseId> UpdateExamAsync(ExamDTO examDTO, int userId);
        Task<BaseResponse<string>> DeleteExamAsync(int examId);
    }
}
