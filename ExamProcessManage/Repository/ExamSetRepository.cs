using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class ExamSetRepository : IExamSetRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly List<string> validStatus = new() { "in_progress", "rejected", "approved", "pending_approval" };

        public ExamSetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ExamSetDTO>> GetListExamSetAsync(int? userId, RequestParamsExamSets queryObject)
        {
            try
            {
                var startRow = (queryObject.page.Value - 1) * queryObject.size;

                // Build base query for ExamSets
                var examSetQuery = _context.ExamSets.AsNoTracking().AsQueryable();
                var examQuery = _context.Exams.AsNoTracking().AsQueryable();

                if (queryObject.exceptValues != null && queryObject.exceptValues.Any())
                    examSetQuery = examSetQuery.Where(p => !queryObject.exceptValues.Contains(p.ExamSetId));

                if (!string.IsNullOrEmpty(queryObject.search))
                    examSetQuery = examSetQuery.Where(p => p.ExamSetName.Contains(queryObject.search));

                if (!string.IsNullOrEmpty(queryObject.stateExamSet))
                    examSetQuery = examSetQuery.Where(e => e.Status == queryObject.stateExamSet);

                if (queryObject.courseId > 0)
                    examSetQuery = examSetQuery.Where(e => e.CourseId == queryObject.courseId);

                if (userId.HasValue)
                    examSetQuery = examSetQuery.Where(q => q.CreatorId == userId);

                if (queryObject.userId.HasValue && !userId.HasValue)
                {
                    var proposalIds = await _context.TeacherProposals
                    .Where(tp => tp.UserId == (ulong)queryObject.userId.Value)
                    .Select(tp => tp.ProposalId)
                    .ToListAsync();

                    if (proposalIds.Any())
                        examSetQuery = examSetQuery.Where(p => p.ProposalId.HasValue && proposalIds.Contains(p.ProposalId.Value));
                }

                if ((bool)queryObject.isParamAddProposal)
                {
                    examSetQuery = examSetQuery.Where(e => e.ProposalId == null);

                    // Filter exams by ExamSetId
                    var examSetIds = await examSetQuery.Select(e => e.ExamSetId).ToListAsync();

                    examQuery = examQuery.Where(p => p.ExamSetId.HasValue && examSetIds.Contains(p.ExamSetId.Value));
                }

                if (queryObject.proposalId.HasValue)
                    examSetQuery = examSetQuery.Where(p => p.ProposalId == queryObject.proposalId);

                // Count total elements before pagination
                var totalCount = await examSetQuery.CountAsync();

                // Fetch paginated data
                var examSets = await examSetQuery
                .OrderBy(p => p.ExamSetId)
                .Skip(startRow)
                .Take(queryObject.size)
                .Include(p => p.Proposal)
                .ThenInclude(tp => tp.TeacherProposals)
                .ToListAsync();

                // Preload related data for DTO mapping
                var departments = await _context.Departments.AsNoTracking().ToDictionaryAsync(d => d.DepartmentId);
                var teachers = await _context.Teachers.AsNoTracking().ToDictionaryAsync(t => t.Id);
                var courses = await _context.Courses.AsNoTracking().ToDictionaryAsync(c => c.CourseId);
                var majors = await _context.Majors.AsNoTracking().ToDictionaryAsync(m => m.MajorId);
                var users = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);

                // Map to DTOs
                var examSetDTOs = examSets.Select(p => new ExamSetDTO
                {
                    id = p.ExamSetId,
                    name = p.ExamSetName,
                    description = p.Description,
                    status = p.Status,
                    exam_quantity = p.ExamQuantity,
                    create_at = p.CreateAt.ToString(),
                    update_at = p.UpdateAt.ToString(),
                    course = p.CourseId.HasValue && courses.TryGetValue(p.CourseId.Value, out var course) ? new CommonObject
                    {
                        id = course.CourseId,
                        name = course.CourseName,
                        code = course.CourseCode
                    } : null,
                    department = p.DepartmentId.HasValue && departments.TryGetValue(p.DepartmentId.Value, out var department) ? new CommonObject
                    {
                        id = department.DepartmentId,
                        name = department.DepartmentName
                    } : null,
                    proposal = p.ProposalId != null ? new CommonObject
                    {
                        id = (int)p.ProposalId,
                        code = p.Proposal.PlanCode
                    } : null,
                    major = p.MajorId.HasValue && majors.TryGetValue(p.MajorId.Value, out var major) ? new CommonObject
                    {
                        id = (int)p.MajorId.Value,
                        name = major.MajorName
                    } : null,
                    exams = (bool)queryObject.isParamAddProposal ? examQuery.Where(e => e.ExamSetId == p.ExamSetId).Select(e => new ExamDTO
                    {
                        code = e.ExamCode,
                        comment = e.Comment,
                        description = e.Description,
                        attached_file = e.AttachedFile,
                        create_at = e.CreateAt.ToString(),
                        status = e.Status,
                        id = e.ExamId,
                        name = e.ExamName
                    }).ToList() : Enumerable.Empty<ExamDTO>().AsQueryable(),
                    user = p.CreatorId.HasValue && users.TryGetValue((ulong)p.CreatorId.Value, out var user) ? new
                    {
                        id = (int)user.Id,
                        name = user.Email ?? "",
                        fullname = user.TeacherId.HasValue && teachers.TryGetValue(user.TeacherId.Value, out var teacher) ? teacher.Name : ""
                    } : null,
                }).ToList();

                // Build page response
                var pageResponse = new PageResponse<ExamSetDTO>
                {
                    totalElements = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                    size = queryObject.size,
                    page = queryObject.page.Value,
                    content = examSetDTOs.ToArray()
                };

                return pageResponse;
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return null;
            }
        }

        public async Task<BaseResponse<ExamSetDTO>> GetDetailExamSetAsync(int? userId, int id)
        {
            try
            {
                // Pre-fetch related data to avoid multiple database calls
                var courses = await _context.Courses.AsNoTracking().ToDictionaryAsync(c => c.CourseId);
                var departments = await _context.Departments.AsNoTracking().ToDictionaryAsync(d => d.DepartmentId);
                var users = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);
                var majors = await _context.Majors.AsNoTracking().ToDictionaryAsync(m => m.MajorId);
                var proposals = await _context.Proposals.AsNoTracking().ToDictionaryAsync(p => p.ProposalId);
                var teachers = await _context.Teachers.AsNoTracking().ToDictionaryAsync(t => t.Id);

                // Fetch the ExamSet with Proposal and TeacherProposals
                var examSet = await _context.ExamSets
                    .AsNoTracking()
                    .Include(p => p.Proposal)
                    .ThenInclude(p => p.TeacherProposals)
                    .FirstOrDefaultAsync(p => p.ExamSetId == id);

                // Return early if the ExamSet is not found
                if (examSet == null)
                {
                    return new BaseResponse<ExamSetDTO>
                    {
                        message = $"Proposal with id = {id} could not be found",
                        data = null
                    };
                }

                // Fetch associated exams for the ExamSet
                var exams = await _context.Exams
                    .AsNoTracking()
                    .Where(ex => ex.ExamSetId == examSet.ExamSetId)
                    .Select(e => new ExamDTO
                    {
                        academic_year = new CommonObject
                        {
                            id = (int)e.AcademicYearId,
                            name = e.AcademicYear.YearName ?? string.Empty,
                        },
                        attached_file = e.AttachedFile,
                        comment = e.Comment,
                        description = e.Description,
                        code = e.ExamCode,
                        id = e.ExamId,
                        name = e.ExamName,
                        status = e.Status,
                        create_at = e.CreateAt.ToString()
                    })
                    .ToListAsync();

                // Create the DTO mapping using the pre-fetched dictionaries
                var examSetDTO = new ExamSetDTO
                {
                    id = examSet.ExamSetId,
                    name = examSet.ExamSetName,
                    description = examSet.Description,
                    exam_quantity = examSet.ExamQuantity,
                    status = examSet.Status,
                    create_at = examSet.CreateAt.ToString(),
                    update_at = examSet.UpdateAt.ToString(),
                    course = examSet.CourseId.HasValue && courses.TryGetValue(examSet.CourseId.Value, out var course) ? new CommonObject
                    {
                        id = course.CourseId,
                        name = course.CourseName,
                        code = course.CourseCode
                    } : null,
                    proposal = examSet.ProposalId.HasValue ? new CommonObject
                    {
                        id = (int)examSet.ProposalId,
                        name = proposals.TryGetValue((int)examSet.ProposalId, out var proposal) ? proposal.PlanCode : null,
                    } : null,
                    user = examSet.CreatorId.HasValue && users.TryGetValue((ulong)examSet.CreatorId.Value, out var user) ? new
                    {
                        id = (int)user.Id,
                        name = user.Email ?? string.Empty,
                        fullname = user.TeacherId.HasValue && teachers.TryGetValue(user.TeacherId.Value, out var teacher) ? teacher.Name : string.Empty
                    } : null,
                    department = examSet.DepartmentId.HasValue && departments.TryGetValue(examSet.DepartmentId.Value, out var department) ? new CommonObject
                    {
                        id = department.DepartmentId,
                        name = department.DepartmentName
                    } : null,
                    exams = exams,
                    major = examSet.MajorId.HasValue && majors.TryGetValue(examSet.MajorId.Value, out var major) ? new CommonObject
                    {
                        id = major.MajorId,
                        name = major.MajorName
                    } : null
                };

                return new BaseResponse<ExamSetDTO>
                {
                    data = examSetDTO
                };
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return null;
            }
        }

        public async Task<BaseResponseId> CreateExamSetAsync(int userId, ExamSetDTO examSetDTO)
        {
            try
            {
                if (examSetDTO == null)
                    return new BaseResponseId { status = 400, message = "Bộ đề rỗng" };

                var errors = new List<ErrorDetail>();

                // Kiểm tra tên bộ đề
                if (!string.IsNullOrEmpty(examSetDTO.name) && examSetDTO.name != "string")
                {
                    bool isExistingName = await _context.ExamSets.AsNoTracking()
                        .AnyAsync(e => examSetDTO.name == e.ExamSetName);
                    if (isExistingName)
                        errors.Add(new ErrorDetail { field = "name", message = "Tên bộ đề đã tồn tại" });
                }

                // Kiểm tra trạng thái bộ đề
                if (!validStatus.Contains(examSetDTO.status))
                    errors.Add(new ErrorDetail { field = "status", message = "Trạng thái bộ đề không hợp lệ" });

                // Kiểm tra học phần
                var course = await _context.Courses.AsNoTracking()
                    .Include(c => c.Major).ThenInclude(m => m.Department)
                    .FirstOrDefaultAsync(c => c.CourseId == examSetDTO.course.id);

                if (course == null)
                    errors.Add(new ErrorDetail { field = "course", message = "Học phần không hợp lệ" });
                else if (course.Major == null)
                    errors.Add(new ErrorDetail { field = "major", message = "Chuyên ngành không hợp lệ" });
                else if (course.Major.Department == null)
                    errors.Add(new ErrorDetail { field = "department", message = "Khoa không hợp lệ" });

                // Kiểm tra đề xuất
                if (examSetDTO.proposal != null && examSetDTO.proposal.id > 0)
                {
                    bool isExistingProposal = await _context.Proposals.AsNoTracking()
                        .AnyAsync(p => p.ProposalId == examSetDTO.proposal.id || p.PlanCode == examSetDTO.proposal.code);
                    if (!isExistingProposal)
                        errors.Add(new ErrorDetail { field = "exam_set.proposal", message = "Không tìm thấy Đề xuất" });
                }

                // Kiểm tra các bài thi
                var examList = new List<Exam>();
                if (examSetDTO.exams != null && examSetDTO.exams.Any())
                {
                    var examIds = examSetDTO.exams.Select(e => e.id).ToList();
                    var existingExams = await _context.Exams.Where(e => examIds.Contains(e.ExamId)).ToListAsync();
                    var examCodeSet = new HashSet<int>();

                    foreach (var examId in examIds)
                    {
                        if (!examCodeSet.Add((int)examId))
                        {
                            errors.Add(new ErrorDetail
                            {
                                field = $"exam_set.exams.{examId}",
                                message = $"Bài thi bị trùng lặp {examId}"
                            });
                        }
                        else if (!existingExams.Any(e => e.ExamId == examId))
                        {
                            errors.Add(new ErrorDetail
                            {
                                field = $"exam_set.exams.{examId}",
                                message = $"Không tồn tại bài thi {examId}"
                            });
                        }
                        else
                        {
                            var exam = existingExams.First(e => e.ExamId == examId);
                            examList.Add(exam);
                        }
                    }
                }

                // Trả về lỗi nếu có
                if (errors.Any())
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Validation Failed",
                        errors = errors
                    };
                }

                // Tạo bộ đề mới
                var newExamSet = new ExamSet
                {
                    ExamSetName = examSetDTO.name,
                    DepartmentId = examSetDTO?.department?.id,
                    MajorId = examSetDTO?.major?.id,
                    ExamQuantity = (int)examSetDTO.exam_quantity,
                    CreatorId = userId,
                    Description = examSetDTO.description ?? string.Empty,
                    Status = examSetDTO.status,
                    CourseId = course?.CourseId,
                    ProposalId = examSetDTO.proposal?.id > 0 ? examSetDTO.proposal.id : null,
                    CreateAt = DateOnly.FromDateTime(DateTime.Now),
                    Exams = examList
                };

                await _context.ExamSets.AddAsync(newExamSet);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    message = "Thêm bộ đề thành công",
                    data = new DetailResponse { id = newExamSet.ExamSetId }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = "Có lỗi xảy ra: " + ex.Message
                };
            }
        }

        public async Task<BaseResponseId> UpdateExamSetAsync(int userId, ExamSetDTO examSet)
        {
            var response = new BaseResponseId();
            var errorList = new List<ErrorDetail>();
            try
            {
                if (examSet == null) errorList.Add(new() { message = "Null exam set" });
                if (examSet.id <= 0) errorList.Add(new() { field = "exam_set.id", message = "Invalid examset id" });
                if (!validStatus.Contains(examSet.status)) errorList.Add(new() { field = "exam_set.status", message = "Invalid status" });
                //if (examSet.course.id <= 0) errorList.Add(new() { field = "exam_set.course.id", message = "Invalid course id" });
                //if (examSet.exam_quantity <= 0) errorList.Add(new() { field = "exam_set.exam_quantity", message = "Invalid exam quantity" });

                var examDTOs = examSet.exams?.ToList();
                if (examDTOs != null && examDTOs.Any())
                {
                    var examIds = new HashSet<int>();
                    for (int i = 0; i < examDTOs.Count; i++)
                    {
                        var id = examDTOs[i].id;
                        if (id <= 0) errorList.Add(new()
                        {
                            field = $"exam_set.exams.{i}",
                            message = "Invalid exam id"
                        });
                        if (!examIds.Add((int)examDTOs[i].id))
                        {
                            errorList.Add(new()
                            {
                                field = $"exam_set.exams.{i}",
                                message = $"Conflict exam {id}"
                            });
                        }
                    }
                }

                if (errorList.Any()) return new BaseResponseId
                {
                    status = 400,
                    message = "Invalid input",
                    errors = errorList
                };

                var existExamSet = await _context.ExamSets.Include(t => t.Exams).FirstOrDefaultAsync(e => e.ExamSetId == examSet.id);
                if (existExamSet == null)
                    errorList.Add(new() { message = $"Exam set not found {examSet.id}" });
                else
                {
                    if (existExamSet.CreatorId != userId)
                    {
                        return new BaseResponseId
                        {
                            status = 403,
                            message = "Forbiden",
                            errors = new() { new() { message = "You do not have permission to update this exam set" } }
                        };
                    }

                    if (existExamSet.Status == "approved")
                    {
                        return new BaseResponseId
                        {
                            status = 403,
                            message = "Forbiden",
                            errors = new() { new() { message = "This exam set has been approved, unable to update" } }
                        };
                    }

                    if (existExamSet.Status != examSet.status)
                        switch (existExamSet.Status)
                        {
                            case "in_progress":
                                if (examSet.status == "pending_approval")
                                {
                                    existExamSet.Status = examSet.status;
                                    break;
                                }
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Invalid status",
                                    errors = new List<ErrorDetail> { new()
                                {
                                    field = "status",
                                    message = "Can only update from 'in_progress' to 'pending_approval'."
                                } }
                                };

                            case "pending_approval":
                                if (examSet.status == "approved" || examSet.status == "rejected" || examSet.status == "in_progress")
                                {
                                    existExamSet.Status = examSet.status;
                                    break;
                                }
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Invalid status",
                                    errors = new List<ErrorDetail> { new()
                                {
                                    field = "status",
                                    message = "Can only update from 'pending_approval' to 'approved', 'rejected', or 'in_progress'."
                                } }
                                };

                            case "rejected":
                                if (examSet.status == "in_progress" || examSet.status == "pending_approval")
                                {
                                    existExamSet.Status = examSet.status;
                                    break;
                                }
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Invalid status",
                                    errors = new List<ErrorDetail> { new()
                                {
                                    field = "status",
                                    message = "Can only update from 'rejected' to 'in_progress' or 'pending_approval'."
                                } }
                                };

                            default:
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Invalid status",
                                    errors = new List<ErrorDetail> { new() { field = "status", message = "Unexpected status." } }
                                };
                        }

                    if (examSet.department != null && examSet.department.id > 0)
                    {
                        if (!await _context.Departments.AnyAsync(d => d.DepartmentId == examSet.department.id)) errorList.Add(new()
                        {
                            field = "exam_set.department",
                            message = $"Department not found {examSet.department.id}"
                        });
                        else existExamSet.DepartmentId = examSet.department.id;
                    }

                    if (examSet.major != null && examSet.major.id > 0)
                    {
                        if (!await _context.Majors.AnyAsync(m => m.MajorId == examSet.major.id)) errorList.Add(new()
                        {
                            field = "exam_set.major",
                            message = $"Major not found {examSet.major.id}"
                        });
                        else existExamSet.MajorId = examSet.major.id;
                    }

                    if (examSet.proposal != null && examSet.proposal.id > 0)
                    {
                        if (!await _context.Proposals.AnyAsync(p => p.ProposalId == examSet.proposal.id)) errorList.Add(new()
                        {
                            field = "exam_set.proposal",
                            message = $"Proposal not found {examSet.proposal.id}"
                        });
                        else existExamSet.ProposalId = examSet.proposal.id;
                    }

                    if (examSet.course != null && examSet.course.id > 0)
                    {
                        if (!await _context.Courses.AnyAsync(c => c.CourseId == examSet.course.id)) errorList.Add(new()
                        {
                            field = "exam_set.course",
                            message = $"Course not found {examSet.course.id}"
                        });
                        else existExamSet.CourseId = examSet.course.id;
                    }

                    existExamSet.Status = examSet.status;
                    existExamSet.ExamQuantity = (int)examSet.exam_quantity;
                    existExamSet.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
                    existExamSet.ExamSetName = examSet.name == "string" || string.IsNullOrEmpty(examSet.name)
                        ? existExamSet.ExamSetName : examSet.name;
                    existExamSet.Description = examSet.description == "string" || string.IsNullOrEmpty(examSet.description)
                        ? existExamSet.Description : examSet.description;

                    var examList = new List<Exam>();
                    if (examSet.exams != null && examSet.exams.Any())
                    {
                        var examComments = examSet.exams.ToDictionary(e => e.id, e => e.comment);
                        var examsListId = examComments.Keys.ToHashSet();
                        var existingExams = await _context.Exams.Where(e => examsListId.Contains(e.ExamId)).ToListAsync();
                        var examCodeSet = new HashSet<int>();
                        var examsToRemove = existingExams.Where(e => !examsListId.Contains(e.ExamId)).ToList();

                        if (examsToRemove.Any())
                            foreach (var exam in examsToRemove) { exam.ExamSetId = null; }

                        int i = 0;
                        foreach (var examId in examsListId)
                        {
                            if (!examCodeSet.Add((int)examId))
                            {
                                errorList.Add(new()
                                {
                                    field = $"exam_set.exams.{i}",
                                    message = $"Conflict exam {examId}"
                                });
                            }
                            else if (!existingExams.Any(e => e.ExamId == examId))
                            {
                                errorList.Add(new()
                                {
                                    field = $"exam_set.exams.{i}",
                                    message = $"Exam not found {examId}"
                                });
                            }
                            else
                            {
                                var exam = existingExams.First(e => e.ExamId == examId);

                                var comment = examComments[examId];
                                exam.Comment = string.IsNullOrEmpty(comment) || comment == "string" || exam.Comment == comment ? exam.Comment : comment;

                                if (examSet.status != exam.Status && exam.Status != "approved")
                                    switch (examSet.status)
                                    {
                                        case "approved":
                                            if (exam.Status == "pending_approval")
                                                exam.Status = examSet.status;
                                            else
                                                errorList.Add(new() { field = $"exam_set.exams.{i}", message = "Invalid exam status" });
                                            break;

                                        case "rejected":
                                            if (exam.Status == "pending_approval")
                                                exam.Status = examSet.status;
                                            else
                                                errorList.Add(new() { field = $"exam_set.exams.{i}", message = "Invalid exam status" });
                                            break;
                                        default:
                                            exam.Status = examSet.status;
                                            break;
                                    }
                                examList.Add(exam);
                            }
                            i++;
                        }
                    }

                    if (errorList.Any()) return new BaseResponseId
                    {
                        status = 400,
                        message = "Update failed",
                        errors = errorList
                    };

                    if (examDTOs != null && examList.Count == examDTOs.Count) existExamSet.Exams = examList;

                    await _context.SaveChangesAsync();

                    response.message = "Update successfully";
                    response.data = new DetailResponse { id = existExamSet.ExamSetId };
                }
            }
            catch (Exception ex)
            {
                errorList.Add(new()
                {
                    message = $"An error occurred:  {ex.Message} {ex.InnerException}"
                });
                response.errors = errorList;
            }

            return response;
        }

        public async Task<BaseResponseId> DeleteExamSetAsync(int userId, int examSetId, bool examSetOnly)
        {
            try
            {
                var findExamSet = await _context.ExamSets
                    .Include(es => es.Exams)
                    .FirstOrDefaultAsync(es => es.ExamSetId == examSetId);

                if (findExamSet == null)
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Not found",
                        errors = new() { new() { message = $"Exam set not found {examSetId}" } }
                    };

                if (userId != findExamSet.CreatorId)
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Forbiden",
                        errors = new() { new() { message = "You do not have the right to delete this exam set." } }
                    };

                if (findExamSet.Status == "approved")
                {
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Forbiden",
                        errors = new() { new() { message = "he exam set has been approved and cannot be deleted." } }
                    };
                }

                var exams = findExamSet.Exams;

                if (exams.Any())
                {
                    if (examSetOnly)
                    {
                        var examIds = exams.Select(e => e.ExamId).ToList();
                        await _context.Exams
                            .Where(e => examIds.Contains(e.ExamId))
                            .ForEachAsync(e => e.ExamSetId = null);
                    }
                    else
                    {
                        var approvedExams = exams.Where(e => e.Status == "approved").ToList();
                        if (approvedExams.Any())
                            return new BaseResponseId
                            {
                                status = 405,
                                message = "Forbiden",
                                errors = new() { new() { message = "The exam has been approved and cannot be deleted." } }
                            };

                        var nonCreatorExams = exams.Where(e => e.CreatorId != userId).ToList();
                        if (nonCreatorExams.Any())
                            return new BaseResponseId
                            {
                                status = 405,
                                message = "Forbiden",
                                errors = new() { new() { message = "You do not have the right to delete this exam." } }
                            };

                        _context.Exams.RemoveRange(exams);
                    }
                }

                _context.ExamSets.Remove(findExamSet);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = examSetOnly ? "Delete exam set successfully." : "Delete examset and exams successfully.",
                    data = new() { id = findExamSet.ExamSetId }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = $"An error occurred: {ex.Message}",
                    errors = new() { new() { message = ex.InnerException.ToString() } }
                };
            }
        }
    }
}
