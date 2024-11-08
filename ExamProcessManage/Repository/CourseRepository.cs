using ExamProcessManage.Data;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
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

        public async Task<PageResponse<CourseReponse>> GetListCourseAsync(int majorId, QueryObject queryObject)
        {
            // Validate QueryObject
            if (queryObject.page <= 0)
            {
                throw new ArgumentException("Page number must be greater than zero.");
            }

            if (queryObject.size <= 0)
            {
                throw new ArgumentException("Page size must be greater than zero.");
            }

            var responses = new List<CourseReponse>();

            // Base query
            var courseQueryable = _context.Courses.AsQueryable();

            // MajorId filter
            if (majorId > 0)
            {
                courseQueryable = courseQueryable.Where(c => c.MajorId == majorId);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(queryObject.search))
            {
                courseQueryable = courseQueryable.Where(c =>
                    c.CourseName!.Contains(queryObject.search) ||
                    c.CourseCode!.Contains(queryObject.search));
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

            // Get the list of majors (for mapping major data)
            var majorList = await _context.Majors.ToListAsync();

            // Apply pagination
            var totalCount = await courseQueryable.CountAsync();

            // If no records found, return empty content
            if (totalCount == 0)
            {
                return new PageResponse<CourseReponse>
                {
                    content = responses, // Empty array
                    totalElements = totalCount,
                    totalPages = 0, // No pages available
                    size = queryObject.size,
                    page = queryObject.page!.Value,
                    numberOfElements = responses.Count
                };
            }

            // Retrieve the paginated list of courses
            var courseList = await courseQueryable
                .Skip((queryObject.page!.Value - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            // Create response objects for each course in the current page
            foreach (var item in courseList)
            {
                Major? majorCourse = null;
                foreach (var m in majorList)
                {
                    if (m.MajorId == item.MajorId)
                    {
                        majorCourse = m;
                        break;
                    }
                }

                var course = new CourseReponse
                {
                    id = item.CourseId,
                    code = item.CourseCode ?? string.Empty,
                    name = item.CourseName ?? string.Empty,
                    credit = item.CourseCredit ?? 0,
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


        public async Task<BaseResponse<CourseReponse>> GetDetailCourseAsync(int courseId)
        {
            var response = new BaseResponse<CourseReponse>();
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course != null)
            {
                var major = await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == course.MajorId);

                response.message = "success";
                response.data = new CourseReponse
                {
                    id = course.CourseId,
                    code = course.CourseCode ?? string.Empty,
                    name = course.CourseName ?? string.Empty,
                    credit = course.CourseCredit ?? 0,
                    major = new CommonObject
                    {
                        id = major?.MajorId ?? 0,
                        code = major?.MajorId.ToString(),
                        name = major?.MajorName ?? string.Empty,
                    }
                };
            }
            else
            {
                response.message = $"course with id = '{courseId}' could not be found";
            }

            return response;
        }

        public async Task<BaseResponse<List<CourseReponse>>> CreateCourseAsync(List<CourseReponse> inputCourses)
        {
            var errors = new List<ErrorDetail>();
            try
            {
                var response = new BaseResponse<List<CourseReponse>>
                {
                    data = new List<CourseReponse>()
                };

                var newCourses = new List<Course>();

                for (int i = 0; i < inputCourses.Count; i++)
                {
                    var inputCourse = inputCourses[i];
                    var existCourseName = await _context.Courses.AnyAsync(c => c.CourseCode == inputCourse.name );
                    errors.Add(new ErrorDetail { field = $"course.${i}.name", message = "Tên học phần đã tồn tại!" });
                    var existCourseCode = await _context.Courses.AnyAsync(c => c.CourseCode == inputCourse.code );
                    errors.Add(new ErrorDetail { field = $"course.${i}.code", message = "Mã học phần đã tồn tại!" });
                    if (!existCourseName || !existCourseCode )
                    {
                        var newCourse = new Course
                        {
                            CourseCode = inputCourse.code,
                            CourseName = inputCourse.name,
                            CourseCredit = inputCourse.credit,
                            MajorId = inputCourse.major.id 
                        };

                        newCourses.Add(newCourse);
                        response.data.Add(inputCourse);
                    }
                    else
                    {
                       
                        return new BaseResponse<List<CourseReponse>>()
                        {
                            message = "Thất bại",
                            data = null,
                            errors = errors
                        };
                    }
                }

                if (newCourses.Any())
                {
                    await _context.Courses.AddRangeAsync(newCourses);
                    await _context.SaveChangesAsync();

                    response.message = "Courses added successfully";
                }

                return response;
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<CourseReponse>>
                {
                    message = "An error occurred: " + ex.Message
                };
            }
        }

        public async Task<BaseResponse<CourseReponse>> UpdateCourseAsync(CourseReponse updateCourse)
        {
            try
            {
                var response = new BaseResponse<CourseReponse>();
                var existCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == updateCourse.id);

                if (existCourse != null)
                {
                            await _context.Courses.AnyAsync(c => c.CourseName == updateCourse.name);
                            existCourse.CourseName = updateCourse.name;
                            existCourse.CourseCredit = updateCourse.credit;
                            existCourse.MajorId = updateCourse.major.id > 0 ? updateCourse.major.id : existCourse.MajorId;
                            await _context.SaveChangesAsync();
                            response.message = "update successfully";
                            response.data = updateCourse;
                }
                else
                {
                    response.message = $"no course found with id = '{updateCourse.id}'";
                }

                return response;
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseReponse>
                {
                    message = "an error occurred: " + ex.Message
                };
            }
        }

        public async Task<BaseResponse<CourseReponse>> DeleteCourseAsync(int courseId)
        {
            try
            {
                var response = new BaseResponse<CourseReponse>();
                var existCourse = await _context.Courses.FirstOrDefaultAsync(y => y.CourseId == courseId);

                if (existCourse != null)
                {
                    _context.Courses.Remove(existCourse);
                    await _context.SaveChangesAsync();

                    response.message = "delete successfully";
                    response.data = new CourseReponse
                    {
                        id = existCourse.CourseId,
                        code = existCourse.CourseCode,
                        name = existCourse.CourseName,
                        credit = (int)existCourse.CourseCredit!,
                        major = new CommonObject { id = (int)existCourse.MajorId! }
                    };
                }
                else
                {
                    response.message = $"no course found with ID = '{courseId}'";
                }

                return response;
            }
            catch (Exception ex)
            {
                return new BaseResponse<CourseReponse>
                {
                    message = "an error occurred: " + ex.Message
                };
            }
        }
    }
}
