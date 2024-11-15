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
                var examSetQueryable = _context.ExamSets.AsNoTracking().AsQueryable();
                var examQueryable = _context.Exams.AsNoTracking().AsQueryable();

                if (queryObject.exceptValues != null && queryObject.exceptValues.Any())
                    examSetQueryable = examSetQueryable.Where(p => !queryObject.exceptValues.Contains(p.ExamSetId));

                if (!string.IsNullOrEmpty(queryObject.search))
                    examSetQueryable = examSetQueryable.Where(p =>
                        p.ExamSetName != null && p.ExamSetName.Contains(queryObject.search));

                if (!string.IsNullOrEmpty(queryObject.stateExamSet))
                    examSetQueryable = examSetQueryable.Where(e => e.Status == queryObject.stateExamSet);

                if (queryObject.courseId > 0)
                    examSetQueryable = examSetQueryable.Where(e => e.CourseId == queryObject.courseId);

                if (userId.HasValue)
                    examSetQueryable = examSetQueryable.Where(q => q.CreatorId == userId);

                // sap xep moi nhat dau tien
                if (queryObject.sort is not (null or "" or "string"))
                {
                    examSetQueryable = queryObject.sort.ToLower() switch
                    {
                        "exam_set_name" => examSetQueryable.OrderBy(es => es.ExamSetName), // Sắp xếp theo tên bộ đề thi
                        "exam_set_name_desc" => examSetQueryable.OrderByDescending(es =>
                            es.ExamSetName), // Sắp xếp giảm dần theo tên bộ đề thi
                        "status" => examSetQueryable.OrderBy(es => es.Status), // Sắp xếp theo trạng thái
                        "status_desc" => examSetQueryable.OrderByDescending(es =>
                            es.Status), // Sắp xếp giảm dần theo trạng thái
                        "exam_quantity" => examSetQueryable.OrderBy(es =>
                            es.ExamQuantity), // Sắp xếp theo số lượng đề thi
                        "exam_quantity_desc" => examSetQueryable.OrderByDescending(es =>
                            es.ExamQuantity), // Sắp xếp giảm dần theo số lượng đề thi
                        "create_at" => examSetQueryable.OrderBy(es => es.CreateAt), // Sắp xếp theo ngày tạo
                        "create_at_desc" => examSetQueryable.OrderByDescending(es =>
                            es.CreateAt), // Sắp xếp giảm dần theo ngày tạo
                        "update_at" => examSetQueryable.OrderBy(es => es.UpdateAt), // Sắp xếp theo ngày cập nhật
                        "update_at_desc" => examSetQueryable.OrderByDescending(es =>
                            es.UpdateAt), // Sắp xếp giảm dần theo ngày cập nhật
                        "department_id" => examSetQueryable.OrderBy(es => es.DepartmentId), // Sắp xếp theo ID phòng ban
                        "department_id_desc" => examSetQueryable.OrderByDescending(es =>
                            es.DepartmentId), // Sắp xếp giảm dần theo ID phòng ban
                        "major_id" => examSetQueryable.OrderBy(es => es.MajorId), // Sắp xếp theo ID ngành
                        "major_id_desc" => examSetQueryable.OrderByDescending(es =>
                            es.MajorId), // Sắp xếp giảm dần theo ID ngành
                        "course_id" => examSetQueryable.OrderBy(es => es.CourseId), // Sắp xếp theo ID khóa học
                        "course_id_desc" => examSetQueryable.OrderByDescending(es =>
                            es.CourseId), // Sắp xếp giảm dần theo ID khóa học
                        "proposal_id" => examSetQueryable.OrderBy(es => es.ProposalId), // Sắp xếp theo ID đề xuất
                        "proposal_id_desc" => examSetQueryable.OrderByDescending(es =>
                            es.ProposalId), // Sắp xếp giảm dần theo ID đề xuất
                        _ => examSetQueryable.OrderByDescending(es => es.CreateAt), // Sắp xếp mặc định theo ngày tạo
                    };
                }

                if (queryObject.userId.HasValue && !userId.HasValue)
                {
                    var proposalIds = await _context.TeacherProposals
                        .Where(tp => tp.UserId == (ulong)queryObject.userId.Value)
                        .Select(tp => tp.ProposalId)
                        .ToListAsync();

                    if (proposalIds.Any())
                        examSetQueryable = examSetQueryable.Where(p =>
                            p.ProposalId.HasValue && proposalIds.Contains(p.ProposalId.Value));
                }

                if (queryObject.isParamAddProposal ?? false)
                {
                    examSetQueryable = examSetQueryable.Where(e => e.ProposalId == null);

                    // Filter exams by ExamSetId
                    var examSetIds = await examSetQueryable.Select(e => e.ExamSetId).ToListAsync();

                    examQueryable =
                        examQueryable.Where(p => p.ExamSetId.HasValue && examSetIds.Contains(p.ExamSetId.Value));
                }

                if (queryObject.proposalId.HasValue)
                    examSetQueryable = examSetQueryable.Where(p => p.ProposalId == queryObject.proposalId);

                // Count total elements before pagination
                var totalCount = await examSetQueryable.CountAsync();

                // Fetch paginated data
                var examSets = await examSetQueryable
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
                        ? examQueryable.Where(e => e.ExamSetId == p.ExamSetId).Select(e => new ExamDTO
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

        public async Task<BaseResponse<ExamSetDTO>> GetDetailExamSetAsync(int examSetId)
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
                    .FirstOrDefaultAsync(p => p.ExamSetId == examSetId);

                // Return early if the ExamSet is not found
                if (examSet == null)
                {
                    return new BaseResponse<ExamSetDTO>
                    {
                        status = 404,
                        message = "Không tìm thấy",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = $"Không tìm thấy bộ đề {examSetId}"
                            }
                        }
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
                    status = 200,
                    message = "Thành công",
                    data = examSetDto
                };
            }
            catch (Exception exception)
            {
                return new BaseResponse<ExamSetDTO>
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

        public async Task<BaseResponseId> CreateExamSetAsync(int userId, ExamSetDTO examSetDto)
        {
            try
            {
                var errors = new List<ErrorDetail>();

                #region Kiem tra dau vao

                // kiem tra ten bo de
                if (examSetDto.name is null or "string")
                {
                    errors.Add(new ErrorDetail
                    {
                        field = "name",
                        message = "Tên bộ đề không hợp lệ"
                    });
                }

                var isExistingName = await _context.ExamSets.AsNoTracking()
                    .AnyAsync(e => examSetDto.name == e.ExamSetName);

                if (isExistingName)
                    errors.Add(new ErrorDetail
                    {
                        field = "name",
                        message = "Tên bộ đề đã tồn tại"
                    });

                // kiem tra so luong de thi yeu cau
                if (examSetDto.exam_quantity is null or < 0)
                {
                    errors.Add(new ErrorDetail
                    {
                        field = "exam_quantity",
                        message = "Số lượng đề thi yêu cầu kkông hợp lệ"
                    });
                }

                // kiem tra mo ta
                if (examSetDto.description is null or "string")
                {
                    errors.Add(new ErrorDetail
                    {
                        field = "description",
                        message = "Mô tả bộ đề không hợp lệ"
                    });
                }

                // kiem tra trang thai
                if (!_validStatus.Contains(examSetDto.status))
                    errors.Add(new ErrorDetail
                    {
                        field = "status",
                        message = "Trạng thái bộ đề không hợp lệ"
                    });

                // kiem tra hoc phan
                var course = await _context.Courses.AsNoTracking()
                    .Include(c => c.Major).ThenInclude(m => m!.Department)
                    .FirstOrDefaultAsync(c => c.CourseId == examSetDto.course.id);

                if (course == null)
                    errors.Add(new ErrorDetail
                    {
                        field = "course",
                        message = "Học phần không hợp lệ"
                    });
                else if (course.Major == null)
                    errors.Add(new ErrorDetail
                    {
                        field = "major",
                        message = "Chuyên ngành không hợp lệ"
                    });
                else if (course.Major.Department == null)
                    errors.Add(new ErrorDetail
                    {
                        field = "department",
                        message = "Khoa không hợp lệ"
                    });

                // kiem tra de xuat
                if (examSetDto.proposal is { id: > 0 })
                {
                    var isExistingProposal = await _context.Proposals.AsNoTracking()
                        .AnyAsync(p =>
                            p.ProposalId == examSetDto.proposal.id || p.PlanCode == examSetDto.proposal.code);

                    if (!isExistingProposal)
                        errors.Add(new ErrorDetail
                        {
                            message = "Không tìm thấy đề xuất"
                        });
                }

                // kiem tra cac de thi
                var examList = new List<Exam>();
                if (examSetDto.exams != null && examSetDto.exams.Any())
                {
                    var examIds = examSetDto.exams.Select(e => e.id).ToList();
                    var existingExams = await _context.Exams.Where(e => examIds.Contains(e.ExamId)).ToListAsync();
                    var examCodeSet = new HashSet<int>();

                    if (examIds.Any())
                    {
                        var i = 0;
                        foreach (var examId in examIds)
                        {
                            if (!examCodeSet.Add((int)examId!))
                            {
                                errors.Add(new ErrorDetail
                                {
                                    field = $"exams.{i}.id",
                                    message = $"Bài thi bị trùng lặp {examId}"
                                });
                            }
                            else if (existingExams.All(e => e.ExamId != examId))
                            {
                                errors.Add(new ErrorDetail
                                {
                                    field = $"exams.{i}.id",
                                    message = $"Không tồn tại bài thi {examId}"
                                });
                            }
                            else
                            {
                                var exam = existingExams.First(e => e.ExamId == examId);
                                examList.Add(exam);
                            }

                            i++;
                        }
                    }
                }

                // tra ve loi
                if (errors.Any())
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Có lỗi xảy ra",
                        errors = errors
                    };
                }

                #endregion

                // Tạo bộ đề mới
                var newExamSet = new ExamSet
                {
                    ExamSetName = examSetDto.name,
                    DepartmentId = examSetDto.department?.id,
                    MajorId = examSetDto.major?.id,
                    ExamQuantity = (int)examSetDto.exam_quantity!,
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
                    status = 200,
                    message = "Thêm bộ đề thành công",
                    data = new DetailResponse { id = newExamSet.ExamSetId }
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

        public async Task<BaseResponseId> UpdateExamSetAsync(int userId, ExamSetDTO examSetDto, bool isAdmin)
        {
            try
            {
                var errorList = new List<ErrorDetail>();

                #region Kiem tra dau vao

                // Check loi dau vao
                if (examSetDto.id <= 0)
                    errorList.Add(new ErrorDetail
                    {
                        field = "id",
                        message = $"Mã bộ đề không hợp lệ {examSetDto.id}"
                    });

                if (!_validStatus.Contains(examSetDto.status))
                {
                    errorList.Add(new ErrorDetail
                    {
                        field = "status",
                        message = $"Trạng thái không hợp lệ '{examSetDto.status}'"
                    });
                }

                if (examSetDto.course.id < 0)
                    errorList.Add(new ErrorDetail
                    {
                        field = "course",
                        message = $"Mã học phần không hợp lệ {examSetDto.course.id}"
                    });

                if (examSetDto.exam_quantity < 0)
                    errorList.Add(new ErrorDetail
                    {
                        field = "exam_quantity",
                        message = $"Số lượng đề thi không hợp lệ {examSetDto.exam_quantity}"
                    });

                // Kiem tra tinh hop le, trung exam
                var examDtos = examSetDto.exams?.ToList();

                if (examDtos != null && examDtos.Any())
                {
                    var examIds = new HashSet<int>();
                    for (var i = 0; i < examDtos.Count; i++)
                    {
                        var id = examDtos[i].id;
                        if (id <= 0)
                            errorList.Add(new ErrorDetail
                            {
                                field = $"{i}.exams.id",
                                message = $"Mã đề thi không hợp lệ {id}"
                            });
                        if (!examIds.Add((int)examDtos[i].id!))
                        {
                            errorList.Add(new ErrorDetail
                            {
                                field = $"{i}.exams.id",
                                message = $"Đề thi bị trùng {id}"
                            });
                        }
                    }
                }

                if (errorList.Any())
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Thất bại",
                        errors = errorList
                    };

                // Lay ra exam set da co kem theo exams
                var existExamSet = await _context.ExamSets.Include(ex => ex.Exams)
                    .FirstOrDefaultAsync(e => e.ExamSetId == examSetDto.id);

                if (existExamSet == null)
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = $"Không tìm thấy bộ đề {examSetDto.id}"
                            }
                        }
                    };

                // Bao loi khi bo de da duoc phe duyet
                if (existExamSet.Status == "approved")
                    return new BaseResponseId
                    {
                        status = 403,
                        message = "Không được phép",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "Bộ đề này đã được phê duyệt, không được sửa"
                            }
                        }
                    };

                #endregion

                // Retrieve the list of exams from the exam set
                var existExams = existExamSet.Exams.ToList();

                #region Admin update

                // Check if the user is an admin
                if (isAdmin)
                {
                    if (existExamSet.Status != examSetDto.status)
                    {
                        // trang thai phu hop cho admin
                        if (existExamSet.Status != "pending_approval" &&
                            examSetDto.status is not ("approved" or "rejected"))
                        {
                            return new BaseResponseId
                            {
                                status = 400,
                                message = "Thất bại",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        field = "status",
                                        message = "Trạng thái không phù hợp cho admin"
                                    }
                                }
                            };
                        }

                        switch (examSetDto.status)
                        {
                            case "approved":
                                if (existExamSet.Exams.Count < existExamSet.ExamQuantity)
                                {
                                    errorList.Add(new ErrorDetail
                                    {
                                        field = "exam_quantity",
                                        message = "Số lượng đề thi chưa đủ"
                                    });
                                }
                                else
                                {
                                    if (existExamSet.Exams.Count < existExamSet.ExamQuantity)
                                    {
                                        errorList.Add(new ErrorDetail
                                        {
                                            field = "exam_quantity",
                                            message = "Số lượng đề thi chưa đủ"
                                        });
                                    }
                                    else
                                    {
                                        if (existExams.All(e => e.Status is "pending_approval" or "approved"))
                                        {
                                            foreach (var existExam in existExams)
                                            {
                                                existExam.Status = existExam.Status == "approved"
                                                    ? existExam.Status
                                                    : examSetDto.status;
                                            }

                                            existExamSet.Status = examSetDto.status;
                                        }
                                        else
                                        {
                                            var i = 0;
                                            foreach (var existExam in existExams)
                                            {
                                                if (existExam.Status is "in_progress" or "rejected")
                                                    errorList.Add(new ErrorDetail
                                                    {
                                                        field = $"exams.{i}.status",
                                                        message =
                                                            $"Trạng thái đề thi {existExam.ExamId} không hợp lệ"
                                                    });
                                                i++;
                                            }
                                        }
                                    }
                                }

                                break;

                            case "rejected":
                                if (existExams.All(ex => ex.Status is "rejected"))
                                    existExamSet.Status = examSetDto.status;
                                if (existExams.Where(e => e.Status != "approved")
                                    .All(e => e.Status == "rejected"))
                                    existExamSet.Status = "rejected";

                                break;

                            default:
                                errorList.Add(new ErrorDetail
                                {
                                    field = "status",
                                    message = "Trạng thái bộ đề không hợp lệ"
                                });

                                break;
                        }
                    }

                    // Update the exam set
                    if (existExamSet.ExamQuantity != examSetDto.exam_quantity)
                        if (examSetDto.exam_quantity != null)
                            existExamSet.ExamQuantity = (int)examSetDto.exam_quantity;

                    existExamSet.UpdateAt = DateOnly.FromDateTime(DateTime.Now);

                    // Return error response if there are any validation errors
                    if (errorList.Any())
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Thất bại",
                            errors = errorList
                        };
                    }
                }

                #endregion

                #region Teacher update

                // Neu user khong phai admin
                else
                {
                    // Kiem tra exam set co phai do nguoi dung dang dang nhap tao khong
                    if (existExamSet.CreatorId != userId)
                    {
                        return new BaseResponseId
                        {
                            status = 403,
                            message = "Không được phép",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    message = "Bạn không có quyền sửa đề thi của giảng viên khác"
                                }
                            }
                        };
                    }

                    // Trang thai dau vao chi danh cho admin
                    if (examSetDto.status is "approved" or "rejected")
                        return new BaseResponseId
                        {
                            status = 403,
                            message = "Không được phép",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    field = "status",
                                    message = "Trạng thái cập nhật đề thi chỉ dành cho admin"
                                }
                            }
                        };

                    // Thay doi khoa
                    if (examSetDto.department is { id: > 0 })
                    {
                        if (!await _context.Departments.AnyAsync(d => d.DepartmentId == examSetDto.department.id))
                            errorList.Add(new ErrorDetail
                            {
                                field = "department",
                                message = $"Không tìm thấy khoa {examSetDto.department.id}"
                            });
                        else existExamSet.DepartmentId = examSetDto.department.id;
                    }

                    // Thay doi chuyen nghanh
                    if (examSetDto.major is { id: > 0 })
                    {
                        if (!await _context.Majors.AnyAsync(m => m.MajorId == examSetDto.major.id))
                            errorList.Add(new ErrorDetail
                            {
                                field = "major",
                                message = $"Không tìm thấy chuyên ngành {examSetDto.major.id}"
                            });
                        else existExamSet.MajorId = examSetDto.major.id;
                    }

                    // Thay doi de xuat
                    if (examSetDto.proposal is { id: > 0 })
                    {
                        if (!await _context.Proposals.AnyAsync(p => p.ProposalId == examSetDto.proposal.id))
                            errorList.Add(new ErrorDetail
                            {
                                field = "proposal",
                                message = $"Không tìm thấy đề xuất {examSetDto.proposal.id}"
                            });
                        else existExamSet.ProposalId = examSetDto.proposal.id;
                    }

                    // Thay doi hoc phan
                    if (examSetDto.course.id > 0)
                    {
                        if (!await _context.Courses.AnyAsync(c => c.CourseId == examSetDto.course.id))
                            errorList.Add(new ErrorDetail
                            {
                                field = "course",
                                message = $"Không tìm thấy học phần {examSetDto.course.id}"
                            });
                        else existExamSet.CourseId = examSetDto.course.id;
                    }

                    existExamSet.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
                    existExamSet.ExamSetName = examSetDto.name == "string" || string.IsNullOrEmpty(examSetDto.name)
                        ? existExamSet.ExamSetName
                        : examSetDto.name;
                    existExamSet.Description =
                        examSetDto.description == "string" || string.IsNullOrEmpty(examSetDto.description)
                            ? existExamSet.Description
                            : examSetDto.description;
                    existExamSet.UpdateAt = DateOnly.FromDateTime(DateTime.Now);


                    // Cap nhat exams
                    if (examDtos != null)
                    {
                        // Lấy danh sách kỳ thi mới dựa trên thông tin từ examDTO
                        var newExams = await _context.Exams
                            .Where(e => examDtos.Select(dto => dto.id).Contains(e.ExamId))
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
                                    field = "exams",
                                    message = $"Đề thi đã được phê duyệt, không thể gỡ khỏi bộ đề {oldExam.ExamId}."
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
                        var examStatusDict = examDtos.ToDictionary(dto => dto.id, dto => dto.status);
                        var i = 0;

                        // Cập nhật trạng thái kỳ thi mới dựa trên examDTO
                        foreach (var newExam in newExams)
                        {
                            if ((newExam.ExamSetId == null || newExam.ExamSetId == existExamSet.ExamSetId) &&
                                newExam.CreatorId == userId)
                            {
                                if (examStatusDict.TryGetValue(newExam.ExamId, out var newStatus))
                                {
                                    if (newExam.Status != examDtos[i].status)

                                        if (newStatus is "approved" or "rejected")
                                            errorList.Add(new ErrorDetail
                                            {
                                                field = $"exam_set.exams.{i}",
                                                message = "Giảng viên không đuợc phê duyệt đề thi"
                                            });
                                        else
                                            switch (newExam.Status)
                                            {
                                                case "in_progress" when
                                                    examDtos[i].status == "pending_approval":
                                                    newExam.Status = examDtos[i].status;
                                                    newExam.Comment = string.Empty;
                                                    break;
                                                case "pending_approval" when
                                                    examDtos[i].status == "in_progress":
                                                case "rejected" when examDtos[i].status == "in_progress":
                                                    newExam.Status = examDtos[i].status;
                                                    break;
                                                default:
                                                    errorList.Add(new ErrorDetail
                                                    {
                                                        field = $"exams.{i}.status",
                                                        message =
                                                            $"Trạng thái không hợp lệ {newExam.ExamId}: '{newExam.Status}' -> '{examDtos[i].status}'"
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
                                        "Đề thi nà đã được gán cho bộ đề khác không phải của bạn"
                                });

                            i++;
                        }

                        // Thay thế kỳ thi cũ bằng kỳ thi mới trong examSet
                        existExamSet.Exams = newExams;
                    }


                    // Cập nhật trạng thái exam set dựa trên trạng thái của các kỳ thi
                    if (existExamSet.Status != examSetDto.status)
                        switch (existExamSet.Status)
                        {
                            case "in_progress" when examSetDto.status == "pending_approval":
                            {
                                if (existExamSet.Exams.All(e => e.Status is "pending_approval" or "approved"))
                                    if (existExamSet.Exams.Count >= existExamSet.ExamQuantity)
                                        existExamSet.Status = examSetDto.status;
                                    else
                                        errorList.Add(new ErrorDetail
                                        {
                                            field = "exam_quantity",
                                            message =
                                                $"Bộ đề chưa đủ đề thi: {existExamSet.Exams.Count}/{existExamSet.ExamQuantity}."
                                        });
                                else
                                    errorList.Add(new ErrorDetail
                                    {
                                        field = "status",
                                        message = "Các đề thi của bộ đề chưa được chuyển trạng thái chờ phê duyệt"
                                    });
                                break;
                            }
                            case "pending_approval" when examSetDto.status == "in_progress":
                            case "rejected" when examSetDto.status == "in_progress":
                                existExamSet.Status = examSetDto.status;
                                break;
                            default:
                                errorList.Add(new ErrorDetail
                                {
                                    field = "status",
                                    message =
                                        $"Trạng thái không hợp lệ cho bộ đề: '{existExamSet.Status}' -> '{examSetDto.status}'"
                                });
                                break;
                        }
                }

                #endregion

                // Return errors if any were found
                if (errorList.Any())
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Thất bại",
                        errors = errorList
                    };

                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Thành công",
                    data = new DetailResponse
                    {
                        id = existExamSet.ExamSetId
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
                        message = "Không tìm thấy",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = $"Không tìm thấy bộ đề {examSetId}"
                            }
                        }
                    };

                // Check if the user has permission to delete the exam set
                if (userId != findExamSet.CreatorId)
                    return new BaseResponseId
                    {
                        status = 403,
                        message = "Không được phép",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "Bạn không được phép xóa bộ đề của giảng viên khác"
                            }
                        }
                    };

                // Check if the exam set is approved
                if (findExamSet.Status == "approved")
                    return new BaseResponseId
                    {
                        status = 403,
                        message = "Không được phép",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "Bộ đề đã được phê duyệt không thể xóa"
                            }
                        }
                    };

                // Check if the exam set is part of a proposal
                if (findExamSet.ProposalId > 0)
                    return new BaseResponseId
                    {
                        status = 403,
                        message = "Không được phép",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "Bộ đề đang được gán cho đề xuất, không thể xóa"
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
                                status = 403,
                                message = "Không được phép",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        message =
                                            "Một hoặc nhiều đề thi của bộ đề đã được duyệt, không thể xóa"
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
                                status = 403,
                                message = "Không được phép",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        message =
                                            "Một hoặc nhiều đề thi của bộ đề đã được duyệt, không thể xóa bộ đề"
                                    }
                                }
                            };

                        // Check if any exam is not created by the current user
                        var nonCreatorExams = exams.Where(e => e.CreatorId != userId).ToList();
                        if (nonCreatorExams.Any())
                            return new BaseResponseId
                            {
                                status = 403,
                                message = "Không được phép",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        message = "Một hoặc nhiều đề thi không phải của bạn, không thể xóa"
                                    }
                                }
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
                    message = withExam ? "Xóa bộ đề thành công" : "Xóa bộ đề và đề thi thành công",
                    data = new DetailResponse { id = findExamSet.ExamSetId }
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