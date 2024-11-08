using ExamProcessManage.Data;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class MajorRepository : IMajorRepository
    {
        private readonly ApplicationDbContext _context;

        public MajorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<MajorResponse>> GetListMajorAsync(int departmentId, QueryObject queryObject)
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

            var listMajors = new List<MajorResponse>();
            var baseQuery = _context.Majors.AsQueryable();

            // Filter by DepartmentId if applicable
            if (departmentId > 0)
            {
                baseQuery = baseQuery.Where(m => m.DepartmentId == departmentId);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(queryObject.search))
            {
                baseQuery = baseQuery.Where(m => m.MajorName!.Contains(queryObject.search));
            }

            // Apply sorting if specified
            if (!string.IsNullOrEmpty(queryObject.sort))
            {
                baseQuery = queryObject.sort.ToLower() switch
                {
                    "name" => baseQuery.OrderBy(m => m.MajorName),
                    "name_desc" => baseQuery.OrderByDescending(m => m.MajorName),
                    _ => baseQuery.OrderByDescending(m => m.MajorId)
                };
            }

            // Get total count of majors
            var totalCount = await baseQuery.CountAsync();

            // If no records found, return empty content
            if (totalCount == 0)
            {
                return new PageResponse<MajorResponse>
                {
                    content = listMajors, // Empty array
                    totalElements = totalCount,
                    totalPages = 0, // No pages available
                    size = queryObject.size,
                    page = queryObject.page!.Value,
                    numberOfElements = listMajors.Count
                };
            }

            // Get the list of majors with pagination
            var majorList = await baseQuery
                .Skip((queryObject.page!.Value - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            // Get the department list to map departments to majors
            var departmentList = await _context.Departments.ToListAsync();

            // Create response objects for each major
            listMajors.AddRange(from item in majorList
                let departmentMajor =
                    departmentList.FirstOrDefault(d => d.DepartmentId == item.DepartmentId)
                select new MajorResponse
                {
                    id = item.MajorId,
                    name = item.MajorName,
                    created_at = item.CreatedAt.ToString(),
                    updated_at = item.UpdatedAt.ToString(),
                    department = new CommonObject
                    {
                        id = departmentMajor?.DepartmentId ?? (int)item.DepartmentId!,
                        code = departmentMajor?.DepartmentId.ToString() ?? string.Empty,
                        name = departmentMajor?.DepartmentName ?? string.Empty
                    }
                });

            // Return paginated response
            return new PageResponse<MajorResponse>
            {
                content = listMajors,
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                page = queryObject.page.Value,
                numberOfElements = listMajors.Count
            };
        }

        public async Task<BaseResponse<MajorResponse>> GetDetailMajorAsync(int majorId)
        {
            var major = await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == majorId);

            if (major == null)
                return new BaseResponse<MajorResponse>
                {
                    status = 404,
                    message = "Không tìm thấy chuyên ngành",
                    errors = new List<ErrorDetail>()
                    {
                        new()
                        {
                            message = "Không tìm thấy chuyên ngành"
                        }
                    }
                };

            var department =
                await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == major.DepartmentId);

            return new BaseResponse<MajorResponse>
            {
                status = 200,
                message = "Thành công",
                data = new MajorResponse
                {
                    id = major.MajorId,
                    name = major.MajorName,
                    created_at = major.CreatedAt.ToString(),
                    updated_at = major.UpdatedAt.ToString(),
                    department = new CommonObject
                    {
                        id = department!.DepartmentId,
                        code = department.DepartmentId.ToString(),
                        name = department.DepartmentName
                    }
                }
            };
        }

        public async Task<BaseResponseId> CreateMajorAsync(MajorResponse inputMajor)
        {
            try
            {
                if (string.IsNullOrEmpty(inputMajor.name) || inputMajor.name == "string")
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Tên chuyên ngành không hợp lệ",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "name",
                                message = "Tên chuyên ngành không hợp lệ"
                            }
                        }
                    };
                }

                var existMajor = await _context.Majors.AnyAsync(m => m.MajorName == inputMajor.name);

                if (existMajor)
                {
                    return new BaseResponseId
                    {
                        status = 409,
                        message = "Tên chuyên ngành bị trùng",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "name",
                                message = "Tên chuyên ngành bị trùng"
                            }
                        }
                    };
                }

                var newMajor = new Major
                {
                    MajorName = inputMajor.name,
                    CreatedAt = DateTime.Now,
                    DepartmentId = inputMajor.department.id
                };

                await _context.Majors.AddAsync(newMajor);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Thêm chuyên ngành thành công",
                    data = new DetailResponse
                    {
                        id = newMajor.MajorId
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

        public async Task<BaseResponseId> UpdateMajorAsync(MajorResponse updateMajor)
        {
            try
            {
                if (string.IsNullOrEmpty(updateMajor.name) || updateMajor.name == "string")
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Tên chuyên ngành không hợp lệ",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "name",
                                message = "Tên chuyên ngành không hợp lệ"
                            }
                        }
                    };
                }

                var existMajor = await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == updateMajor.id);

                if (existMajor == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy chuyêng ngành",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "id",
                                message = "Không tìm thấy chuyêng ngành"
                            }
                        }
                    };
                }

                if (updateMajor.name != existMajor.MajorName)
                {
                    var checkConflictMajor = await _context.Majors.AnyAsync(m => m.MajorName == updateMajor.name);

                    if (checkConflictMajor)
                    {
                        return new BaseResponseId
                        {
                            status = 409,
                            message = "Tên chuyên ngành bị trùng",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    field = "name",
                                    message = "Tên chuyên ngành bị trùng"
                                }
                            }
                        };
                    }
                }

                existMajor.MajorName = updateMajor.name;
                existMajor.DepartmentId =
                    updateMajor.department.id > 0 ? updateMajor.department.id : existMajor.DepartmentId;
                existMajor.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Cập nhật chuyên ngành thành công",
                    data = new DetailResponse
                    {
                        id = existMajor.MajorId
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

        public async Task<BaseResponseId> DeleteMajorAsync(int majorId)
        {
            try
            {
                var existMajor = await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == majorId);

                if (existMajor == null)
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy chuyên ngành",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "id",
                                message = "Không tìm thấy chuyên ngành"
                            }
                        }
                    };

                _context.Majors.Remove(existMajor);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Xóa chuyên ngành thành công",
                    data = new DetailResponse
                    {
                        id = existMajor.MajorId
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
    }
}