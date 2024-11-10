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

        public async Task<PageResponse<CourseResponse>> GetListCourseAsync(int majorId, QueryObject queryObject)
        {
            var responses = new List<CourseResponse>();

            // Base query
            var courseQueryable = _context.Courses.AsQueryable();

            // MajorId filter
            if (majorId > 0)
                courseQueryable = courseQueryable.Where(c => c.MajorId == majorId);

            // Apply search filter
            if (!string.IsNullOrEmpty(queryObject.search))
                courseQueryable = courseQueryable.Where(c =>
                    c.CourseName!.Contains(queryObject.search) ||
                    c.CourseCode!.Contains(queryObject.search));

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
                    _ => courseQueryable.OrderByDescending(c => c.CourseId),
                };
            }

            // Get the list of majors (for mapping major data)
            var majorList = await _context.Majors.ToListAsync();

            // Apply pagination
            var totalCount = await courseQueryable.CountAsync();

            // If no records found, return empty content
            if (totalCount == 0)
            {
                return new PageResponse<CourseResponse>
                {
                    content = responses, // Empty array
                    totalElements = totalCount,
                    totalPages = 0, // No pages available
                    size = queryObject.size,
                    page = queryObject.page,
                    numberOfElements = responses.Count
                };
            }

            // Retrieve the paginated list of courses
            var courseList = await courseQueryable
                .Skip((queryObject.page - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            // Create response objects for each course in the current page
            responses.AddRange(from item in courseList
                let major = majorList.FirstOrDefault(m => m.MajorId == item.MajorId)
                select new CourseResponse
                {
                    id = item.CourseId,
                    code = item.CourseCode ?? string.Empty,
                    name = item.CourseName ?? string.Empty,
                    credit = item.CourseCredit ?? 0,
                    major = major != null
                        ? new CommonObject
                        {
                            id = major.MajorId,
                            code = major.MajorId.ToString(),
                            name = major.MajorName
                        }
                        : null
                });

            // Return paginated response
            return new PageResponse<CourseResponse>
            {
                content = responses,
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                page = queryObject.page,
                numberOfElements = responses.Count
            };
        }

        public async Task<BaseResponse<CourseResponse>> GetDetailCourseAsync(int courseId)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return new BaseResponse<CourseResponse>
                {
                    status = 404,
                    message = "Không tìm thấy học phần",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            field = "course_id",
                            message = "Không tìm thấy học phần"
                        }
                    }
                };
            }

            var major = await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == course.MajorId);

            return new BaseResponse<CourseResponse>
            {
                status = 200,
                message = "Thành công",
                data = new CourseResponse
                {
                    id = course.CourseId,
                    code = course.CourseCode,
                    name = course.CourseName,
                    credit = course.CourseCredit ?? 0,
                    major = major != null
                        ? new CommonObject
                        {
                            id = major.MajorId,
                            code = major.MajorId.ToString(),
                            name = major.MajorName
                        }
                        : null
                }
            };
        }

        public async Task<BaseResponse<List<DetailResponse>>> CreateCourseAsync(List<CourseResponse> inputCourses)
        {
            try
            {
                var errors = new List<ErrorDetail>();
                var newCourses = new List<Course>();

                // Kiểm tra trùng lặp trong inputCourses
                var duplicateNames = inputCourses.GroupBy(c => c.name)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                var duplicateCodes = inputCourses.GroupBy(c => c.code)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                for (var i = 0; i < inputCourses.Count; i++)
                {
                    var inputCourse = inputCourses[i];

                    // Kiểm tra xem course name và code có trùng trong inputCourses không
                    if (duplicateNames.Contains(inputCourse.name))
                        errors.Add(new ErrorDetail
                            { field = $"courses.{i}.name", message = "Tên học phần bị trùng lặp trong danh sách" });

                    if (duplicateCodes.Contains(inputCourse.code))
                        errors.Add(new ErrorDetail
                            { field = $"courses.{i}.code", message = "Mã học phần bị trùng lặp trong danh sách" });

                    var isExistName = await _context.Courses.AnyAsync(c => c.CourseName == inputCourse.name);
                    var isExistCode = await _context.Courses.AnyAsync(c => c.CourseCode == inputCourse.code);
                    var isValidCredit = inputCourse.credit > 0;

                    if (inputCourse.major != null)
                    {
                        var isExistMajor = await _context.Majors.AnyAsync(m => m.MajorId == inputCourse.major.id);
                        if (!isExistMajor)
                            errors.Add(new ErrorDetail { field = $"courses.{i}.major.id" });
                    }

                    if (isExistName)
                        errors.Add(new ErrorDetail
                            { field = $"courses.{i}.name", message = "Tên học phần đã tồn tại" });

                    if (isExistCode)
                        errors.Add(new ErrorDetail { field = $"courses.{i}.code", message = "Mã học phần đã tồn tại" });

                    if (!isValidCredit)
                        errors.Add(new ErrorDetail
                            { field = $"courses.{i}.credit", message = "Số tín chỉ không hợp lệ" });

                    newCourses.Add(new Course
                    {
                        CourseCode = inputCourse.code,
                        CourseName = inputCourse.name,
                        CourseCredit = inputCourse.credit,
                        MajorId = inputCourse.major?.id,
                        CreatedAt = DateTime.Now
                    });
                }

                if (errors.Any())
                {
                    return new BaseResponse<List<DetailResponse>>
                    {
                        status = 400,
                        message = "Có lỗi xảy ra",
                        errors = errors
                    };
                }

                await _context.Courses.AddRangeAsync(newCourses);
                await _context.SaveChangesAsync();

                return new BaseResponse<List<DetailResponse>>
                {
                    status = 200,
                    message = "Thêm mới học phần thành công",
                    data = newCourses.Select(c => new DetailResponse { id = c.CourseId }).ToList()
                };
            }
            catch (Exception exception)
            {
                return new BaseResponse<List<DetailResponse>>
                {
                    status = 500,
                    message = exception.Message,
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = exception.InnerException?.Message ?? exception.Message
                        }
                    }
                };
            }
        }

        public async Task<BaseResponseId> UpdateCourseAsync(CourseResponse updateCourse)
        {
            try
            {
                var errors = new List<ErrorDetail>();
                var existCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == updateCourse.id);

                if (existCourse == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy học phần",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "course_id",
                                message = "Không tìm thấy học phần"
                            }
                        }
                    };
                }

                if (updateCourse.code != existCourse.CourseCode)
                {
                    var isExistCode = await _context.Courses.AnyAsync(c => c.CourseCode == updateCourse.code);

                    if (isExistCode)
                        errors.Add(new ErrorDetail { field = "courses.0.code", message = "Mã học phần đã tồn tại" });

                    existCourse.CourseCode = updateCourse.code;
                }

                if (updateCourse.name != existCourse.CourseName)
                {
                    var isExistName = await _context.Courses.AnyAsync(c => c.CourseName == updateCourse.name);

                    if (isExistName)
                        errors.Add(new ErrorDetail { field = "courses.0.name", message = "Tên học phần đã tồn tại" });

                    existCourse.CourseName = updateCourse.name;
                }

                if (updateCourse.credit <= 0)
                    errors.Add(new ErrorDetail { field = "courses.0.credit", message = "Số tín chỉ không hợp lệ" });

                if (updateCourse.major == null)
                    existCourse.MajorId = null;

                if (updateCourse.credit != existCourse.CourseCredit)
                    existCourse.CourseCredit = updateCourse.credit;

                if (updateCourse.major != null && updateCourse.major.id != existCourse.MajorId)
                    existCourse.MajorId = updateCourse.major.id;

                if (errors.Any())
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Có lỗi xảy ra",
                        errors = errors
                    };

                existCourse.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Cập nhật học phần thành công",
                    data = new DetailResponse { id = existCourse.CourseId }
                };
            }
            catch (Exception exception)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = exception.Message,
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = exception.InnerException?.Message ?? exception.Message
                        }
                    }
                };
            }
        }

        public async Task<BaseResponseId> DeleteCourseAsync(int courseId)
        {
            try
            {
                var existCourse = await _context.Courses.FirstOrDefaultAsync(y => y.CourseId == courseId);

                if (existCourse == null)
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy học phần",
                        errors = new List<ErrorDetail>
                        {
                            new ErrorDetail
                            {
                                field = "course_id",
                                message = "Không tìm thấy học phần"
                            }
                        }
                    };

                _context.Courses.Remove(existCourse);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Xóa học phần thành công",
                    data = new DetailResponse { id = existCourse.CourseId }
                };
            }
            catch (Exception exception)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = exception.Message,
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = exception.InnerException?.Message ?? exception.Message
                        }
                    }
                };
            }
        }
    }
}