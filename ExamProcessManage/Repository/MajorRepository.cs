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
            var listMajors = new List<MajorResponse>();
            var baseQuery = _context.Majors.AsQueryable();

            if (departmentId > 0)
            {
                baseQuery = baseQuery.Where(m => m.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(queryObject.search))
            {
                baseQuery = baseQuery.Where(m => m.MajorName.Contains(queryObject.search));
            }

            if (!string.IsNullOrEmpty(queryObject.sort))
            {
                baseQuery = queryObject.sort.ToLower() switch
                {
                    "name" => baseQuery.OrderBy(m => m.MajorName),
                    "name_desc" => baseQuery.OrderByDescending(m => m.MajorName),
                    _ => baseQuery.OrderBy(m => m.MajorId)
                };
            }

            var departmentList = await _context.Departments.ToListAsync();
            var totalCount = await baseQuery.CountAsync();
            var majorList = await baseQuery
                .Skip((queryObject.page.Value - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            foreach (var item in majorList)
            {
                var departmentMajor = departmentList.FirstOrDefault(d => d.DepartmentId == item.DepartmentId);
                listMajors.Add(new MajorResponse
                {
                    id = item.MajorId,
                    name = item.MajorName ?? string.Empty,
                    department = new CommonObject
                    {
                        id = departmentMajor?.DepartmentId ?? (int)item.DepartmentId,
                        code = departmentMajor.DepartmentId.ToString(),
                        name = departmentMajor.DepartmentName
                    }
                });
            }

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
            var response = new BaseResponse<MajorResponse>();
            var major = await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == majorId);

            if (major != null)
            {
                var department = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == major.DepartmentId);

                response.message = "success";
                response.data = new MajorResponse
                {
                    id = major.MajorId,
                    name = major.MajorName,
                    department = new CommonObject
                    {
                        id = department.DepartmentId,
                        code = department.DepartmentId.ToString(),
                        name = department.DepartmentName
                    }
                };
            }
            else
            {
                response.message = $"major with id = '{majorId}' could not be found";
            }

            return response;
        }

        public async Task<BaseResponse<MajorResponse>> CreateMajorAsync(MajorResponse inputMajor)
        {
            try
            {
                var response = new BaseResponse<MajorResponse>();

                var existMajor = await _context.Majors.AnyAsync(m => m.MajorId == inputMajor.id || m.MajorName == inputMajor.name);

                if (!existMajor)
                {
                    var newMajor = new Major
                    {
                        MajorId = inputMajor.id,
                        MajorName = inputMajor.name,
                        DepartmentId = inputMajor.department.id
                    };

                    await _context.Majors.AddAsync(newMajor);
                    await _context.SaveChangesAsync();

                    response.data = inputMajor;
                    response.message = "major added successfully";
                }
                else
                {
                    response.message = $"major already exists";
                }

                return response;
            }
            catch (Exception ex)
            {
                return new BaseResponse<MajorResponse>
                {
                    message = "an error occurred: " + ex.Message
                };
            }
        }

        public async Task<BaseResponse<MajorResponse>> UpdateMajorAsync(MajorResponse updateMajor)
        {
            try
            {
                var response = new BaseResponse<MajorResponse>();
                var existMajor = await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == updateMajor.id);

                if (existMajor != null)
                {
                    if (existMajor.MajorName != updateMajor.name && existMajor.DepartmentId != updateMajor.department.id)
                    {
                        var checkConflictMajor = await _context.Majors.AnyAsync(m => m.MajorName == updateMajor.name);

                        if (!checkConflictMajor)
                        {
                            existMajor.MajorName = updateMajor.name;
                            existMajor.DepartmentId = updateMajor.department.id > 0 ? updateMajor.department.id : existMajor.DepartmentId;

                            await _context.SaveChangesAsync();

                            response.message = "update successfully";
                            response.data = updateMajor;
                        }
                        else
                        {
                            response.message = $"major name = '{updateMajor.name}' already exists";
                        }
                    }
                    else
                    {
                        response.message = "no changes detected";
                    }
                }
                else
                {
                    response.message = $"no major found with id = '{updateMajor.id}'";
                }

                return response;
            }
            catch (Exception ex)
            {
                return new BaseResponse<MajorResponse>
                {
                    message = "an error occurred: " + ex.Message
                };
            }
        }

        public async Task<BaseResponse<MajorResponse>> DeleteMajorAsync(int majorId)
        {
            try
            {
                var response = new BaseResponse<MajorResponse>();
                var existMajor = await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == majorId);

                if (existMajor != null)
                {
                    _context.Majors.Remove(existMajor);
                    await _context.SaveChangesAsync();

                    response.message = "delete successfully";
                    response.data = new MajorResponse
                    {
                        id = existMajor.MajorId,
                        name = existMajor.MajorName,
                        department = new CommonObject { id = (int)existMajor.DepartmentId }
                    };
                }
                else
                {
                    response.message = $"no major found with ID = '{majorId}'";
                }

                return response;
            }
            catch (Exception ex)
            {
                return new BaseResponse<MajorResponse>
                {
                    message = "an error occurred: " + ex.Message
                };
            }
        }
    }
}
