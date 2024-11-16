using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository;

public class ExamRepository : IExamRepository
{
    private readonly ApplicationDbContext _context;

    private readonly List<string> _validStatus = new()
        { "in_progress", "rejected", "approved", "pending_approval" };

    public ExamRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PageResponse<ExamDTO>> GetListExamsAsync(ExamRequestParams queryObject, int? userId)
    {
        var startRow = (queryObject.page - 1) * queryObject.size;
        var examQueryable = _context.Exams.AsNoTracking().AsQueryable();
        var users = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);
        var teachers = await _context.Teachers.AsNoTracking().ToDictionaryAsync(t => t.Id);
        if (queryObject.exceptValues != null && queryObject.exceptValues.Any())
        {
            examQueryable = examQueryable.Where(p => !queryObject.exceptValues.Contains(p.ExamId));
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(queryObject.search))
        {
            examQueryable = examQueryable.Where(e =>
                e.ExamName != null && (e.ExamCode.Contains(queryObject.search) ||
                                       e.ExamName.Contains(queryObject.search)));
        }

        if (queryObject.isGetForAddExamSet != null && (bool)queryObject.isGetForAddExamSet)
        {
            examQueryable = examQueryable.Where(e => e.ExamSetId == null);
        }

        // Apply filters based on query parameters
        if (queryObject.exam_set_id != null)
        {
            examQueryable = examQueryable.Where(p => p.ExamSetId == queryObject.exam_set_id);
        }

        if (!string.IsNullOrEmpty(queryObject.status))
        {
            examQueryable = examQueryable.Where(e => e.Status == queryObject.status);
        }

        if (queryObject.academic_year_id > 0)
        {
            examQueryable = examQueryable.Where(e => e.AcademicYearId == queryObject.academic_year_id);
        }

        if (queryObject.month_upload > 0)
        {
            examQueryable = examQueryable.Where(e =>
                e.CreateAt != null && e.CreateAt.Value.Month == queryObject.month_upload);
        }

        // Apply userId filter if provided
        if (userId.HasValue)
        {
            examQueryable = examQueryable.Where(e => e.CreatorId == userId.Value);
        }

        // Apply sorting
        if (queryObject.sort is not (null or "" or "string"))
        {
            examQueryable = queryObject.sort.ToLower() switch
            {
                "exam_code" => examQueryable.OrderBy(e => e.ExamCode), // Sắp xếp theo mã đề thi
                "exam_code_desc" => examQueryable.OrderByDescending(e =>
                    e.ExamCode), // Sắp xếp giảm dần theo mã đề thi
                "exam_name" => examQueryable.OrderBy(e => e.ExamName), // Sắp xếp theo tên đề thi
                "exam_name_desc" => examQueryable.OrderByDescending(e =>
                    e.ExamName), // Sắp xếp giảm dần theo tên đề thi
                "status" => examQueryable.OrderBy(e => e.Status), // Sắp xếp theo trạng thái
                "status_desc" => examQueryable.OrderByDescending(e => e.Status), // Sắp xếp giảm dần theo trạng thái
                "create_at" => examQueryable.OrderBy(e => e.CreateAt), // Sắp xếp theo ngày tạo
                "create_at_desc" => examQueryable.OrderByDescending(e =>
                    e.CreateAt), // Sắp xếp giảm dần theo ngày tạo
                "update_at" => examQueryable.OrderBy(e => e.UpdateAt), // Sắp xếp theo ngày cập nhật
                "update_at_desc" => examQueryable.OrderByDescending(e =>
                    e.UpdateAt), // Sắp xếp giảm dần theo ngày cập nhật
                "exam_set_id" => examQueryable.OrderBy(e => e.ExamSetId), // Sắp xếp theo ID bộ đề thi
                "exam_set_id_desc" => examQueryable.OrderByDescending(e =>
                    e.ExamSetId), // Sắp xếp giảm dần theo ID bộ đề thi
                "creator_id" => examQueryable.OrderBy(e => e.CreatorId), // Sắp xếp theo ID người tạo
                "creator_id_desc" => examQueryable.OrderByDescending(e =>
                    e.CreatorId), // Sắp xếp giảm dần theo ID người tạo
                "academic_year_id" => examQueryable.OrderBy(e => e.AcademicYearId), // Sắp xếp theo ID năm học
                "academic_year_id_desc" => examQueryable.OrderByDescending(e =>
                    e.AcademicYearId), // Sắp xếp giảm dần theo ID năm học
                _ => examQueryable.OrderByDescending(e => e.CreateAt), // Sắp xếp mặc định theo ngày tạo
            };
        }

