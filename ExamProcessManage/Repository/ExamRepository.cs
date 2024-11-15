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

        private readonly List<string> _validStatus = new()
            { "in_progress", "rejected", "approved", "pending_approval" };

        public ExamRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ExamDTO>> GetListExamsAsync(ExamRequestParams query, int? userId)
        {
            var startRow = (query.page - 1) * query.size;
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
                baseQuery = baseQuery.Where(e =>
                    e.ExamName != null && (e.ExamCode.Contains(query.search) || e.ExamName.Contains(query.search)));
            }

            if (query.isGetForAddExamSet != null && (bool)query.isGetForAddExamSet)
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
                baseQuery = baseQuery.Where(e => e.CreateAt != null && e.CreateAt.Value.Month == query.month_upload);
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
                totalPages = (int)Math.Ceiling((double)totalCount / query.size),
                size = query.size,
                page = query.page,
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

                for (var i = 0; i < exams.Count; i++)
                {
                    var examDto = exams[i];

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
                #region Validate

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

                // Nếu là admin
                if (isAdmin)
                {
                    if (existExam.Status == "pending_approval" &&
                        examDto.status is "approved" or "rejected")
                    {
                        if (!string.IsNullOrEmpty(examDto.comment) && examDto.comment != "string")
                            existExam.Comment = examDto.comment;
                        else
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

                        existExam.Status = examDto.status;
                        existExam.UpdateAt = DateOnly.FromDateTime(DateTime.Now);
                    }
                    else
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
                }
                // Nếu là giảng viên
                else
                {
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
                        switch (existExam.Status)
                        {
                            case "in_progress" when examDto.status == "pending_approval":
                            case "pending_approval" when examDto.status == "in_progress":
                                existExam.Status = examDto.status;
                                existExam.Comment = examDto.comment;

                                break;
                            case "rejected" when
                                examDto.status is "in_progress" or "pending_approval":
                            {
                                // Bắt buộc phải có thay đổi
                                if (examDto.name == existExam.ExamName &&
                                    examDto.attached_file == existExam.AttachedFile &&
                                    examDto.description == existExam.Description)
                                {
                                    return new BaseResponseId
                                    {
                                        status = 400,
                                        message = "Không hợp lệ",
                                        errors = new List<ErrorDetail>
                                        {
                                            new()
                                            {
                                                message = "Bạn cần phải chỉnh sửa thông tin đề để chuyển trạng thái"
                                            }
                                        }
                                    };
                                }

                                existExam.Status = examDto.status;
                                existExam.Comment = examDto.comment;
                                break;
                            }
                            default:
                                return new BaseResponseId
                                {
                                    status = 400,
                                    message = "Không hợp lệ",
                                    errors = new List<ErrorDetail>
                                    {
                                        new()
                                        {
                                            field = "status",
                                            message = "Không thể thay đổi trạng thái"
                                        }
                                    }
                                };
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

                #region Update Exam Set status

                // Lấy Exam Set cùng với Exam
                var examSetById = await _context.ExamSets.Include(ex => ex.Exams)
                    .FirstOrDefaultAsync(es => es.ExamSetId == existExam.ExamSetId);

                if (examSetById != null)
                {
                    // Lấy danh sách tất cả các Exam trong ExamSet
                    var examsByExamSet = examSetById.Exams;
                    var isEnough = examSetById.ExamQuantity == examsByExamSet.Count;

                    if (isEnough)
                    {
                        // Bỏ qua các exams đã "approved" và kiểm tra các trạng thái còn lại
                        var examsToCheck = examsByExamSet.Where(exam => exam.Status != "approved").ToList();

                        // Kiểm tra nếu tất cả các exam trong examsToCheck có cùng trạng thái
                        var allExamsInProgress = examsToCheck.All(exam => exam.Status == "in_progress");
                        var allExamsPendingApproval = examsToCheck.All(exam => exam.Status == "pending_approval");
                        var allExamsRejected = examsToCheck.All(exam => exam.Status == "rejected");

                        if (allExamsInProgress)
                        {
                            examSetById.Status = "in_progress";
                        }
                        else if (allExamsPendingApproval)
                        {
                            examSetById.Status = "pending_approval";
                        }
                        else if (allExamsRejected && examsToCheck.Count == examSetById.ExamQuantity)
                        {
                            examSetById.Status = "rejected";
                        }
                        else if (examsByExamSet.All(exam => exam.Status == "approved"))
                        {
                            examSetById.Status = "approved";
                        }
                        else
                        {
                            // Nếu trạng thái bị pha trộn hoặc không đủ số lượng, chuyển exam_set về "in_progress"
                            examSetById.Status = "in_progress";
                        }
                    }
                    else
                    {
                        // Nếu chưa đủ số lượng, mặc định chuyển trạng thái exam_set về "in_progress"
                        examSetById.Status = "in_progress";
                    }

                    // Cập nhật exam_set
                    _context.ExamSets.Update(examSetById);
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
}