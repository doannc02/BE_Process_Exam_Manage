using ExamProcessManage.Data;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class CourseRepository : ICourseRepository
    {
        private readonly ApplicationDbContext _context;

        public CourseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<CourseReponse>> GetListCourseAsync(QueryObject queryObject)
        {
            var responses = new List<CourseReponse>();

            // Base query
            var courseQueryable = _context.Courses.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(queryObject.search))
            {
                courseQueryable = courseQueryable.Where(c =>
                    c.CourseName.Contains(queryObject.search) ||
                    c.CourseCode.Contains(queryObject.search));
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(queryObject.sort))
            {
                courseQueryable = queryObject.sort.ToLower() switch
                {
                    "name" => courseQueryable.OrderBy(c => c.CourseName),
                    "name_desc" => courseQueryable.OrderByDescending(c => c.CourseName),
                    "code" => courseQueryable.OrderBy(c => c.CourseCode),
                    "code_desc" => courseQueryable.OrderByDescending(c => c.CourseCode),
                    "credit" => courseQueryable.OrderBy(c => c.CourseCredit),
                    "credit_desc" => courseQueryable.OrderByDescending(c => c.CourseCredit),
                    _ => courseQueryable.OrderBy(c => c.CourseId),
                };
            }

            // Get the list of majors
            var majorList = await _context.Majors.ToListAsync();

            // Apply pagination
            var totalCount = await courseQueryable.CountAsync();
            var courseList = await courseQueryable
                .Skip((queryObject.page.Value - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            // Create response objects for each course in the current page
            foreach (var item in courseList)
            {
                var majorCourse = majorList.FirstOrDefault(m => m.MajorId == item.MajorId);
                var course = new CourseReponse
                {
                    course_id = item.CourseId,
                    course_code = item.CourseCode ?? string.Empty,
                    course_name = item.CourseName ?? string.Empty,
                    course_credit = (int)item.CourseCredit,
                    major = new CommonObject
                    {
                        id = majorCourse?.MajorId ?? 0,  // Handle potential null value
                        code = majorCourse?.MajorId.ToString() ?? string.Empty,
                        name = majorCourse?.MajorName ?? string.Empty
                    }
                };
                responses.Add(course);
            }

            // Return paginated response
            return new PageResponse<CourseReponse>
            {
                content = responses,
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                page = queryObject.page.Value,
                numberOfElements = responses.Count
            };
        }

        //public async Task<PageResponse<CourseReponse>> GetListCourseAsync(QueryObject queryObject)
        //{
        //    var responses = new List<CourseReponse>();
        //    var courseQueryable = _context.Courses.AsQueryable();
        //    var majorList = await _context.Majors.ToListAsync();
        //    var totalCount = await courseQueryable.CountAsync();
        //    var courseList = await courseQueryable.Skip((queryObject.page.Value - 1) * queryObject.size)
        //        .Take(queryObject.size)
        //        .ToListAsync();

        //    foreach (var item in courseQueryable)
        //    {
        //        var majorCourse = majorList.FirstOrDefault(m => m.MajorId == item.MajorId);
        //        var course = new CourseReponse
        //        {
        //            course_id = item.CourseId,
        //            course_code = item.CourseCode ?? string.Empty,
        //            course_name = item.CourseName ?? string.Empty,
        //            course_credit = (int)item.CourseCredit,
        //            major = new CommonObject
        //            {
        //                id = majorCourse.MajorId,
        //                code = majorCourse.MajorId.ToString(),
        //                name = majorCourse.MajorName
        //            }
        //        };
        //        responses.Add(course);
        //    }

        //    return new PageResponse<CourseReponse>
        //    {
        //        content = responses,
        //        totalElements = totalCount,
        //        totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
        //        size = queryObject.size,
        //        page = queryObject.page.Value,
        //        numberOfElements = responses.Count
        //    };
        //}

        public Task<BaseResponse<CourseReponse>> GetDetailCourseAsync(int courseId)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<CourseReponse>> CreateCourseAsync(CourseReponse newCourse)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<CourseReponse>> UpdateCourseAsync(CourseReponse updateCourse)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<CourseReponse>> DeleteCourseAsync(int courseId)
        {
            throw new NotImplementedException();
        }
    }
}
