using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<PageResponse<DepartmentResponse>> GetListDepartmentAsync(QueryObject queryObject);
        Task<BaseResponse<DepartmentResponse>> GetDetailDepartmentAsync(int id);
        Task<BaseResponse<DepartmentResponse>> CreateDepartmentAsync(DepartmentResponse academicYear);
        Task<BaseResponse<DepartmentResponse>> UpdateDepartmentAsync(DepartmentResponse academicYear);
        Task<BaseResponse<DepartmentResponse>> DeleteDepartmentAsync(int yearId);
    }
}
