using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IMajorRepository
    {
        Task<PageResponse<MajorResponse>> GetListMajorAsync(int departmentId, QueryObject queryObject);
        Task<BaseResponse<MajorResponse>> GetDetailMajorAsync(int majorId);
        Task<BaseResponse<MajorResponse>> CreateMajorAsync(MajorResponse newMajor);
        Task<BaseResponse<MajorResponse>> UpdateMajorAsync(MajorResponse updateMajor);
        Task<BaseResponse<MajorResponse>> DeleteMajorAsync(int majorId);
    }
}
