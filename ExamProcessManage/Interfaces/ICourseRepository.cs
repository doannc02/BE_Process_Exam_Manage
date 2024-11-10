using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface ICourseRepository
    {
        Task<PageResponse<CourseResponse>> GetListCourseAsync(int majorId, QueryObject queryObject);
        Task<BaseResponse<CourseResponse>> GetDetailCourseAsync(int courseId);
        Task<BaseResponse<List<DetailResponse>>> CreateCourseAsync(List<CourseResponse> inputCourses);
        Task<BaseResponseId> UpdateCourseAsync(CourseResponse updateCourse);
        Task<BaseResponseId> DeleteCourseAsync(int courseId);
    }
}