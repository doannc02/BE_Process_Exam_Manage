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
        public ExamRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ExamDTO>> GetListExamAsync(ExamRequestParams queryObject)
        {
            try
            {
                var startRow = (queryObject.page.Value - 1) * queryObject.size;
                var query = _context.Exams.AsNoTracking().AsQueryable();
                var academic_years = _context.AcademicYears.AsNoTracking().ToList();

                if (!string.IsNullOrEmpty(queryObject.search))
                {
                    query = query.Where(p => p != null && p.ExamName.Contains(queryObject.search));
                    query = query.Where(p => p != null && p.ExamCode.Contains(queryObject.search));
                }

                if (queryObject.exam_set_id != null)
                {
                    query = query.Where(p => queryObject.exam_set_id == (p.ExamSetId));
                }

                var totalCount = query.Count();

                var exams = await query.OrderBy(p => p.ExamId).Skip(startRow).Take(queryObject.size).Select(p => new ExamDTO
                {
                    comment = p.Comment,
                    attached_file = p.AttachedFile,
                    description = p.Description,
                    code = p.ExamCode,
                    id = p.ExamId,
                    name = p.ExamName,
                    status = p.Status,
                    upload_date = p.UploadDate,
                    academic_year = new CommonObject
                    {
                        id = academic_years.Find(a => a.AcademicYearId == p.AcademicYearId).AcademicYearId,
                        name = academic_years.Find(a => a.AcademicYearId == p.AcademicYearId).YearName
                    }
                }).ToListAsync();

                var pageResponse = new PageResponse<ExamDTO>
                {
                    totalElements = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                    size = queryObject.size,
                    page = queryObject.page.Value,
                    content = exams.ToArray(),
                };

                return pageResponse;
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return null;
            }
        }

        public async Task<BaseResponse<ExamDTO>> GetDetailExamAsync(int examId)
        {
            try
            {
                var p = await _context.Exams.FindAsync(examId);
                if (p == null) return null;
                var academic_years = _context.AcademicYears.AsNoTracking().ToList();
                var examDto = new ExamDTO
                {
                    comment = p.Comment,
                    attached_file = p.AttachedFile,
                    description = p.Description,
                    code = p.ExamCode,
                    id = p.ExamId,
                    name = p.ExamName,
                    status = p.Status,
                    upload_date = p.UploadDate,
                    academic_year = new CommonObject
                    {
                        id = academic_years.Find(a => a.AcademicYearId == p.AcademicYearId).AcademicYearId,
                        name = academic_years.Find(a => a.AcademicYearId == p.AcademicYearId).YearName
                    }
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
                    message = "Thành công",
                    data = null
                };
            }
        }

        public async Task<BaseResponse<List<DetailResponse>>> CreateExamsAsync(List<ExamDTO> exams)
        {
            try
            {
                if (exams == null || !exams.Any())
                {
                    return new BaseResponse<List<DetailResponse>>
                    {
                        message = "Danh sách bài thi rỗng"
                    };
                }

                var listExam = new List<Exam>();
                var errors = new List<ErrorCodes>();

                for (int i = 0; i < exams.Count; i++)
                {
                    if (exams[i].code == "string" || string.IsNullOrEmpty(exams[i].code))
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = $"exams.{i}.code",
                            message = $"Exam with code '{exams[i].code}' invalid."
                        });
                        continue;
                    }

                    if (exams[i].name == "string" || string.IsNullOrEmpty(exams[i].name))
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = $"exams.{i}.name",
                            message = $"Exam with name '{exams[i].name}' invalid."
                        });
                        continue;
                    }

                    if (exams[i].attached_file == "string" || string.IsNullOrEmpty(exams[i].attached_file))
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = $"exams.{i}.attached_file",
                            message = $"Exam with attached_file '{exams[i].attached_file}' invalid."
                        });
                        continue;
                    }

                    var isDuplicateCode = await _context.Exams.AsNoTracking().AnyAsync(e => e.ExamCode == exams[i].code);
                    if (isDuplicateCode)
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = $"exams.{i}.code",
                            message = $"Exam with code '{exams[i].code}' already exists."
                        });
                        continue;
                    }

                    var isDuplicateName = await _context.Exams.AsNoTracking().AnyAsync(e => e.ExamName == exams[i].name);
                    if (isDuplicateName)
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = $"exams.{i}.name",
                            message = $"Exam with name '{exams[i].name}' already exists."
                        });
                        continue;
                    }

                    var isDuplicateFile = await _context.Exams.AsNoTracking().AnyAsync(e => e.AttachedFile == exams[i].attached_file);
                    if (isDuplicateFile)
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = $"exams.{i}.attached_file",
                            message = $"Exam with file '{exams[i].attached_file}' already exists."
                        });
                        continue;
                    }

                    listExam.Add(new Exam
                    {
                        ExamCode = exams[i].code,
                        ExamName = exams[i].name,
                        ExamSetId = exams[i].exam_set?.id,
                        AcademicYearId = exams[i].academic_year?.id,
                        AttachedFile = exams[i].attached_file,
                        Comment = exams[i].comment,
                        Description = exams[i].description,
                        UploadDate = exams[i].upload_date,
                        Status = exams[i].status,
                    });
                }

                if (listExam.Count == exams.Count)
                {
                    await _context.AddRangeAsync(listExam);
                    await _context.SaveChangesAsync();

                    return new BaseResponse<List<DetailResponse>>
                    {
                        message = "Thêm thành công",
                        data = new List<DetailResponse>(listExam.Select(e => new DetailResponse { id = e.ExamId }).ToList())
                    };
                }
                else
                {
                    return new BaseResponse<List<DetailResponse>>
                    {
                        message = "Lỗi thêm bài thi",
                        errs = errors
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<DetailResponse>>
                {
                    message = "Có lỗi xảy ra: " + ex.Message
                };
            }
        }

        public Task<BaseResponse<int>> UpdateExamAsync(ExamDTO examDTO)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponseId> UpdateStateAsync(int examId, string status, string? comment)
        {
            try
            {
                var validStatuses = new List<string> { "in_progress", "rejected", "approved", "pending_approval" };
                var findExam = await _context.Exams.FindAsync(examId);

                if (findExam == null)
                {
                    return new BaseResponseId
                    {
                        message = $"Không tìm thấy bài thi",
                        errs = new List<ErrorCodes>
                        {
                            new()
                            {
                                code = "exam_id",
                                message = $"Không tìm thấy bài thi {examId}"
                            }
                        }
                    };
                }

                if (!validStatuses.Contains(status))
                {
                    return new BaseResponseId
                    {
                        message = $"Không hợp lệ",
                        errs = new List<ErrorCodes>
                        {
                            new()
                            {
                                code = "status",
                                message = $"status nhập vào không hợp lệ"
                            }
                        }
                    };
                }

                if (findExam.Status == status && findExam.Comment == comment)
                {
                    return new BaseResponseId
                    {
                        message = $"Không có thay đổi",
                        errs = new List<ErrorCodes>
                        {
                            new()
                            {
                                code = "status",
                                message = "status không thay đổi"
                            },
                            new()
                            {
                                code = "comment",
                                message = "comment không thay đổi"
                            }
                        }
                    };
                }

                findExam.Status = status;
                findExam.Comment = comment;
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    data = new DetailResponse { id = findExam.ExamId },
                    message = "Thành công",
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

        public async Task<BaseResponseId> RemoveChildAsync(int examSetId, int examId, string? comment)
        {
            try
            {
                var findExam = await _context.Exams.FindAsync(examId);

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
                        errs = new List<ErrorCodes>
                            {
                                new()
                                {
                                    code = $"exam.exam_id",
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

        public Task<BaseResponse<string>> DeleteExamAsync(int examId)
        {
            throw new NotImplementedException();
        }
    }
}
