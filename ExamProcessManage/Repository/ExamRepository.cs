using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.RequestModels;
using ExamProcessManage.Utils;
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
                    "upload_date" => baseQuery.OrderBy(e => e.CreateAt),
                    "upload_date_desc" => baseQuery.OrderByDescending(e => e.CreateAt),
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
                  name= p.ExamSet.ExamSetName
              } : null,
              user = p.CreatorId.HasValue && users.ContainsKey((ulong)p.CreatorId.Value) ? new
              {
                  id = (int)users[(ulong)p.CreatorId.Value].Id,
                  name = users[(ulong)p.CreatorId.Value].Email ?? "",
                  fullname = users[(ulong)p.CreatorId.Value].TeacherId.HasValue && teachers.ContainsKey(users[(ulong)p.CreatorId.Value].TeacherId.Value) ? teachers[users[(ulong)p.CreatorId.Value].TeacherId.Value].Name : ""
              } : null,
              upload_date = p.CreateAt.ToString(),
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
                var exam = await _context.Exams.FindAsync(examId);
                if (exam == null)
                {
                    return new BaseResponse<ExamDTO>
                    {
                        message = "Không tìm thấy bài thi.",
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
                        id = (int)exam.ExamSetId
                    } : null,
                    user = exam.CreatorId.HasValue && users.TryGetValue((ulong)exam.CreatorId.Value, out var user) ? new
                    {
                        id = (int)user.Id,
                        name = user.Email ?? "",
                        fullname = user.TeacherId.HasValue && teachers.TryGetValue(user.TeacherId.Value, out var teacher) ? teacher.Name : ""
                    } : null,
                    status = exam.Status,
                    upload_date = exam.CreateAt.ToString(),
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
                    message = "Thành công",
                    data = examDto
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<ExamDTO>
                {
                    message = "Có lỗi xảy ra: " + ex.Message,
                };
            }
        }

        public async Task<BaseResponse<List<DetailResponse>>> CreateExamsAsync(List<ExamDTO> exams, int userId)
        {
            try
            {
                if (exams == null || !exams.Any())
                {
                    return new BaseResponse<List<DetailResponse>>
                    {
                        status = 400,
                        message = "Danh sách bài thi rỗng"
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
                    var examHasErrors = false;

                    // Validate code
                    if (IsInvalidString(examDTO.code))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.code",
                            message = $"Exam with code '{examDTO.code}' invalid."
                        });
                        examHasErrors = true;
                    }
                    else if (existingCodes.Contains(examDTO.code))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.code",
                            message = $"Exam with code '{examDTO.code}' already exists."
                        });
                        examHasErrors = true;
                    }

                    // Validate name
                    if (IsInvalidString(examDTO.name))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.name",
                            message = $"Exam with name '{examDTO.name}' invalid."
                        });
                        examHasErrors = true;
                    }
                    else if (existingNames.Contains(examDTO.name))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.name",
                            message = $"Exam with name '{examDTO.name}' already exists."
                        });
                        examHasErrors = true;
                    }

                    // Validate attached file
                    if (IsInvalidString(examDTO.attached_file))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.attached_file",
                            message = $"Exam with attached_file '{examDTO.attached_file}' invalid."
                        });
                        examHasErrors = true;
                    }
                    else if (existingFiles.Contains(examDTO.attached_file))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.attached_file",
                            message = $"Exam with file '{examDTO.attached_file}' already exists."
                        });
                        examHasErrors = true;
                    }

                    // Validate status
                    if (IsInvalidString(examDTO.status) || !validStatus.Contains(examDTO.status))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.status",
                            message = $"Exam with status '{examDTO.status}' is invalid."
                        });
                        examHasErrors = true;
                    }

                    // Validate exam set
                    //if (!examSetIds.Contains(examDTO.exam_set.id))
                    //{
                    //    errors.Add(new ErrorCodes
                    //    {
                    //        code = $"exams.{i}.exam_set.id",
                    //        message = $"ExamSet with id '{examDTO.exam_set.id}' does not exist."
                    //    });
                    //    examHasErrors = true;
                    //}

                    // Validate academic year
                    if (!academicYearIds.Contains(examDTO.academic_year.id))
                    {
                        errors.Add(new()
                        {
                            field = $"exams.{i}.academic_year.id",
                            message = $"AcademicYear with id '{examDTO.academic_year.id}' does not exist."
                        });
                        examHasErrors = true;
                    }

                    // If no errors, add exam to the list
                    if (!examHasErrors)
                    {
                        listExam.Add(new Exam
                        {
                            ExamCode = examDTO.code,
                            ExamName = examDTO.name,
                            ExamSetId = examDTO.exam_set?.id,
                            AcademicYearId = examDTO.academic_year?.id,
                            AttachedFile = examDTO.attached_file,
                            //Comment = examDTO.comment == "string" ? string.Empty : examDTO.comment,
                            Description = examDTO.description == "string" ? string.Empty : examDTO.description,
                            CreateAt = DateTimeFormat.ConvertToDateOnly(examDTO.upload_date),
                            Status = examDTO.status,
                            CreatorId = userId
                        });
                    }
                }

                // If no exams were successfully added, return the errors
                if (listExam.Count == 0)
                {
                    return new BaseResponse<List<DetailResponse>>
                    {
                        message = "Lỗi thêm bài thi",
                        errors = errors
                    };
                }

                // Save valid exams to the database
                await _context.AddRangeAsync(listExam);
                await _context.SaveChangesAsync();

                return new BaseResponse<List<DetailResponse>>
                {
                    message = "Thêm thành công",
                    data = listExam.Select(e => new DetailResponse { id = e.ExamId }).ToList()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<DetailResponse>>
                {
                    message = "Có lỗi xảy ra: " + ex.Message
                };
            }
        }

        private static bool IsInvalidString(string input)
        {
            return string.IsNullOrEmpty(input) || input == "string";
        }

        public async Task<BaseResponseId> UpdateExamAsync(ExamDTO examDTO, int userId)
        {
            try
            {
                var existExam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examDTO.id || e.ExamCode == examDTO.code);

                if (existExam.CreatorId != userId)
                {

                    return new BaseResponseId
                    {
                        message = "Không có quyền xóa đề này",
                    };
                }


                if (existExam == null)
                {
                    return GenerateErrorResponse("code", $"Không tìm thấy bài thi {examDTO.code}", "Không tìm thấy bài thi");
                }

                if (existExam.Status == "approved")
                {
                    return new BaseResponseId
                    {
                        message = "Bài thi đã phê duyệt, không được sửa",
                    };
                }

                if (!validStatus.Contains(examDTO.status))
                {
                    return GenerateErrorResponse("status", "status không hợp lệ: " + examDTO.status, "Status không hợp lệ");
                }

                if (examDTO.academic_year != null && examDTO.academic_year.id <= 0 ||
                    !await _context.AcademicYears.AnyAsync(a => a.AcademicYearId == examDTO.academic_year.id))
                {
                    return GenerateErrorResponse("academic_year", "academicYear khong hop le " + examDTO.academic_year.id, "AcademicYear khong hop le");
                }

                //if (examDTO.exam_set != null && examDTO.exam_set.id <= 0 ||
                //    !await _context.ExamSets.AnyAsync(e => e.ExamSetId == examDTO.exam_set.id))
                //{
                //    return GenerateErrorResponse("exam_set_id", "examSetId khong hop le " + examDTO.exam_set.id, "ExamSet khong hop le");
                //}

                existExam.ExamName = examDTO.name != "string" && examDTO.name != existExam.ExamName ?
                    examDTO.name : existExam.ExamName;
                existExam.AttachedFile = examDTO.attached_file != "string" && examDTO.attached_file != existExam.AttachedFile ?
                    examDTO.attached_file : existExam.AttachedFile;
                existExam.Description = examDTO.description != "string" && examDTO.description != existExam.Description ?
                    examDTO.description : existExam.Description;
                existExam.Comment = examDTO.comment != "string" && examDTO.comment != existExam.Comment ?
                    examDTO.comment : existExam.Comment;
                existExam.Status = examDTO.status != "string" && examDTO.status != existExam.Status ?
                    examDTO.status : existExam.Status;
                existExam.ExamSetId = examDTO.exam_set?.id;
                existExam.AcademicYearId = examDTO.academic_year?.id;

                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    message = "Sửa thành công",
                    data = new DetailResponse
                    {
                        id = existExam.ExamId
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    message = "Có lỗi xảy ra: " + ex.Message
                };
            }
        }

        public async Task<BaseResponseId> UpdateStateAsync(int examId, string status, string? comment = null)
        {
            try
            {
                var findExam = await _context.Exams.FindAsync(examId);
                if (findExam == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy bài thi",
                        errors = new List<ErrorDetail> { new() { field = "exam_id", message = $"Không tìm thấy bài thi {examId}" } }
                    };
                }
                else
                {
                    if (!validStatus.Contains(status))
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Không hợp lệ",
                            errors = new List<ErrorDetail> { new() { field = "status", message = "status nhập vào không hợp lệ" } }
                        };
                    }

                    if (findExam.Status == "approved")
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Đề thi đã được duyệt, không được phép sửa"
                        };
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        // Xử lý logic thay đổi trạng thái dựa trên trạng thái hiện tại và trạng thái mới
                        switch (status)
                        {
                            case "approved":
                                if (findExam.Status == "pending_approval")
                                {
                                    findExam.Status = status;
                                }
                                else
                                {
                                    return new BaseResponseId
                                    {
                                        status = 400,
                                        message = "Trạng thái không hợp lệ"
                                    };
                                }
                                break;

                            case "rejected":
                                if (findExam.Status == "pending_approval")
                                {
                                    findExam.Status = status;
                                }
                                else
                                {
                                    return new BaseResponseId
                                    {
                                        status = 400,
                                        message = "Trạng thái không hợp lệ"
                                    };
                                }
                                break;

                            default:
                                findExam.Status = status; // Các trạng thái khác được phép thay đổi trực tiếp
                                break;
                        }
                    }

                    if (!string.IsNullOrEmpty(comment) && comment != "string")
                    {
                        findExam.Comment = comment;
                    }

                    await _context.SaveChangesAsync();

                    return new BaseResponseId
                    {
                        data = new DetailResponse { id = findExam.ExamId },
                        message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = "Có lỗi xảy ra: " + ex.Message,
                    errors = new List<ErrorDetail> { new() { field = "exception", message = ex.InnerException?.Message ?? ex.Message } }
                };
            }
        }

        private static BaseResponseId GenerateErrorResponse(string code, string errorMessage, string userMessage, string? extraCode = null, string? extraMessage = null)
        {
            var errorList = new List<ErrorDetail> { new() { field = code, message = errorMessage } };

            if (extraCode != null && extraMessage != null)
            {
                errorList.Add(new() { field = extraCode, message = extraMessage });
            }

            return new BaseResponseId
            {
                message = userMessage,
                errors = errorList
            };
        }

        public async Task<BaseResponseId> RemoveChildAsync(int examSetId, int examId, string? comment)
        {
            try
            {
                var findExam = await _context.Exams.FindAsync(examSetId);

                if (findExam != null)
                {
                    findExam.ExamSetId = null;
                    findExam.Comment = comment ?? findExam.Comment;
                }
                else
                {
                    return new BaseResponseId
                    {
                        message = "Thất bại",
                        errors = new List<ErrorDetail>
                            {
                                new()
                                {
                                    field = $"exam.exam_id",
                                    message = "Không tìm thấy bài thi"
                                }
                            }
                    };
                }

                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    message = "Thành công",
                    data = new DetailResponse
                    {
                        id = examId,
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    message = "Có lỗi xảy ra: " + ex.Message,
                };
            }
        }

        public async Task<BaseResponse<string>> DeleteExamAsync(int examId)
        {
            try
            {
                // Tìm bài thi dựa vào examId
                var existExam = await _context.Exams.FindAsync(examId);

                if (existExam == null)
                {
                    // Trả về lỗi nếu không tìm thấy bài thi
                    return new BaseResponse<string>
                    {
                        message = $"Không tìm thấy bài thi với ID {examId}",
                        errors = new List<ErrorDetail> { new() {
                            field = "exam_id",
                            message = $"Exam with ID {examId} not found." } }
                    };
                }

                // Xóa bài thi
                _context.Exams.Remove(existExam);
                await _context.SaveChangesAsync();

                // Trả về thành công sau khi xóa
                return new BaseResponse<string>
                {
                    message = "Xóa thành công",
                    data = $"Bài thi với ID {examId} đã được xóa."
                };
            }
            catch (Exception ex)
            {
                // Xử lý lỗi xảy ra
                return new BaseResponse<string>
                {
                    message = "Có lỗi xảy ra: " + ex.Message
                };
            }
        }
    }
}
