using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<PageResponse<DepartmentResponse>> GetListDepartmentAsync(QueryObject queryObject);
        Task<BaseResponse<DepartmentResponse>> GetDetailDepartmentAsync(int id);
        Task<BaseResponseId> CreateDepartmentAsync(DepartmentResponse department);
        Task<BaseResponseId> UpdateDepartmentAsync(DepartmentResponse department);
        Task<BaseResponseId> DeleteDepartmentAsync(int yearId);
    }
}