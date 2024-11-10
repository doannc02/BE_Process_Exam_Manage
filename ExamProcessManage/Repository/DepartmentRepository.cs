using ExamProcessManage.Data;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly ApplicationDbContext _context;

        public DepartmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<DepartmentResponse>> GetListDepartmentAsync(QueryObject queryObject)
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

            var response = new List<DepartmentResponse>();
            var queryDepartments = _context.Departments.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(queryObject.search))
            {
                queryDepartments = queryDepartments.Where(m => m.DepartmentName!.Contains(queryObject.search));
            }

            // Apply sorting if specified (you can adjust the sort logic as needed)
            if (!string.IsNullOrEmpty(queryObject.sort))
            {
                queryDepartments = queryObject.sort.ToLower() switch
                {
                    "name" => queryDepartments.OrderBy(d => d.DepartmentName),
                    "name_desc" => queryDepartments.OrderByDescending(d => d.DepartmentName),
                    _ => queryDepartments.OrderByDescending(d => d.DepartmentId), // Default sorting
                };
            }

            // Apply pagination
            var totalCount = await queryDepartments.CountAsync();

            // If no records found, return empty content
            if (totalCount == 0)
            {
                return new PageResponse<DepartmentResponse>
                {
                    content = response, // Empty array
                    totalElements = totalCount,
                    totalPages = 0, // No pages available
                    size = queryObject.size,
                    page = queryObject.page,
                    numberOfElements = response.Count,
                    sort = queryObject.sort ?? string.Empty
                };
            }

            var listDepartments = await queryDepartments
                .Skip((queryObject.page - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            response.AddRange(listDepartments.Select(item => new DepartmentResponse
                { id = item.DepartmentId, name = item.DepartmentName ?? string.Empty }));

            return new PageResponse<DepartmentResponse>
            {
                content = response,
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                page = queryObject.page,
                numberOfElements = response.Count,
                sort = queryObject.sort ?? string.Empty
            };
        }

        public async Task<BaseResponse<DepartmentResponse>> GetDetailDepartmentAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department != null)
            {
                return new BaseResponse<DepartmentResponse>
                {
                    status = 200,
                    message = "Thành công",
                    data = new DepartmentResponse
                    {
                        id = department.DepartmentId,
                        name = department.DepartmentName
                    }
                };
            }

            return new BaseResponse<DepartmentResponse>
            {
                status = 404,
                message = "Không tìm thấy khoa",
                errors = new List<ErrorDetail>
                {
                    new()
                    {
                        message = "Không tìm thấy khoa"
                    }
                }
            };
        }

        public async Task<BaseResponseId> CreateDepartmentAsync(DepartmentResponse department)
        {
            try
            {
                if (string.IsNullOrEmpty(department.name))
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Yêu cầu nhập tên khoa",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "name",
                                message = "Yêu cầu nhập tên khoa",
                            }
                        }
                    };
                }

                var existingDepartment = await _context.Departments.AnyAsync(d => d.DepartmentName == department.name);

                if (existingDepartment)
                {
                    return new BaseResponseId
                    {
                        status = 409,
                        message = "Tên đã tồn tại",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "name",
                                message = "Tên đã tồn tại"
                            }
                        }
                    };
                }

                var newDepartment = new Department
                {
                    DepartmentName = department.name,
                };

                await _context.Departments.AddAsync(newDepartment);
                await _context.SaveChangesAsync();

                var response =
                    await _context.Departments.FirstOrDefaultAsync(
                        d => d.DepartmentName == newDepartment.DepartmentName);

                return new BaseResponseId
                {
                    status = 200,
                    message = "Thêm khoa thành công",
                    data = new DetailResponse
                    {
                        id = response!.DepartmentId
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

        public async Task<BaseResponseId> UpdateDepartmentAsync(DepartmentResponse department)
        {
            try
            {
                if (string.IsNullOrEmpty(department.name) || department.name == "string")
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Khoa không hợp lệ",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "name",
                                message = "Tên khoa không hợp lệ"
                            }
                        }
                    };
                }

                var departmentToUpdate =
                    await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == department.id);

                if (departmentToUpdate == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy khoa",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "id",
                                message = "Không tìm thấy khoa"
                            }
                        }
                    };
                }

                if (departmentToUpdate.DepartmentName != department.name)
                {
                    var existingDepartment =
                        await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentName == department.name);

                    if (existingDepartment != null)
                    {
                        return new BaseResponseId
                        {
                            status = 409,
                            message = "Tên khoa bị trùng",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    field = "name",
                                    message = "Tên khoa bị trùng"
                                }
                            }
                        };
                    }
                }

                departmentToUpdate.DepartmentName = department.name;

                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Cập nhật thành công",
                    data = new DetailResponse
                    {
                        id = departmentToUpdate.DepartmentId
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

        public async Task<BaseResponseId> DeleteDepartmentAsync(int id)
        {
            try
            {
                var existingDepartment = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);

                if (existingDepartment == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy khoa",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "id",
                                message = "Không tìm thấy khoa"
                            }
                        }
                    };
                }

                _context.Departments.Remove(existingDepartment);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Xóa khoa thành công",
                    data = new DetailResponse
                    {
                        id = existingDepartment.DepartmentId
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