        // Total number of records after filtering
        var totalCount = await examQueryable.CountAsync();

        // Fetch distinct AcademicYearIds
        var academicYearIds = await examQueryable.Select(p => p.AcademicYearId).Distinct().ToListAsync();
        var academicYears = await _context.AcademicYears
            .Where(a => academicYearIds.Contains(a.AcademicYearId))
            .ToDictionaryAsync(a => a.AcademicYearId, a => a.YearName);

        // Fetch paginated exam list
        var exams = await examQueryable
            .Skip(startRow)
            .Take(queryObject.size)
            .Select(p => new ExamDTO
            {
                comment = p.Comment,
                attached_file = p.AttachedFile,
                description = p.Description,
                code = p.ExamCode,
                id = p.ExamId,
                name = p.ExamName,
                status = p.Status,
                exam_set = p.ExamSetId != null
                    ? new CommonObject
                    {
                        id = (int)p.ExamSetId,
                        name = p.ExamSet.ExamSetName
                    }
                    : null,
                user = p.CreatorId.HasValue && users.ContainsKey((ulong)p.CreatorId.Value)
                    ? new
                    {
                        id = (int)users[(ulong)p.CreatorId.Value].Id,
                        name = users[(ulong)p.CreatorId.Value].Email,
                        fullname = users[(ulong)p.CreatorId.Value].TeacherId.HasValue &&
                                   users[(ulong)p.CreatorId.Value].TeacherId != null &&
                                   teachers.ContainsKey(users[(ulong)p.CreatorId.Value].TeacherId.Value)
                            ? teachers[users[(ulong)p.CreatorId.Value].TeacherId.Value].Name
                            : ""
                    }
                    : null,
                create_at = p.CreateAt.ToString(),
                academic_year = p.AcademicYearId.HasValue && academicYears.ContainsKey(p.AcademicYearId.Value)
                    ? new CommonObject
                    {
                        id = p.AcademicYearId.Value,
                        name = academicYears[p.AcademicYearId.Value]
                    }
                    : null
            }).ToListAsync();

