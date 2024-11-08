using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IMajorRepository
    {
        Task<PageResponse<MajorResponse>> GetListMajorAsync(int departmentId, QueryObject queryObject);
        Task<BaseResponse<MajorResponse>> GetDetailMajorAsync(int majorId);
        Task<BaseResponseId> CreateMajorAsync(MajorResponse major);
        Task<BaseResponseId> UpdateMajorAsync(MajorResponse major);
        Task<BaseResponseId> DeleteMajorAsync(int majorId);
    }
}