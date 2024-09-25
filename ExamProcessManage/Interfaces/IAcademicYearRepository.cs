using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IAcademicYearRepository
    {
        Task<PageResponse<AcademicYearResponse>> GetListAcademicYearAsync(QueryObject queryObject);
        Task<BaseResponse<AcademicYearResponse>> GetDetailAcademicYearAsync(int id);
        Task<BaseResponse<AcademicYearResponse>> CreateAcademicYearAsync(AcademicYearResponse academicYear);
        Task<BaseResponse<AcademicYearResponse>> UpdateAcademicYearAsync(AcademicYearResponse academicYear);
        Task<BaseResponse<AcademicYearResponse>> DeleteAcademicYearAsync(int yearId);
    }
}
