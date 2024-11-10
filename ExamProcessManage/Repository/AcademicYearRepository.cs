using System.Text.RegularExpressions;
using ExamProcessManage.Data;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class AcademicYearRepository : IAcademicYearRepository
    {
        private readonly ApplicationDbContext _context;
        private const string YearPattern = @"^20\d{2}$";

        public AcademicYearRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<AcademicYearResponse>> GetListAcademicYearAsync(QueryObject queryObject)
        {
            var yearResponses = new List<AcademicYearResponse>();
            var baseQuery = _context.AcademicYears.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(queryObject.search))
            {
                baseQuery = baseQuery.Where(a => a.YearName!.Contains(queryObject.search));
            }

            if (!string.IsNullOrEmpty(queryObject.sort))
            {
                baseQuery = queryObject.sort.ToLower() switch
                {
                    "name" => baseQuery.OrderBy(a => a.YearName),
                    "name_desc" => baseQuery.OrderByDescending(a => a.YearName),
                    _ => baseQuery.OrderByDescending(a => a.AcademicYearId), // Default sorting
                };
            }

            // Đếm tổng số bản ghi
            var totalCount = await baseQuery.CountAsync();

            // Nếu không có bản ghi nào, trả về PageResponse với content là mảng rỗng
            if (totalCount == 0)
            {
                return new PageResponse<AcademicYearResponse>()
                {
                    content = yearResponses, // Mảng rỗng
                    totalElements = totalCount,
                    totalPages = 0, // Không có trang nào
                    size = queryObject.size,
                    page = queryObject.page,
                    numberOfElements = yearResponses.Count
                };
            }

            // Lấy danh sách bản ghi theo phân trang
            var listAcademicYears = await baseQuery
                .Skip((queryObject.page - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            yearResponses.AddRange(listAcademicYears.Select(item => new AcademicYearResponse()
            {
                id = item.AcademicYearId, name = item.YearName, start_year = (int)item.StartYear!,
                end_year = (int)item.EndYear!
            }));

            return new PageResponse<AcademicYearResponse>
            {
                content = yearResponses, // Mảng chứa kết quả
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                page = queryObject.page,
                numberOfElements = yearResponses.Count
            };
        }

        public async Task<BaseResponse<AcademicYearResponse>> GetDetailAcademicYearAsync(int id)
        {
            var academicYear = await _context.AcademicYears.FindAsync(id);

            if (academicYear == null)
                return new BaseResponse<AcademicYearResponse>
                {
                    status = 404,
                    message = "Không tìm thấy năm học",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = "Không tìm thấy năm học"
                        }
                    }
                };

            var yearArr = academicYear.YearName.Split('-');

            return new BaseResponse<AcademicYearResponse>
            {
                status = 200,
                message = "Thành công",
                data = new AcademicYearResponse
                {
                    id = academicYear.AcademicYearId,
                    name = academicYear.YearName,
                    start_year = academicYear.StartYear ?? int.Parse(yearArr[0]),
                    end_year = academicYear.EndYear ?? int.Parse(yearArr[1])
                }
            };
        }

        public async Task<BaseResponseId> CreateAcademicYearAsync(AcademicYearResponse year)
        {
            try
            {
                // Năm học hợp lệ 2000 - 2099
                if (year.start_year >= year.end_year ||
                    year.end_year != year.start_year + 1 ||
                    !Regex.IsMatch(year.start_year.ToString(), YearPattern))
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Năm học không hợp lệ",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "start_year",
                                message = $"Năm học không hợp lệ '{year.start_year}-{year.end_year}'"
                            }
                        }
                    };
                }

                var yearName = $"{year.start_year}-{year.end_year}";

                if (year.name != yearName)
                    year.name = yearName;

                var existYear = await _context.AcademicYears.AnyAsync(a => a.YearName == year.name);

                if (existYear)
                {
                    return new BaseResponseId
                    {
                        status = 409,
                        message = "Năm học bị trùng",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = $"Năm học bị trùng '{year.name}'"
                            }
                        }
                    };
                }

                var academicYear = new AcademicYear
                {
                    YearName = year.name,
                    StartYear = year.start_year,
                    EndYear = year.end_year,
                    CreatedAt = DateTime.Now
                };

                await _context.AcademicYears.AddAsync(academicYear);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Thêm năm học thành công",
                    data = new DetailResponse
                    {
                        id = academicYear.AcademicYearId,
                    }
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

        public async Task<BaseResponseId> UpdateAcademicYearAsync(AcademicYearResponse year)
        {
            try
            {
                if (year.start_year >= year.end_year ||
                    year.end_year != year.start_year + 1 ||
                    !Regex.IsMatch(year.start_year.ToString(), YearPattern))
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Năm học không hợp lệ",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "start_year",
                                message = $"Năm học không hợp lệ '{year.start_year}-{year.end_year}'"
                            }
                        }
                    };
                }

                var yearName = $"{year.start_year}-{year.end_year}";

                if (year.name != yearName)
                    year.name = yearName;

                var existYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.AcademicYearId == year.id);

                if (existYear == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy năm học",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "Không tìm thấy năm học"
                            }
                        }
                    };
                }

                if (existYear.YearName != year.name)
                {
                    var yearByNameExists = await _context.AcademicYears.AnyAsync(y => y.YearName == year.name);

                    if (yearByNameExists)
                    {
                        return new BaseResponseId
                        {
                            status = 409,
                            message = "Năm học đã tồn tại",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    message = $"Năm học đã tồn tại '{year.start_year}-{year.end_year}'"
                                }
                            }
                        };
                    }
                }

                existYear.StartYear = year.start_year;
                existYear.EndYear = year.end_year;
                existYear.YearName = year.name;
                existYear.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Cập nhật năm học thành công",
                    data = new DetailResponse
                    {
                        id = existYear.AcademicYearId
                    }
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

        public async Task<BaseResponseId> DeleteAcademicYearAsync(int yearId)
        {
            var existYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.AcademicYearId == yearId);

            if (existYear == null)
            {
                return new BaseResponseId
                {
                    status = 404,
                    message = "Không tìm thấy năm học",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = "Không timg thấy năm học"
                        }
                    }
                };
            }

            _context.AcademicYears.Remove(existYear);
            await _context.SaveChangesAsync();

            return new BaseResponseId
            {
                status = 200,
                message = "Xóa năm học thành công",
                data = new DetailResponse
                {
                    id = existYear.AcademicYearId
                }
            };
        }
    }
}