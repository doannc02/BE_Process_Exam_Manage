﻿using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface ICourseRepository
    {
        Task<PageResponse<CourseReponse>> GetListCourseAsync(int majorId, QueryObject queryObject);
        Task<BaseResponse<CourseReponse>> GetDetailCourseAsync(int courseId);
        Task<BaseResponse<List<CourseReponse>>> CreateCourseAsync(List<CourseReponse> inputCourses);
        Task<BaseResponse<CourseReponse>> UpdateCourseAsync(CourseReponse updateCourse);
        Task<BaseResponse<CourseReponse>> DeleteCourseAsync(int courseId);
    }
}