        // Return paginated result
        return new PageResponse<ExamDTO>
        {
            totalElements = totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
            size = queryObject.size,
            page = queryObject.page,
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
                    message = "Không tìm thấy",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = $"Không tìm thấy đề thi {examId}"
                        }
                    }
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
                exam_set = exam.ExamSetId != null
                    ? new CommonObject
                    {
                        id = (int)exam.ExamSetId,
                        name = examSets.TryGetValue((int)exam.ExamSetId, value: out var examSet)
                            ? examSet.ExamSetName
                            : null
                    }
                    : null,
                user = exam.CreatorId.HasValue && users.TryGetValue((ulong)exam.CreatorId.Value, out var user)
                    ? new
                    {
                        id = (int)user.Id,
                        name = user.Email,
                        fullname = user.TeacherId.HasValue &&
                                   teachers.TryGetValue(user.TeacherId.Value, out var teacher)
                            ? teacher.Name
                            : ""
                    }
                    : null,
                status = exam.Status,
                create_at = exam.CreateAt.ToString(),
                academic_year = exam.AcademicYearId != null &&
                                academicYears.TryGetValue((int)exam.AcademicYearId, out var yearName)
                    ? new CommonObject
                    {
                        id = exam.AcademicYearId.Value,
                        name = yearName
                    }
                    : null
            };

            return new BaseResponse<ExamDTO>
            {
                status = 200,
                message = "Thành công",
                data = examDto
            };
        }
        catch (Exception exception)
        {
            return new BaseResponse<ExamDTO>
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

    public async Task<BaseResponse<List<DetailResponse>>> CreateExamsAsync(List<ExamDTO> exams, int userId)
    {
        try
        {
            if (!exams.Any())
            {
                return new BaseResponse<List<DetailResponse>>
                {
                    status = 400,
                    message = "Thất bại",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = "Không có đề thi nào để thêm mới"
                        }
                    }
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

            var first = exams.FirstOrDefault();

            var existingNames = await _context.Exams.AsNoTracking()
                .Where(e => exams.Select(x => x.name).Contains(e.ExamName) &&
                            first != null &&
                            (first.exam_set == null || e.ExamSetId == first.exam_set.id) &&
                            e.CreatorId == userId)
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

            for (var i = 0; i < exams.Count; i++)
            {
                var examDto = exams[i];

                #region Validation

                // Validate code
                if (string.IsNullOrEmpty(examDto.code) || examDto.code == "string")
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.code",
                        message = $"Mã đề thi không hợp lệ '{examDto.code}'"
                    });
                }
                else if (existingCodes.Contains(examDto.code))
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.code",
                        message = $"Mã đề thi đã tồn tại '{examDto.code}'"
                    });
                }

                // Validate name
                if (string.IsNullOrEmpty(examDto.name) || examDto.name == "string")
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.name",
                        message = $"Tên đề thi không hợp lệ '{examDto.name}'"
                    });
                }
                else if (existingNames.Contains(examDto.name))
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.name",
                        message = $"Tên đề thi đã tồn tại '{examDto.name}'"
                    });
                }

                // Validate attached file
                if (string.IsNullOrEmpty(examDto.attached_file) || examDto.attached_file == "string")
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.attached_file",
                        message = $"File đề thi không hợp lệ '{examDto.attached_file}'"
                    });
                }
                else if (existingFiles.Contains(examDto.attached_file))
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.attached_file",
                        message = $"File đề thi đã tồn tại '{examDto.attached_file}'"
                    });
                }

                // Validate status
                if (string.IsNullOrEmpty(examDto.status) || examDto.status == "string" ||
                    !_validStatus.Contains(examDto.status))
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.status",
                        message = $"Trạng thái không hợp lệ '{examDto.status}'"
                    });
                }

                // Validate exam set
                if (examDto.exam_set is { id: > 0 } &&
                    !examSetIds.Contains((int)examDto.exam_set.id))
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.exam_set",
                        message = $"Không tìm thấy bộ đề '{examDto.exam_set.id}'"
                    });
                }

                // Validate academic year
                if (examDto.academic_year != null && !academicYearIds.Contains(examDto.academic_year.id))
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.academic_year.id",
                        message = $"Không tìm thấy năm học '{examDto.academic_year.name}'"
                    });
                }

                if (examDto is { status: "pending_approval", exam_set: null } || examDto.exam_set is { id: 0 })
                {
                    errors.Add(new ErrorDetail
                    {
                        field = $"exams.{i}.status",
                        message = "Đề thi chưa được gán cho bộ đề nào, không được gửi yêu cầu phê duyệt"
                    });
                }

                #endregion

                // If no errors, add exam to the list
                if (!errors.Any())
                {
                    listExam.Add(new Exam
                    {
                        ExamCode = examDto.code,
                        ExamName = examDto.name,
                        ExamSetId = examDto.exam_set?.id > 0 ? examDto.exam_set?.id : null,
                        AcademicYearId = examDto.academic_year?.id,
                        AttachedFile = examDto.attached_file,
                        Description = examDto.description == "string" ? string.Empty : examDto.description,
                        CreateAt = DateOnly.FromDateTime(DateTime.Now),
                        Status = examDto.status,
                        CreatorId = userId
                    });
                }
            }

            // If no exams were successfully added, return the errors
            if (errors.Any())
            {
                return new BaseResponse<List<DetailResponse>>
                {
                    status = 400,
                    message = "Thêm mới thất bại",
                    errors = errors
                };
            }

            // Save valid exams to the database
            await _context.AddRangeAsync(listExam);
            await _context.SaveChangesAsync();

            return new BaseResponse<List<DetailResponse>>
            {
                status = 200,
                message = "Thêm mới thành công",
                data = listExam.Select(e => new DetailResponse { id = e.ExamId }).ToList()
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

    public async Task<BaseResponseId> UpdateExamAsync(int userId, bool isAdmin, ExamDTO examDto)
    {
        try
        {
            #region Validation

            var existExam =
                await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examDto.id || e.ExamCode == examDto.code);

            if (existExam == null)
            {
                return new BaseResponseId
                {
                    status = 404,
                    message = "Không tìm thấy",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            field = "id",
                            message = $"Không tìm thấy đề thi {examDto.id}"
                        }
                    }
                };
            }

            if (existExam.CreatorId != userId && isAdmin == false)
            {
                return new BaseResponseId
                {
                    status = 403,
                    message = "Không được phép",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = "Bạn không được sửa đề thi của giảng viên khác"
                        }
                    }
                };
            }

            if (existExam.Status == "approved")
            {
                return new BaseResponseId
                {
                    status = 403,
                    message = "Không được phép",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = "Đề thi đã được phê duyệt, không được sửa"
                        }
                    }
                };
            }

            if (!_validStatus.Contains(examDto.status))
            {
                return new BaseResponseId
                {
                    status = 400,
                    message = "Không hợp lệ",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            field = "status",
                            message = "Trạng thái không hợp lệ"
                        }
                    }
                };
            }

            #endregion

            #region Admin update

            // Nếu là admin
            if (isAdmin)
            {
                if (existExam.Status != "pending_approval" ||
                    examDto.status is not ("approved" or "rejected"))
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Không hợp lệ",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "status",
                                message = "Admin không được chuyển trạng thái này"
                            }
                        }
                    };
                }

                if (examDto.comment is null or "" or "string")
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Thất bại",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "comment",
                                message = "Vui lòng nhập bình luận"
                            }
                        }
                    };
                }

                if (existExam.ExamSetId == null)
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Sửa thất bại",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "Đề thi chưa được gán với bộ đề nào"
                            }
                        }
                    };
                }

                existExam.Status = examDto.status;
                existExam.Comment = examDto.comment;
                existExam.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
            }

            #endregion

            #region Teacher update

            // Nếu là giảng viên
            else
            {
                if (existExam.ExamName != examDto.name)
                {
                    var isExistName = await _context.Exams.AnyAsync(exam =>
                        exam.ExamName == examDto.name && exam.ExamSetId == existExam.ExamSetId &&
                        exam.CreatorId == userId);

                    if (isExistName)
                    {
                        return new BaseResponseId
                        {
                            status = 409,
                            message = "Tên bài thi bị trùng",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    field = "name",
                                    message = $"Tên đề thi bị trùng '{examDto.name}'"
                                }
                            }
                        };
                    }
                }

                if (examDto.academic_year is { id: <= 0 } ||
                    !await _context.AcademicYears.AnyAsync(a =>
                        examDto.academic_year != null && a.AcademicYearId == examDto.academic_year.id))
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Không hợp lệ",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "academic_year",
                                message = $"Năm học không hợp lệ '{examDto.academic_year!.name}'"
                            }
                        }
                    };
                }

                if (examDto is { status: "pending_approval", exam_set: null } || examDto.exam_set is { id: 0 })
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Yêu cầu không hợp lệ",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "exams.exam_set",
                                message = "Đề thi chưa được gán cho bộ đề nào"
                            }
                        }
                    };
                }

                switch (examDto.exam_set)
                {
                    case { id: < 0 }:
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Không hợp lệ",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    field = "exam_set",
                                    message = $"Bộ đề không hợp lệ '{examDto.exam_set.id}'"
                                }
                            }
                        };

                    case { id: > 0 } when
                        !await _context.ExamSets.AnyAsync(e => e.ExamSetId == examDto.exam_set.id):
                        return new BaseResponseId
                        {
                            status = 404,
                            message = "Không tìm thấy",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    field = "exam_set",
                                    message = $"Không tìm thấy bộ đề '{examDto.exam_set.id}'"
                                }
                            }
                        };
                }


                // Cập nhật trạng thái
                if (existExam.Status != examDto.status)
                {
                    if (existExam.Status is "approved")
                    {
                        return new BaseResponseId
                        {
                            status = 403,
                            message = "Không được phép",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    message = "Đề thi đã phê duyệt, không được sửa"
                                }
                            }
                        };
                    }

                    if (examDto.status is "approved" or "rejected")
                    {
                        return new BaseResponseId
                        {
                            status = 403,
                            message = "Không được phép",
                            errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    message = "Trạng thái cập nhật không dành cho giảng viên"
                                }
                            }
                        };
                    }

                    switch (existExam.Status)
                    {
                        case "rejected":
                        {
                            if (examDto.status is not ("in_progress" or "pending_approval"))
                            {
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Yêu cầu không hợp lệ",
                                    errors = new List<ErrorDetail>
                                    {
                                        new()
                                        {
                                            message = "Trạng thái không phù hợp cho đề thi"
                                        }
                                    }
                                };
                            }

                            if (examDto.status is "pending_approval" &&
                                examDto.name == existExam.ExamName &&
                                examDto.attached_file == existExam.AttachedFile &&
                                examDto.description == existExam.Description)
                            {
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Yêu cầu không hợp lệ",
                                    errors = new List<ErrorDetail>
                                    {
                                        new()
                                        {
                                            message = "Cần xem lại thông tin đề thi trước khi yêu cầu phê duyệt"
                                        }
                                    }
                                };
                            }

                            break;
                        }

                        case "in_progress":
                            if (examDto.status is not "pending_approval")
                            {
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Yêu cầu không hợp lệ",
                                    errors = new List<ErrorDetail>
                                    {
                                        new()
                                        {
                                            message = "Trạng thái không phù hợp cho đề thi"
                                        }
                                    }
                                };
                            }

                            break;

                        case "pending_approval":
                            if (examDto.status is not "in_progress")
                            {
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Yêu cầu không hợp lệ",
                                    errors = new List<ErrorDetail>
                                    {
                                        new()
                                        {
                                            message = "Trạng thái không phù hợp cho đề thi"
                                        }
                                    }
                                };
                            }

                            break;

                        default:
                            return new BaseResponseId
                            {
                                status = 400,
                                message = "Yêu cầu không hợp lệ",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        message = "Trạng thái không phù hợp cho đề thi"
                                    }
                                }
                            };
                    }

                    existExam.Status = examDto.status;
                }

                // Thay đổi cần thiết
                existExam.ExamName = examDto.name != "string" && examDto.name != existExam.ExamName
                    ? examDto.name
                    : existExam.ExamName;
                existExam.AttachedFile = examDto.attached_file != "string" &&
                                         examDto.attached_file != existExam.AttachedFile
                    ? examDto.attached_file
                    : existExam.AttachedFile;
                existExam.Description =
                    examDto.description != "string" && examDto.description != existExam.Description
                        ? examDto.description
                        : existExam.Description;
                existExam.ExamSetId = examDto.exam_set?.id == 0 ? existExam.ExamSetId : examDto.exam_set?.id;
                existExam.AcademicYearId = examDto.academic_year?.id;
                existExam.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
            }

            #endregion

            #region Update exam set status

            // Lấy Exam Set cùng với Exam
            var existExamSet = await _context.ExamSets.Include(ex => ex.Exams)
                .FirstOrDefaultAsync(es => es.ExamSetId == existExam.ExamSetId);

            if (existExamSet != null)
            {
                // Lấy danh sách tất cả các Exam trong ExamSet
                var existExams = existExamSet.Exams;
                var isEnough = existExamSet.ExamQuantity == existExams.Count;

                if (isEnough)
                {
                    // Kiểm tra nếu tất cả các exam trong examsToCheck có cùng trạng thái
                    var allExamsApproved = existExams.All(exam => exam.Status == "approved");
                    var allExamsRejected = existExams.All(exam => exam.Status == "rejected");
                    var allExamsInProgress = existExams.All(exam => exam.Status == "in_progress");
                    var allExamsPendingApproval = existExams.All(exam => exam.Status == "pending_approval");


                    if (allExamsApproved)
                    {
                        existExamSet.Status = "approved";
                    }
                    else if (allExamsRejected)
                    {
                        existExamSet.Status = "rejected";
                    }
                    else if (allExamsInProgress)
                    {
                        existExamSet.Status = "in_progress";
                    }
                    else if (allExamsPendingApproval)
                    {
                        existExamSet.Status = "pending_approval";
                    }
                    else
                    {
                        var allPending = existExams
                            .Where(exam => exam.Status is not "approved")
                            .All(exam => exam.Status is "pending_approval");

                        var anyRejectedOrInProgress =
                            existExams.Any(exam => exam.Status is "rejected" or "in_progress");

                        if (allPending)
                        {
                            existExamSet.Status = "pending_approval";
                        }
                        else if (anyRejectedOrInProgress)
                        {
                            existExamSet.Status = "in_progress";
                        }
                    }
                }
                else
                {
                    // Nếu chưa đủ số lượng, mặc định chuyển trạng thái exam_set về "in_progress"
                    existExamSet.Status = "in_progress";
                }

                // Cập nhật exam_set
                _context.ExamSets.Update(existExamSet);
            }

            #endregion

            // Lưu thay đổi vào DB
            _context.Exams.Update(existExam);
            await _context.SaveChangesAsync();

            return new BaseResponseId
            {
                status = 200,
                message = "Cập nhật thành công",
                data = new DetailResponse { id = existExam.ExamId }
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
                    message = "Không tìm thấy",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            field = "examId",
                            message = $"Không tìm thấy đề thi {examId}"
                        }
                    }
                };
            }

            if (existExam.CreatorId != userId)
            {
                return new BaseResponseId
                {
                    status = 403,
                    message = "Không được phép",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = "Bạn không được xóa đề thi của giảng viên khác"
                        }
                    }
                };
            }

            if (existExam.Status == "approved")
            {
                return new BaseResponseId
                {
                    status = 403,
                    message = "Không được phép",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = "Đề thi đã được phê duyệt, không thể xóa"
                        }
                    }
                };
            }

            _context.Exams.Remove(existExam);
            await _context.SaveChangesAsync();

            return new BaseResponseId
            {
                status = 200,
                message = "Xóa thành công",
                data = new DetailResponse { id = existExam.ExamId }
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