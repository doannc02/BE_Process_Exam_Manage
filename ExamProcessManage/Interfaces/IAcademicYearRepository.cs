using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IAcademicYearRepository
    {
        Task<PageResponse<AcademicYearResponse>> GetListAcademicYearAsync(QueryObject queryObject);
        Task<BaseResponse<AcademicYearResponse>> GetDetailAcademicYearAsync(int id);
        Task<BaseResponseId> CreateAcademicYearAsync(AcademicYearResponse academicYear);
        Task<BaseResponseId> UpdateAcademicYearAsync(AcademicYearResponse academicYear);
        Task<BaseResponseId> DeleteAcademicYearAsync(int yearId);
    }
}
