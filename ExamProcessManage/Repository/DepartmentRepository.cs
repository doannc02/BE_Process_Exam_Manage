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
            var response = new List<DepartmentResponse>();
            var queryDepartments = _context.Departments.AsQueryable();
            var totalCount = await queryDepartments.CountAsync();
            var listDepartments = await queryDepartments
                .Skip((queryObject.page.Value - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            foreach (var item in listDepartments)
            {
                response.Add(new DepartmentResponse
                {
                    id = item.DepartmentId,
                    name = item.DepartmentName ?? string.Empty
                });
            }

            return new PageResponse<DepartmentResponse>
            {
                content = response,
                page = queryObject.page.Value,
                size = queryObject.size,
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                numberOfElements = response.Count,
                sort = queryObject.sort ?? string.Empty
            };
        }

        public async Task<BaseResponse<DepartmentResponse>> GetDetailDepartmentAsync(int id)
        {
            var response = new BaseResponse<DepartmentResponse>();
            var department = await _context.Departments.FindAsync(id);

            if (department != null)
            {
                response.message = "success";
                response.data = new DepartmentResponse
                {
                    id = department.DepartmentId,
                    name = department.DepartmentName ?? string.Empty
                };
            }
            else
            {
                response.message = $"no department found with ID = '{id}'";
            }

            return response;
        }

        public async Task<BaseResponse<DepartmentResponse>> CreateDepartmentAsync(DepartmentResponse department)
        {
            var response = new BaseResponse<DepartmentResponse>();

            try
            {
                var existed = await _context.Departments.AnyAsync(d => d.DepartmentId == department.id ||
                d.DepartmentName.Contains(department.name));

                if (!existed)
                {
                    var newDepartment = new Department
                    {
                        DepartmentId = department.id,
                        DepartmentName = department.name
                    };

                    await _context.Departments.AddAsync(newDepartment);
                    await _context.SaveChangesAsync();

                    response.message = "add department successfully";
                    response.data = department;
                }
                else
                {
                    response.message = $"conflict data at ID = '{department.id}' or Name = '{department.name}'";
                }
            }
            catch (Exception ex)
            {
                response.message = $"an error occurred: {ex.Message}";
            }

            return response;
        }

        public async Task<BaseResponse<DepartmentResponse>> UpdateDepartmentAsync(DepartmentResponse department)
        {
            var response = new BaseResponse<DepartmentResponse>();

            try
            {
                var existed = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == department.id);

                if (existed != null)
                {
                    var existedName = await _context.Departments.AnyAsync(d => d.DepartmentId == department.id && d.DepartmentName == department.name);

                    if (!existedName)
                    {
                        existed.DepartmentName = department.name;

                        await _context.SaveChangesAsync();

                        response.message = "update successfully";
                        response.data = department;
                    }
                    else
                    {
                        response.message = $"no changes detected";
                    }
                }
                else
                {
                    response.message = $"no department found with ID = '{department.id}'";
                }
            }
            catch (Exception ex)
            {
                response.message = $"an error occurred: {ex.Message}";
            }

            return response;
        }

        public async Task<BaseResponse<DepartmentResponse>> DeleteDepartmentAsync(int id)
        {
            var response = new BaseResponse<DepartmentResponse>();

            var existed = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (existed != null)
            {
                _context.Departments.Remove(existed);
                await _context.SaveChangesAsync();

                response.message = "delete successfully";
                response.data = new DepartmentResponse
                {
                    id = existed.DepartmentId,
                    name = existed.DepartmentName ?? string.Empty
                };
            }
            else
            {
                response.message = $"no department found with ID = '{id}'";
            }

            return response;
        }
    }
}
