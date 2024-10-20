using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class ExamRepository : IExamRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly List<string> validStatus = new() { "in_progress", "rejected", "approved", "pending_approval" };
        public ExamRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ExamDTO>> GetListExamsAsync(ExamRequestParams query, int? userId)
        {
            var startRow = (query.page.Value - 1) * query.size;
            var baseQuery = _context.Exams.AsNoTracking().AsQueryable();
            var users = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);
            var teachers = await _context.Teachers.AsNoTracking().ToDictionaryAsync(t => t.Id);
            if (query.exceptValues != null && query.exceptValues.Any())
            {
                baseQuery = baseQuery.Where(p => !query.exceptValues.Contains(p.ExamId));
            }
            // Apply search filter
            if (!string.IsNullOrEmpty(query.search))
            {
                baseQuery = baseQuery.Where(e => e.ExamCode.Contains(query.search) || e.ExamName.Contains(query.search));
            }
            if ((bool)query.isGetForAddExamSet)
            {
                baseQuery = baseQuery.Where(e => e.ExamSetId == null);
            }
            // Apply filters based on query parameters
            if (query.exam_set_id != null)
            {
                baseQuery = baseQuery.Where(p => p.ExamSetId == query.exam_set_id);
            }

            if (!string.IsNullOrEmpty(query.status))
            {
                baseQuery = baseQuery.Where(e => e.Status == query.status);
            }

            if (query.academic_year_id > 0)
            {
                baseQuery = baseQuery.Where(e => e.AcademicYearId == query.academic_year_id);
            }

            if (query.month_upload > 0)
            {
                baseQuery = baseQuery.Where(e => e.CreateAt.Value.Month == query.month_upload);
            }

            // Apply userId filter if provided
            if (userId.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.CreatorId == userId.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(query.sort))
            {
                baseQuery = query.sort.ToLower() switch
                {
                    "code" => baseQuery.OrderBy(e => e.ExamCode),
                    "code_desc" => baseQuery.OrderByDescending(e => e.ExamCode),
                    "name" => baseQuery.OrderBy(e => e.ExamName),
                    "name_desc" => baseQuery.OrderByDescending(e => e.ExamName),
                    "create_at" => baseQuery.OrderBy(e => e.CreateAt),
                    "create_at_desc" => baseQuery.OrderByDescending(e => e.CreateAt),
                    "status" => baseQuery.OrderBy(e => e.Status),
                    "status_desc" => baseQuery.OrderByDescending(e => e.Status),
                    _ => baseQuery.OrderBy(e => e.ExamId)
                };
            }

            // Total number of records after filtering
            var totalCount = await baseQuery.CountAsync();

            // Fetch distinct AcademicYearIds
            var academicYearIds = await baseQuery.Select(p => p.AcademicYearId).Distinct().ToListAsync();
            var academicYears = await _context.AcademicYears
            .Where(a => academicYearIds.Contains(a.AcademicYearId))
            .ToDictionaryAsync(a => a.AcademicYearId, a => a.YearName);

            // Fetch paginated exam list
            var exams = await baseQuery
                .OrderBy(p => p.ExamId)
                .Skip(startRow)
                .Take(query.size)
                .Select(p => new ExamDTO
                {
                    comment = p.Comment,
                    attached_file = p.AttachedFile,
                    description = p.Description,
                    code = p.ExamCode,
                    id = p.ExamId,
                    name = p.ExamName,
                    status = p.Status,
                    exam_set = p.ExamSetId != null ? new CommonObject
                    {
                        id = (int)p.ExamSetId,
                        name = p.ExamSet.ExamSetName
                    } : null,
                    user = p.CreatorId.HasValue && users.ContainsKey((ulong)p.CreatorId.Value) ? new
                    {
                        id = (int)users[(ulong)p.CreatorId.Value].Id,
                        name = users[(ulong)p.CreatorId.Value].Email ?? "",
                        fullname = users[(ulong)p.CreatorId.Value].TeacherId.HasValue && teachers.ContainsKey(users[(ulong)p.CreatorId.Value].TeacherId.Value) ? teachers[users[(ulong)p.CreatorId.Value].TeacherId.Value].Name : ""
                    } : null,
                    create_at = p.CreateAt.ToString(),
                    academic_year = p.AcademicYearId.HasValue && academicYears.ContainsKey(p.AcademicYearId.Value) ? new CommonObject
                    {
                        id = p.AcademicYearId.Value,
                        name = academicYears[p.AcademicYearId.Value]
                    } : null
                }).ToListAsync();

            // Return paginated result
            return new PageResponse<ExamDTO>
            {
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / query.size),
                size = query.size,
                page = query.page.Value,
                content = exams,
            };
        }

        public async Task<BaseResponse<ExamDTO>> GetDetailExamAsync(int examId)
        {
            try
            {
                var users = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);
                var teachers = await _context.Teachers.AsNoTracking().ToDictionaryAsync(t => t.Id);
                var examSets = await _context.ExamSets.AsNoTracking().ToDictionaryAsync(t => t.ExamSetId);
                var exam = await _context.Exams.FindAsync(examId);

                if (exam == null)
                {
                    return new BaseResponse<ExamDTO>
                    {
                        status = 404,
                        message = "Not found",
                        errors = new() { new() { message = $"Exam not found {examId}" } }
                    };
                }

                // Truy vấn năm học và chuyển đổi thành từ điển để tra cứu nhanh
                var academicYears = await _context.AcademicYears.AsNoTracking()
                    .ToDictionaryAsync(a => a.AcademicYearId, a => a.YearName);

                // Tạo DTO cho bài thi
                var examDto = new ExamDTO
                {
                    comment = exam.Comment,
                    attached_file = exam.AttachedFile,
                    description = exam.Description,
                    code = exam.ExamCode,
                    id = exam.ExamId,
                    name = exam.ExamName,
                    exam_set = exam.ExamSetId != null ? new CommonObject
                    {
                        id = (int)exam.ExamSetId,
                        name = examSets.TryGetValue((int)exam.ExamSetId, out var exam_set) ? exam_set.ExamSetName : null,
                    } : null,
                    user = exam.CreatorId.HasValue && users.TryGetValue((ulong)exam.CreatorId.Value, out var user) ? new
                    {
                        id = (int)user.Id,
                        name = user.Email ?? "",
                        fullname = user.TeacherId.HasValue && teachers.TryGetValue(user.TeacherId.Value, out var teacher) ? teacher.Name : ""
                    } : null,
                    status = exam.Status,
                    create_at = exam.CreateAt.ToString(),
                    academic_year = academicYears.TryGetValue((int)exam.AcademicYearId, out var yearName)
                        ? new CommonObject
                        {
                            id = exam.AcademicYearId.Value,
                            name = yearName
                        }
                        : null
                };

                return new BaseResponse<ExamDTO>
                {
                    message = "Success",
                    data = examDto
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ExamDTO>
                {
                    status = 500,
                    message = "An error occured: " + ex.Message,
                    errors = new() { new() { message = ex.InnerException.ToString() } }
                };
            }
        }

        public async Task<BaseResponse<List<DetailResponse>>> CreateExamsAsync(List<ExamDTO> exams, int userId)
        {
            try
            {
                if (exams == null || !exams.Any())
                {
                    return new()
                    {
                        status = 400,
                        message = "Invalid input",
                        errors = new() { new() { message = "Null input" } }
                    };
                }

                var listExam = new List<Exam>();
                var errors = new List<ErrorDetail>();

                // Fetch existing codes, names, and attached files in one query each
                var examCodes = exams.Select(x => x.code).ToList();
                var existingCodes = await _context.Exams.AsNoTracking()
                                          .Where(e => examCodes.Contains(e.ExamCode))
                                          .Select(e => e.ExamCode)
                                          .ToListAsync();

                var existingNames = await _context.Exams.AsNoTracking()
                                          .Where(e => exams.Select(x => x.name).Contains(e.ExamName))
                                          .Select(e => e.ExamName)
                                          .ToListAsync();

                var existingFiles = await _context.Exams.AsNoTracking()
                                          .Where(e => exams.Select(x => x.attached_file).Contains(e.AttachedFile))
                                          .Select(e => e.AttachedFile)
                                          .ToListAsync();

                var examSetIds = await _context.ExamSets.AsNoTracking()
                                          .Select(e => e.ExamSetId)
                                          .ToListAsync();

                var academicYearIds = await _context.AcademicYears.AsNoTracking()
                                              .Select(e => e.AcademicYearId)
                                              .ToListAsync();

                for (int i = 0; i < exams.Count; i++)
                {
                    var examDTO = exams[i];

                    // Validate code
                    if (string.IsNullOrEmpty(examDTO.code) || examDTO.code == "string")
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.code",
                            message = $"Exam with code '{examDTO.code}' invalid."
                        });
                    }
                    else if (existingCodes.Contains(examDTO.code))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.code",
                            message = $"Exam with code '{examDTO.code}' already exists."
                        });
                    }

                    // Validate name
                    if (string.IsNullOrEmpty(examDTO.name) || examDTO.name == "string")
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.name",
                            message = $"Exam with name '{examDTO.name}' invalid."
                        });
                    }
                    else if (existingNames.Contains(examDTO.name))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.name",
                            message = $"Exam with name '{examDTO.name}' already exists."
                        });
                    }

                    // Validate attached file
                    if (string.IsNullOrEmpty(examDTO.attached_file) || examDTO.attached_file == "string")
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.attached_file",
                            message = $"Exam with attached_file '{examDTO.attached_file}' invalid."
                        });
                    }
                    else if (existingFiles.Contains(examDTO.attached_file))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.attached_file",
                            message = $"Exam with file '{examDTO.attached_file}' already exists."
                        });
                    }

                    // Validate status
                    if (string.IsNullOrEmpty(examDTO.status) || examDTO.status == "string" || !validStatus.Contains(examDTO.status))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.status",
                            message = $"Exam with status '{examDTO.status}' is invalid."
                        });
                    }

                    // Validate exam set
                    if (examDTO.exam_set != null && examDTO.exam_set.id > 0 && !examSetIds.Contains(examDTO.exam_set.id))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.exam_set.id",
                            message = $"ExamSet with id '{examDTO.exam_set.id}' does not exist."
                        });
                    }

                    // Validate academic year
                    if (!academicYearIds.Contains(examDTO.academic_year.id))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.academic_year.id",
                            message = $"AcademicYear with id '{examDTO.academic_year.id}' does not exist."
                        });
                    }

                    // If no errors, add exam to the list
                    if (!errors.Any())
                    {
                        listExam.Add(new Exam
                        {
                            ExamCode = examDTO.code,
                            ExamName = examDTO.name,
                            ExamSetId = examDTO.exam_set?.id > 0 ? examDTO.exam_set?.id : null,
                            AcademicYearId = examDTO.academic_year?.id,
                            AttachedFile = examDTO.attached_file,
                            Description = examDTO.description == "string" ? string.Empty : examDTO.description,
                            CreateAt = DateOnly.FromDateTime(DateTime.Now),
                            Status = examDTO.status,
                            CreatorId = userId
                        });
                    }
                }

                // If no exams were successfully added, return the errors
                if (listExam.Count == 0 && errors.Any())
                {
                    return new BaseResponse<List<DetailResponse>>
                    {
                        status = 500,
                        message = "Add exam failed",
                        errors = errors
                    };
                }

                // Save valid exams to the database
                await _context.AddRangeAsync(listExam);
                await _context.SaveChangesAsync();

                return new BaseResponse<List<DetailResponse>>
                {
                    message = "Add exam successfully",
                    data = listExam.Select(e => new DetailResponse { id = e.ExamId }).ToList()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<DetailResponse>>
                {
                    status = 500,
                    message = "An error occurred: " + ex.Message,
                    errors = new() { new() { message = ex.InnerException.ToString() } }
                };
            }
        }

        public async Task<BaseResponseId> UpdateExamAsync(int userId, bool isAdmin, ExamDTO examDTO)
        {
            try
            {
                var existExam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examDTO.id || e.ExamCode == examDTO.code);

                if (existExam == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Not Found",
                        errors = new() { new() { field = "id", message = $"Exam not found {examDTO.id}" } }
                    };
                }

                if (existExam.CreatorId != userId && isAdmin == false)
                {
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Method Not Allowed",
                        errors = new() { new() { message = "You do not have permission to update this exam." } }
                    };
                }

                if (existExam.Status == "approved")
                {
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Method Not Allowed",
                        errors = new() { new() { message = "Exam has been approved and cannot updated." } }
                    };
                }

                if (!validStatus.Contains(examDTO.status))
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Bad request",
                        errors = new() { new() { field = "status", message = "Invalid status." } }
                    };
                }

                if (isAdmin)
                {
                    if (existExam.Status == "pending_approval" && (examDTO.status == "approved" || examDTO.status == "rejected"))
                    {
                        if (!string.IsNullOrEmpty(examDTO.comment) && examDTO.comment != "string")
                            existExam.Comment = examDTO.comment;
                        else
                            return new BaseResponseId
                            {
                                status = 400,
                                message = "Bad request",
                                errors = new() { new() { field = "comment", message = "Invalid comment." } }
                            };

                        existExam.Status = examDTO.status;
                        existExam.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
                    }
                    else
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Bad request",
                            errors = new() { new() { field = "status", message = "Invalid status." } }
                        };
                    }
                }
                else
                {
                    if (examDTO.academic_year != null && examDTO.academic_year.id <= 0 ||
                        !await _context.AcademicYears.AnyAsync(a => a.AcademicYearId == examDTO.academic_year.id))
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Bad request",
                            errors = new() { new() { field = "academic_year", message = "Invalid academic year." } }
                        };
                    }

                    if (examDTO.exam_set != null && examDTO.exam_set.id < 0)
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Bad request",
                            errors = new() { new() { field = "exam_set", message = "Invalid exam set." } }
                        };
                    }

                    if (examDTO.exam_set != null && examDTO.exam_set.id > 0 && !await _context.ExamSets.AnyAsync(e => e.ExamSetId == examDTO.exam_set.id))
                        return new BaseResponseId
                        {
                            status = 404,
                            message = "Not found",
                            errors = new() { new() { field = "exam_set", message = "Exam set not found." } }
                        };

                    existExam.ExamName = examDTO.name != "string" && examDTO.name != existExam.ExamName
                        ? examDTO.name : existExam.ExamName;
                    existExam.AttachedFile = examDTO.attached_file != "string" && examDTO.attached_file != existExam.AttachedFile
                        ? examDTO.attached_file : existExam.AttachedFile;
                    existExam.Description = examDTO.description != "string" && examDTO.description != existExam.Description
                        ? examDTO.description : existExam.Description;
                    existExam.ExamSetId = examDTO.exam_set?.id == 0 ? existExam.ExamSetId : examDTO.exam_set?.id;
                    existExam.AcademicYearId = examDTO.academic_year?.id;
                    existExam.UpdateAt = DateOnly.FromDateTime(DateTime.Now);

                    if (existExam.Status == "in_progress" && examDTO.status == "pending_approval")
                        existExam.Status = examDTO.status;
                    else if (existExam.Status == "pending_approval" && examDTO.status == "in_progress")
                        existExam.Status = examDTO.status;
                    else if (existExam.Status == "rejected" && examDTO.status == "in_progress")
                    {
                        existExam.Status = examDTO.status;
                        existExam.Comment = string.Empty;
                    }
                    else
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Bad request",
                            errors = new() { new() { field = "status", message = "Invalid status." } }
                        };
                }

                var examsByExamSet = await _context.Exams.Where(e => e.ExamSetId == existExam.ExamSetId).ToListAsync();
                if (examsByExamSet.Any())
                {
                    bool allExamsPendingApproval = examsByExamSet.All(exam => exam.Status == "pending_approval");
                    var examSet = await _context.ExamSets.FindAsync(existExam.ExamSetId);
                    if (examSet != null && examSet.Status != "approved")
                    {
                        var newStatus = allExamsPendingApproval ? "pending_approval" : "in_progress";
                        if (examSet.Status != newStatus)
                        {
                            examSet.Status = newStatus;
                            _context.ExamSets.Update(examSet);
                        }
                    }
                }

                _context.Exams.Update(existExam);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Update successfully.",
                    data = new DetailResponse { id = existExam.ExamId }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = $"An error occurred: {ex.Message}",
                    errors = new() { new() { message = ex.InnerException?.ToString() } }
                };
            }
        }

        public async Task<BaseResponseId> DeleteExamAsync(int userId, int examId)
        {
            try
            {
                var existExam = await _context.Exams.FindAsync(examId);
                if (existExam == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Not found",
                        errors = new List<ErrorDetail> { new() { field = "examId", message = $"Exam not found {examId}" } }
                    };
                }

                if (existExam.CreatorId != userId)
                {
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Method Not Allowed",
                        errors = new List<ErrorDetail> { new() { message = "You do not have the right to delete other instructors' exams." } }
                    };
                }

                if (existExam.Status == "approved")
                {
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Method Not Allowed",
                        errors = new List<ErrorDetail> { new() { message = "Exam has been approved and cannot be deleted." } }
                    };
                }

                _context.Exams.Remove(existExam);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Delete successfully",
                    data = new() { id = existExam.ExamId }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = $"Internal Server Error: {ex.Message} {ex.InnerException}"
                };
            }
        }
    }
}
