using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface ICourseRepository
    {
        Task<PageResponse<CourseReponse>> GetListCourseAsync(int majorId, QueryObject queryObject);
        Task<BaseResponse<CourseReponse>> GetDetailCourseAsync(int courseId);
        Task<BaseResponse<CourseReponse>> CreateCourseAsync(CourseReponse newCourse);
        Task<BaseResponse<CourseReponse>> UpdateCourseAsync(CourseReponse updateCourse);
        Task<BaseResponse<CourseReponse>> DeleteCourseAsync(int courseId);
    }
}
