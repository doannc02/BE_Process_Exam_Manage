using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IAcademicYearRepository
    {
        Task<PageResponse<AcademicYearResponse>> GetListAcademicYearAsync(QueryObject queryObject);
        Task<BaseResponse<AcademicYearResponse>> GetDetailAcademicYearAsync(int id);
    }
}
