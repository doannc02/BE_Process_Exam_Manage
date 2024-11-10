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

        private readonly List<string> _validStatus = new()
            { "in_progress", "rejected", "approved", "pending_approval" };

        public ExamSetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ExamSetDTO>> GetListExamSetAsync(int? userId, RequestParamsExamSets queryObject)
        {
            try
            {
                var startRow = (queryObject.page - 1) * queryObject.size;

                // Build base query for ExamSets
                var examSetQuery = _context.ExamSets.AsNoTracking().AsQueryable();
                var examQuery = _context.Exams.AsNoTracking().AsQueryable();

                if (queryObject.exceptValues != null && queryObject.exceptValues.Any())
                    examSetQuery = examSetQuery.Where(p => !queryObject.exceptValues.Contains(p.ExamSetId));

                if (!string.IsNullOrEmpty(queryObject.search))
                    examSetQuery = examSetQuery.Where(p =>
                        p.ExamSetName != null && p.ExamSetName.Contains(queryObject.search));

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
                        examSetQuery = examSetQuery.Where(p =>
                            p.ProposalId.HasValue && proposalIds.Contains(p.ProposalId.Value));
                }

                if (queryObject.isParamAddProposal ?? false)
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
                    .ThenInclude(tp => tp!.TeacherProposals)
                    .ToListAsync();

                // Preload related data for DTO mapping
                var departments = await _context.Departments.AsNoTracking().ToDictionaryAsync(d => d.DepartmentId);
                var teachers = await _context.Teachers.AsNoTracking().Select(t => new { t.Id, t.Name }).ToListAsync();

                // Chuyển danh sách thành từ điển
                var teachersDict = teachers.ToDictionary(t => t.Id, t => t.Name);

                var courses = await _context.Courses.AsNoTracking().ToDictionaryAsync(c => c.CourseId);
                var majors = await _context.Majors.AsNoTracking().ToDictionaryAsync(m => m.MajorId);
                var users = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);

                // Map to DTOs
                var examSetDtOs = examSets.Select(p => new ExamSetDTO
                {
                    id = p.ExamSetId,
                    name = p.ExamSetName,
                    description = p.Description,
                    status = p.Status,
                    exam_quantity = p.ExamQuantity,
                    create_at = p.CreateAt.ToString(),
                    update_at = p.UpdateAt.ToString(),
                    course = p.CourseId.HasValue && courses.TryGetValue(p.CourseId.Value, out var course)
                        ? new CommonObject
                        {
                            id = course.CourseId,
                            name = course.CourseName ?? "unknown",
                            code = course.CourseCode ?? "N?A"
                        }
                        : null,
                    department =
                        p.DepartmentId.HasValue && departments.TryGetValue(p.DepartmentId.Value, out var department)
                            ? new CommonObject
                            {
                                id = department.DepartmentId,
                                name = department.DepartmentName
                            }
                            : null,
                    proposal = p.ProposalId != null
                        ? new CommonObject
                        {
                            id = (int)p.ProposalId,
                            code = p.Proposal?.PlanCode
                        }
                        : null,
                    major = p.MajorId.HasValue && majors.TryGetValue(p.MajorId.Value, out var major)
                        ? new CommonObject
                        {
                            id = p.MajorId.Value,
                            name = major.MajorName
                        }
                        : null,
                    exams = queryObject.isParamAddProposal ?? false
                        ? examQuery.Where(e => e.ExamSetId == p.ExamSetId).Select(e => new ExamDTO
                        {
                            code = e.ExamCode,
                            comment = e.Comment,
                            description = e.Description,
                            attached_file = e.AttachedFile,
                            create_at = e.CreateAt.ToString(),
                            status = e.Status,
                            id = e.ExamId,
                            name = e.ExamName
                        }).ToList()
                        : Enumerable.Empty<ExamDTO>().AsQueryable(),
                    user = p.CreatorId.HasValue && users.TryGetValue((ulong)p.CreatorId.Value, out var user)
                        ? new
                        {
                            id = (int)user.Id,
                            name = user.Email,
                            fullname = user.TeacherId.HasValue &&
                                       teachersDict.TryGetValue(user.TeacherId.Value, out var teacherName)
                                ? teacherName
                                : ""
                        }
                        : null,
                }).ToList();

                // Build page response
                var pageResponse = new PageResponse<ExamSetDTO>
                {
                    totalElements = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                    size = queryObject.size,
                    page = queryObject.page,
                    content = examSetDtOs.ToArray()
                };

                return pageResponse;
            }
            catch (Exception)
            {
                return new PageResponse<ExamSetDTO>
                {
                    totalElements = 0,
                    totalPages = 0,
                    page = 0,
                    size = 0,
                    numberOfElements = 0
                };
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
                    .ThenInclude(p => p!.TeacherProposals)
                    .FirstOrDefaultAsync(p => p.ExamSetId == id);

                // Return early if the ExamSet is not found
                if (examSet == null)
                {
                    return new BaseResponse<ExamSetDTO>
                    {
                        message = $"Không tìm thấy bộ đề",
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
                            id = (int)e.AcademicYearId!,
                            name = e.AcademicYear!.YearName,
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
                var examSetDto = new ExamSetDTO
                {
                    id = examSet.ExamSetId,
                    name = examSet.ExamSetName,
                    description = examSet.Description,
                    exam_quantity = examSet.ExamQuantity,
                    status = examSet.Status,
                    create_at = examSet.CreateAt.ToString(),
                    update_at = examSet.UpdateAt.ToString(),
                    course = examSet.CourseId.HasValue && courses.TryGetValue(examSet.CourseId.Value, out var course)
                        ? new CommonObject
                        {
                            id = course.CourseId,
                            name = course.CourseName,
                            code = course.CourseCode
                        }
                        : null,
                    proposal = examSet.ProposalId.HasValue
                        ? new CommonObject
                        {
                            id = (int)examSet.ProposalId,
                            name = proposals.TryGetValue((int)examSet.ProposalId, out var proposal)
                                ? proposal.PlanCode
                                : null,
                        }
                        : null,
                    user = examSet.CreatorId.HasValue && users.TryGetValue((ulong)examSet.CreatorId.Value, out var user)
                        ? new
                        {
                            id = (int)user.Id,
                            name = user.Email,
                            fullname = user.TeacherId.HasValue &&
                                       teachers.TryGetValue(user.TeacherId.Value, out var teacher)
                                ? teacher.Name
                                : string.Empty
                        }
                        : null,
                    department =
                        examSet.DepartmentId.HasValue &&
                        departments.TryGetValue(examSet.DepartmentId.Value, out var department)
                            ? new CommonObject
                            {
                                id = department.DepartmentId,
                                name = department.DepartmentName
                            }
                            : null,
                    exams = exams,
                    major = examSet.MajorId.HasValue && majors.TryGetValue(examSet.MajorId.Value, out var major)
                        ? new CommonObject
                        {
                            id = major.MajorId,
                            name = major.MajorName
                        }
                        : null
                };

                return new BaseResponse<ExamSetDTO>
                {
                    data = examSetDto
                };
            }
            catch (Exception exception)
            {
                // Log the exception (ex) here if needed
                return new BaseResponse<ExamSetDTO>
                {
                    status = 500,
                    message = exception.Message,
                    errors = new List<ErrorDetail>
                    {
                        new() { message = exception.InnerException?.ToString() ?? exception.Message }
                    }
                };
            }
        }

        public async Task<BaseResponseId> CreateExamSetAsync(int userId, ExamSetDTO examSetDto)
        {
            try
            {
                var errors = new List<ErrorDetail>();

                // Kiểm tra tên bộ đề
                if (!string.IsNullOrEmpty(examSetDto.name) && examSetDto.name != "string")
                {
                    var isExistingName = await _context.ExamSets.AsNoTracking()
                        .AnyAsync(e => examSetDto.name == e.ExamSetName);
                    if (isExistingName)
                        errors.Add(new ErrorDetail { field = "name", message = "Tên bộ đề đã tồn tại" });
                }

                // Kiểm tra trạng thái bộ đề
                if (!_validStatus.Contains(examSetDto.status))
                    errors.Add(new ErrorDetail { field = "status", message = "Trạng thái bộ đề không hợp lệ" });

                // Kiểm tra học phần
                var course = await _context.Courses.AsNoTracking()
                    .Include(c => c.Major).ThenInclude(m => m.Department)
                    .FirstOrDefaultAsync(c => c.CourseId == examSetDto.course.id);

                if (course == null)
                    errors.Add(new ErrorDetail { field = "course", message = "Học phần không hợp lệ" });
                else if (course.Major == null)
                    errors.Add(new ErrorDetail { field = "major", message = "Chuyên ngành không hợp lệ" });
                else if (course.Major.Department == null)
                    errors.Add(new ErrorDetail { field = "department", message = "Khoa không hợp lệ" });

                // Kiểm tra đề xuất
                if (examSetDto.proposal != null && examSetDto.proposal.id > 0)
                {
                    var isExistingProposal = await _context.Proposals.AsNoTracking()
                        .AnyAsync(p =>
                            p.ProposalId == examSetDto.proposal.id || p.PlanCode == examSetDto.proposal.code);
                    if (!isExistingProposal)
                        errors.Add(new ErrorDetail { field = "exam_set.proposal", message = "Không tìm thấy Đề xuất" });
                }

                // Kiểm tra các bài thi
                var examList = new List<Exam>();
                if (examSetDto.exams != null && examSetDto.exams.Any())
                {
                    var examIds = examSetDto.exams.Select(e => e.id).ToList();
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
                        status = 500,
                        message = "Validation Failed",
                        errors = errors
                    };
                }

                // Tạo bộ đề mới
                var newExamSet = new ExamSet
                {
                    ExamSetName = examSetDto.name,
                    DepartmentId = examSetDto.department?.id,
                    MajorId = examSetDto.major?.id,
                    ExamQuantity = (int)examSetDto.exam_quantity,
                    CreatorId = userId,
                    Description = examSetDto.description ?? string.Empty,
                    Status = examSetDto.status,
                    CourseId = course?.CourseId,
                    ProposalId = examSetDto.proposal?.id > 0 ? examSetDto.proposal.id : null,
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

        public async Task<BaseResponseId> UpdateExamSetAsync(int userId, ExamSetDTO examSet, bool isAdmin)
        {
            try
            {
                var errorList = new List<ErrorDetail>();

                if (examSet == null)
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Bộ đề nhập vào rỗng",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "Bộ đề nhập vào rỗng"
                            }
                        }
                    };
                }

                // Check loi dau vao
                if (examSet.id <= 0)
                    errorList.Add(new ErrorDetail { field = "id", message = $"Invalid examset id {examSet.id}" });

                if (!_validStatus.Contains(examSet.status))
                {
                    errorList.Add(new ErrorDetail
                    {
                        field = "status",
                        message = $"Invalid status '{examSet.status}'"
                    });
                }

                if (examSet.course.id < 0)
                    errorList.Add(new ErrorDetail
                        { field = "course.id", message = $"Invalid course id {examSet.course.id}" });

                if (examSet.exam_quantity < 0)
                    errorList.Add(new ErrorDetail
                        { field = "exam_quantity", message = $"Invalid exam quantity {examSet.exam_quantity}" });

                // Kiem tra tinh hop le, trung exam
                var examDtOs = examSet.exams?.ToList();
                if (examDtOs != null && examDtOs.Any())
                {
                    var examIds = new HashSet<int>();
                    for (var i = 0; i < examDtOs.Count; i++)
                    {
                        var id = examDtOs[i].id;
                        if (id <= 0)
                            errorList.Add(new ErrorDetail
                            {
                                field = $"exams.{i}",
                                message = $"Invalid exam id {id}"
                            });
                        if (!examIds.Add((int)examDtOs[i].id!))
                        {
                            errorList.Add(new ErrorDetail
                            {
                                field = $"exams.{i}",
                                message = $"Conflict exam {id}"
                            });
                        }
                    }
                }

                if (errorList.Any())
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Invalid input",
                        errors = errorList
                    };

                // Lay ra exam set da co kem theo exams
                var existExamSet = await _context.ExamSets.Include(t => t.Exams)
                    .FirstOrDefaultAsync(e => e.ExamSetId == examSet.id);
                if (existExamSet == null)
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Not found",
                        errors = new List<ErrorDetail> { new() { message = $"Exam set not found {examSet.id}" } }
                    };

                // Bao loi khi bo de da duoc phe duyet
                if (existExamSet.Status == "approved")
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Forbidden",
                        errors = new List<ErrorDetail>
                            { new() { message = "This exam set has been approved, unable to update" } }
                    };

                // Retrieve the list of exams from the exam set
                var existExams = existExamSet.Exams.ToList();


                // Check if the user is an admin
                if (isAdmin)
                {
                    // Ensure the exam set is pending approval and the new status is valid
                    if (existExamSet.Status != "pending_approval" ||
                        (examSet.status != "approved" && examSet.status != "rejected"))
                    {
                        errorList.Add(new() { field = "status", message = "Invalid status for exam set." });
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Update failed",
                            errors = errorList
                        };
                    }

                    if (examDtOs != null)
                    {
                        var i = 0;
                        foreach (var examDto in examDtOs)
                        {
                            var exam = existExams.FirstOrDefault(e => e.ExamId == examDto.id);

                            if (exam == null)
                            {
                                errorList.Add(new ErrorDetail
                                    { field = $"exam_set.exams.{i}", message = $"Exam not match {examDto.id}" });
                                i++;
                                continue;
                            }

                            switch (exam.Status)
                            {
                                // Exam already approved, cannot update
                                case "approved":
                                    errorList.Add(new ErrorDetail
                                        { message = "The exam has been approved, and cannot be updated." });
                                    i++;
                                    continue;
                                // Check for valid status transitions (only pending exams can be approved/rejected)
                                case "pending_approval" when
                                    examDto.status is "approved" or "rejected":
                                {
                                    // Validate the comment
                                    if (string.IsNullOrEmpty(examDto.comment) || examDto.comment == "string")
                                    {
                                        return new BaseResponseId
                                        {
                                            status = 400,
                                            message = "Bad request",
                                            errors = new List<ErrorDetail> { new() { field = "comment", message = "Invalid comment." } }
                                        };
                                    }

                                    // Update exam details
                                    exam.Comment = examDto.comment;
                                    exam.Status = examDto.status;
                                    exam.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
                                    break;
                                }
                                default:
                                    errorList.Add(new ErrorDetail { field = $"exam_set.exams.{i}", message = "Invalid status for exam." });
                                    break;
                            }

                            i++;
                        }
                    }

                    switch (examSet.status)
                    {
                        case "approved":
                        {
                            // Check if all exams are already approved
                            var allExamsApproved = existExams.All(e => e.Status == "approved");
                            if (allExamsApproved)
                            {
                                // If all exams are approved, set the exam set status to approved
                                existExamSet.Status = "approved";
                            }
                            else
                            {
                                // If not all exams are approved, return an error
                                errorList.Add(new ErrorDetail { field = "status", message = "Not all exams are approved." });
                            }

                            break;
                        }
                        case "rejected":
                            // If the status is rejected, set the exam set status to rejected directly
                            existExamSet.Status = "rejected";
                            break;
                        default:
                            errorList.Add(new ErrorDetail { field = "status", message = "Invalid status transition." });
                            break;
                    }

                    // Update timestamp of the exam set
                    existExamSet.UpdateAt = DateOnly.FromDateTime(DateTime.Now);

                    // Return error response if there are any validation errors
                    if (errorList.Any())
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Update failed",
                            errors = errorList
                        };
                    }
                }

                // Neu user khong phai admin
                else
                {
                    // Kiem tra exam set co phai do nguoi dung dang dang nhap tao khong
                    if (existExamSet.CreatorId != userId)
                    {
                        return new BaseResponseId
                        {
                            status = 405,
                            message = "Forbiden",
                            errors = new List<ErrorDetail> { new() { message = "You do not have the right to update this exam set." } }
                        };
                    }

                    // Trang thai dau vao chi danh cho admin
                    if (examSet.status is "approved" or "rejected")
                        return new BaseResponseId
                        {
                            status = 405,
                            message = "Not allowed",
                            errors = new List<ErrorDetail> { new() { field = "status", message = "Status not allowed for user." } }
                        };

                    // Thay doi khoa
                    if (examSet.department is { id: > 0 })
                    {
                        if (!await _context.Departments.AnyAsync(d => d.DepartmentId == examSet.department.id))
                            errorList.Add(new ErrorDetail
                            {
                                field = "department",
                                message = $"Department not found {examSet.department.id}"
                            });
                        else existExamSet.DepartmentId = examSet.department.id;
                    }

                    // Thay doi chuyen nghanh
                    if (examSet.major is { id: > 0 })
                    {
                        if (!await _context.Majors.AnyAsync(m => m.MajorId == examSet.major.id))
                            errorList.Add(new ErrorDetail
                            {
                                field = "major",
                                message = $"Major not found {examSet.major.id}"
                            });
                        else existExamSet.MajorId = examSet.major.id;
                    }

                    // Thay doi de xuat
                    if (examSet.proposal is { id: > 0 })
                    {
                        if (!await _context.Proposals.AnyAsync(p => p.ProposalId == examSet.proposal.id))
                            errorList.Add(new ErrorDetail
                            {
                                field = "proposal",
                                message = $"Proposal not found {examSet.proposal.id}"
                            });
                        else existExamSet.ProposalId = examSet.proposal.id;
                    }

                    // Thay doi hoc phan
                    if (examSet.course.id > 0)
                    {
                        if (!await _context.Courses.AnyAsync(c => c.CourseId == examSet.course.id))
                            errorList.Add(new ErrorDetail
                            {
                                field = "course",
                                message = $"Course not found {examSet.course.id}"
                            });
                        else existExamSet.CourseId = examSet.course.id;
                    }

                    existExamSet.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
                    existExamSet.ExamSetName = examSet.name == "string" || string.IsNullOrEmpty(examSet.name)
                        ? existExamSet.ExamSetName
                        : examSet.name;
                    existExamSet.Description =
                        examSet.description == "string" || string.IsNullOrEmpty(examSet.description)
                            ? existExamSet.Description
                            : examSet.description;
                    existExamSet.UpdateAt = DateOnly.FromDateTime(DateTime.Now);


                    // Cap nhat exams
                    if (examDtOs != null)
                    {
                        // Lấy danh sách kỳ thi mới dựa trên thông tin từ examDTO
                        var newExams = await _context.Exams
                            .Where(e => examDtOs.Select(dto => dto.id).Contains(e.ExamId))
                            .ToListAsync();

                        // Tạo tập hợp ID của các kỳ thi mới để kiểm tra kỳ thi cũ
                        var newExamIds = new HashSet<int>(newExams.Select(e => e.ExamId));

                        // Xử lý các kỳ thi cũ
                        foreach (var oldExam in existExams.Where(oldExam => !newExamIds.Contains(oldExam.ExamId)))
                        {
                            if (oldExam.Status == "approved")
                            {
                                errorList.Add(new ErrorDetail
                                {
                                    field = "exams", message = $"Can not remove exam is approved {oldExam.ExamId}."
                                });
                            }
                            else
                            {
                                oldExam.Status = oldExam.Status == "pending_approval"
                                    ? "in_progress"
                                    : oldExam.Status;
                                oldExam.ExamSetId = null;
                                oldExam.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
                            }
                        }


                        // Tạo dictionary để nhanh chóng truy cập trạng thái kỳ thi theo id
                        var examStatusDict = examDtOs.ToDictionary(e => e.id, e => e.status);
                        var i = 0;

                        // Cập nhật trạng thái kỳ thi mới dựa trên examDTO
                        foreach (var newExam in newExams)
                        {
                            if ((newExam.ExamSetId == null || newExam.ExamSetId == existExamSet.ExamSetId) &&
                                newExam.CreatorId == userId)
                            {
                                if (examStatusDict.TryGetValue(newExam.ExamId, out var newStatus))
                                {
                                    if (newExam.Status != examDtOs[i].status)

                                        if (newStatus is "approved" or "rejected")
                                            errorList.Add(new ErrorDetail
                                            {
                                                field = $"exam_set.exams.{i}",
                                                message = "Users are not allowed to approve the exam."
                                            });
                                        else switch (newExam.Status)
                                        {
                                            case "in_progress" when
                                                examDtOs[i].status == "pending_approval":
                                                newExam.Status = examDtOs[i].status;
                                                newExam.Comment = string.Empty;
                                                break;
                                            case "pending_approval" when
                                                examDtOs[i].status == "in_progress":
                                            case "rejected" when examDtOs[i].status == "in_progress":
                                                newExam.Status = examDtOs[i].status;
                                                break;
                                            default:
                                                errorList.Add(new ErrorDetail
                                                {
                                                    field = $"exams.{i}.status",
                                                    message =
                                                        $"Invalid status for exam {newExam.ExamId}: '{newExam.Status}' to '{examDtOs[i].status}'."
                                                });
                                                break;
                                        }
                                }
                            }
                            else
                                errorList.Add(new ErrorDetail
                                {
                                    field = $"exams.{i}",
                                    message =
                                        "This exam has been assigned to another exam set or you are not the owner."
                                });

                            i++;
                        }

                        // Thay thế kỳ thi cũ bằng kỳ thi mới trong examSet
                        existExamSet.Exams = newExams;
                    }


                    // Cập nhật trạng thái exam set dựa trên trạng thái của các kỳ thi
                    if (existExamSet.Status != examSet.status)
                        switch (existExamSet.Status)
                        {
                            case "in_progress" when examSet.status == "pending_approval":
                            {
                                if (existExamSet.Exams.All(e => e.Status is "pending_approval" or "approved"))
                                    if (existExamSet.Exams.Count >= existExamSet.ExamQuantity)
                                        existExamSet.Status = examSet.status;
                                    else
                                        errorList.Add(new ErrorDetail
                                        {
                                            field = "exam_quantity",
                                            message =
                                                $"Exam set not enough exams: {existExamSet.Exams.Count}/{existExamSet.ExamQuantity}."
                                        });
                                else
                                    errorList.Add(new ErrorDetail
                                    {
                                        field = "status",
                                        message = "Not all exams are pending approval."
                                    });
                                break;
                            }
                            case "pending_approval" when examSet.status == "in_progress":
                            case "rejected" when examSet.status == "in_progress":
                                existExamSet.Status = examSet.status;
                                break;
                            default:
                                errorList.Add(new ErrorDetail
                                {
                                    field = "status",
                                    message = "Invalid status for exam set: '{existExamSet.Status}' to '{examSet.status}'"
                                });
                                break;
                        }
                }

                // Return errors if any were found
                if (errorList.Any())
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Update failed",
                        errors = errorList
                    };

                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Update successfully",
                    data = new DetailResponse { id = existExamSet.ExamSetId }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = $"An error occurred: {ex.Message}",
                    errors = new List<ErrorDetail> { new() { message = ex.InnerException?.ToString() ?? ex.Message } }
                };
            }
        }

        public async Task<BaseResponseId> DeleteExamSetAsync(int userId, int examSetId, bool withExam)
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
                        errors = new List<ErrorDetail> { new() { message = $"Exam set not found {examSetId}" } }
                    };

                // Check if the user has permission to delete the exam set
                if (userId != findExamSet.CreatorId)
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Forbidden",
                        errors = new List<ErrorDetail> { new() { message = "You do not have the right to delete this exam set." } }
                    };

                // Check if the exam set is approved
                if (findExamSet.Status == "approved")
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Forbidden",
                        errors = new List<ErrorDetail> { new() { message = "The exam set has been approved and cannot be deleted." } }
                    };

                // Check if the exam set is part of a proposal
                if (findExamSet.ProposalId > 0)
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Forbidden",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "This exam set is currently assigned to a proposal and cannot be deleted."
                            }
                        }
                    };

                var exams = findExamSet.Exams;

                // If there are exams related to this exam set
                if (exams.Any())
                {
                    if (!withExam)
                    {
                        // Check if any exam in the exam set is approved
                        var approvedExams = exams.Where(e => e.Status == "approved").ToList();
                        if (approvedExams.Any())
                            return new BaseResponseId
                            {
                                status = 405,
                                message = "Forbidden",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        message =
                                            "One or more exams have been approved, the exam set cannot be deleted."
                                    }
                                }
                            };

                        // Set ExamSetId to null for the related exams
                        var examIds = exams.Select(e => e.ExamId).ToList();
                        await _context.Exams
                            .Where(e => examIds.Contains(e.ExamId))
                            .ForEachAsync(e => e.ExamSetId = null);
                    }
                    else
                    {
                        // Check if any exam is approved
                        var approvedExams = exams.Where(e => e.Status == "approved").ToList();
                        if (approvedExams.Any())
                            return new BaseResponseId
                            {
                                status = 405,
                                message = "Forbidden",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        message =
                                            "One or more exams have been approved, the exam set cannot be deleted."
                                    }
                                }
                            };

                        // Check if any exam is not created by the current user
                        var nonCreatorExams = exams.Where(e => e.CreatorId != userId).ToList();
                        if (nonCreatorExams.Any())
                            return new BaseResponseId
                            {
                                status = 405,
                                message = "Forbidden",
                                errors = new List<ErrorDetail> { new() { message = "One or more exams are not yours, and cannot be deleted." } }
                            };

                        // Delete the related exams
                        _context.Exams.RemoveRange(exams);
                    }
                }

                // Finally, delete the exam set
                _context.ExamSets.Remove(findExamSet);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = withExam ? "Delete exam set successfully." : "Delete exam set and exams successfully.",
                    data = new DetailResponse { id = findExamSet.ExamSetId }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = $"An error occurred: {ex.Message}",
                    errors = new List<ErrorDetail> { new() { message = ex.InnerException?.ToString() ?? ex.Message } }
                };
            }
        }
    }
}