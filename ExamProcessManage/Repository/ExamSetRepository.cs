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
        public ExamSetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ExamSetDTO>> GetListExamSetAsync(int? userId, RequestParamsExamSets queryObject)
        {
            try
            {
                var startRow = (queryObject.page.Value - 1) * queryObject.size;
                var query = _context.ExamSets.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(queryObject.search))
                {
                    query = query.Where(p => p.ExamSetName.Contains(queryObject.search));
                }
                if (userId.HasValue)
                {
                    var proposalIds = _context.TeacherProposals
                    .Where(tp => tp.UserId == (ulong)userId.Value)
                    .Select(tp => tp.ProposalId)
                    .ToList();

                    if (proposalIds.Any())
                    {
                        query = query.Where(p => proposalIds.Contains(p.ProposalId));
                    }
                }

                if (queryObject.userId.HasValue && !userId.HasValue)
                {
                    var proposalIds = _context.TeacherProposals
                    .Where(tp => tp.UserId == (ulong)queryObject.userId.Value)
                    .Select(tp => tp.ProposalId)
                    .ToList();
                    if (proposalIds.Any())
                    {
                        query = query.Where(p => proposalIds.Contains(p.ProposalId));
                    }
                }

                if (queryObject.proposalId != null)
                {
                    query = query.Where(p => queryObject.proposalId == (p.ProposalId));
                }

                var totalCount = query.Count();
                var examSet = await query.OrderBy(p => p.ExamSetId).Skip(startRow).Take(queryObject.size).Include(p => p.Proposal)
                .ThenInclude(tp => tp.TeacherProposals)
                .Select(p => new ExamSetDTO
                {
                    course = new CommonObject
                    {
                        id = (int)p.CourseId,
                        name = p.Course.CourseName,
                        code = p.Course.CourseCode
                    },
                    department = p.Department,
                    description = p.Description,
                    status = p.Status,
                    exam_quantity = p.ExamQuantity,
                    id = p.ExamSetId,
                    major = p.Major,
                    name = p.ExamSetName,
                    user = p.Proposal.TeacherProposals.Select(tp => new CommonObject
                    {
                        id = (int)tp.User.Id,
                        name = tp.User.Name + " - " + tp.User.Teacher.Name
                    }).FirstOrDefault()
                }).ToListAsync();

                var pageResponse = new PageResponse<ExamSetDTO>
                {
                    totalElements = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                    size = queryObject.size,
                    page = queryObject.page.Value,
                    content = examSet.ToArray()
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
                var examSet = await _context.ExamSets
               .AsNoTracking().Include(p => p.Proposal).ThenInclude(p => p.TeacherProposals)
               .FirstOrDefaultAsync(p => p.ExamSetId == id);
                var exams = _context.Exams.AsNoTracking().AsQueryable();
                var userIds = examSet.Proposal.TeacherProposals.Select(tp => tp.UserId).ToList();
                var user = examSet.Proposal.TeacherProposals.Where(tp => userIds.Contains(tp.UserId))
                    .Select(tp => new CommonObject
                    {
                        id = (int)tp.UserId,
                        name = tp.User?.Name ?? "" + " - " + tp.User?.Teacher?.Name ?? ""
                    }).FirstOrDefault();
                if (userId != null)
                {
                    if (userId != user.id)
                    {
                        return new BaseResponse<ExamSetDTO>
                        {
                            message = "403",
                            data = null
                        }; ;
                    }
                }
                if (examSet == null)
                {
                    return new BaseResponse<ExamSetDTO>
                    {
                        message = $"Proposal with id = {id} could not be found",
                        data = null
                    };
                }

                var examSetDTO = new ExamSetDTO
                {
                    course = new CommonObject
                    {
                        name = examSet.Course?.CourseName ?? string.Empty,
                        code = examSet.Course?.CourseCode ?? string.Empty,
                        id = examSet.Course?.CourseId ?? 0
                    },
                    user = user,
                    department = examSet.Department,
                    description = examSet.Description,
                    id = examSet.ExamSetId,
                    name = examSet.ExamSetName,
                    exam_quantity = examSet.ExamQuantity,
                    status = examSet.Status,
                    exams = exams.Where(c => c.ExamSetId == examSet.ExamSetId).Select(e => new CommonObject
                    {
                        code = e.ExamCode,
                        id = e.ExamId,
                        name = e.ExamName
                    }).ToList(),
                    major = examSet.Major,
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

        public async Task<BaseResponseId> CreateExamSetAsync(ExamSetDTO examSetDTO)
        {
            try
            {
                if (examSetDTO == null)
                {
                    return new BaseResponseId
                    {
                        errorCode = 400,
                        message = "Bộ đề rỗng"
                    };
                }

                var errors = new List<ErrorCodes>();

                // Check if ExamSet name exists
                if (!string.IsNullOrEmpty(examSetDTO.name) && examSetDTO.name != "string")
                {
                    var isExistingName = await _context.ExamSets.AsNoTracking()
                        .AnyAsync(e => examSetDTO.name == e.ExamSetName);
                    if (isExistingName)
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = "exam_set.name",
                            message = "Tên bộ đề đã tồn tại"
                        });
                    }
                }

                // Validate status
                var validStatus = new List<string> { "in_progress", "rejected", "approved", "pending_approval" };
                if (!validStatus.Contains(examSetDTO.status))
                {
                    errors.Add(new ErrorCodes
                    {
                        code = "exam_set.status",
                        message = "Trạng thái bộ đề không hợp lệ"
                    });
                }

                // Validate course
                if (examSetDTO.course == null || examSetDTO.course.id == 0)
                {
                    errors.Add(new ErrorCodes
                    {
                        code = "exam_set.course",
                        message = "Học phần không hợp lệ"
                    });
                }

                // Validate proposal
                if (examSetDTO.proposal != null && examSetDTO.proposal.id > 0)
                {
                    var isExistingProposal = await _context.Proposals.AsNoTracking()
                        .AnyAsync(p => p.ProposalId == examSetDTO.proposal.id || p.PlanCode == examSetDTO.proposal.code);

                    if (!isExistingProposal)
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = "exam_set.proposal",
                            message = "Không tìm thấy Đề xuất"
                        });
                    }
                }

                // Validate exams
                var examList = new List<Exam>();
                var examCodeSet = new HashSet<int>();

                if (examSetDTO.exams != null && examSetDTO.exams.Any())
                {
                    var exams = examSetDTO.exams.ToList();
                    for (int i = 0; i < exams.Count; i++)
                    {
                        var examId = exams[i].id;
                        if (examCodeSet.Contains(examId))
                        {
                            errors.Add(new ErrorCodes
                            {
                                code = $"exam_set.exams.{i}",
                                message = $"Bài thi bị trùng lặp {exams[i].id}"
                            });
                            continue;
                        }

                        var examByCode = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examId);
                        if (examByCode == null)
                        {
                            errors.Add(new ErrorCodes
                            {
                                code = $"exam_set.exams.{i}",
                                message = $"Không tồn tại bài thi {exams[i].id}"
                            });
                        }
                        else
                        {
                            examCodeSet.Add(examId);
                            examList.Add(examByCode);
                        }
                    }
                }

                // Return early if validation failed
                if (errors.Any())
                {
                    return new BaseResponseId
                    {
                        errorCode = 400,
                        message = "Validation Failed",
                        errs = errors
                    };
                }

                // Check course, major, and department
                var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == examSetDTO.course.id);
                if (course == null)
                {
                    errors.Add(new ErrorCodes
                    {
                        code = "exam_set.course",
                        message = "Học phần không hợp lệ"
                    });
                }
                else
                {
                    var major = await _context.Majors.AsNoTracking().FirstOrDefaultAsync(m => m.MajorId == course.MajorId);
                    if (major == null)
                    {
                        errors.Add(new ErrorCodes
                        {
                            code = "exam_set.major",
                            message = "Chuyên ngành không hợp lệ"
                        });
                    }
                    else
                    {
                        var department = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.DepartmentId == major.DepartmentId);
                        if (department == null)
                        {
                            errors.Add(new ErrorCodes
                            {
                                code = "exam_set.department",
                                message = "Khoa không hợp lệ"
                            });
                        }
                    }
                }

                // Return early if validation failed after course/major/department checks
                if (errors.Any())
                {
                    return new BaseResponseId
                    {
                        errorCode = 400,
                        message = "Validation Failed",
                        errs = errors
                    };
                }

                // Create new ExamSet
                var newExamSet = new ExamSet
                {
                    ExamSetName = examSetDTO.name,
                    Department = course?.Major?.Department?.DepartmentName ?? string.Empty,
                    Major = course?.Major?.MajorName ?? string.Empty,
                    ExamQuantity = examSetDTO.exams.Count(),
                    Description = examSetDTO.description ?? string.Empty,
                    Status = examSetDTO.status,
                    CourseId = examSetDTO.course.id,
                    ProposalId = examSetDTO.proposal?.id == 0 ? null : examSetDTO.proposal?.id,
                    Exams = examList
                };

                await _context.ExamSets.AddAsync(newExamSet);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    message = "Thêm bộ đề thành công",
                    data = new DetailResponse
                    {
                        id = newExamSet.ExamSetId
                    }
                };
            }
            catch (Exception ex)
            {
                // Handle exception and return error message
                return new BaseResponseId
                {
                    errorCode = 500,
                    message = "Có lỗi xảy ra: " + ex.Message
                };
            }
        }








        // Kiểm tra dữ liệu đầu vào
        //if (examSetDto == null || examSetDto.exams == null || !examSetDto.exams.Any())
        //{
        //    return null; // Dữ liệu không hợp lệ
        //}

        // Kiểm tra xem ExamSet đã tồn tại trong DB hay chưa
        //var ckExisExamSets = await _context.ExamSets
        //    .FirstOrDefaultAsync(es => es.ExamSetId == examSetDto.id || es.ExamSetName == examSetDto.name);

        //if (ckExisExamSets != null)
        //{
        //    var errs = new List<ErrorCodes>();
        //    // Nếu ExamSet đã tồn tại, trả về thông báo tồn tại
        //    var err = new ErrorCodes
        //    {
        //        message = "ExamSet đã tồn tại",
        //        code = $"exam_set_name"
        //    };

        //    return new BaseResponseId
        //    {
        //        message = "Exam set đã tồn tại",
        //        errs = errs
        //    };
        //}

        // Kiểm tra phần tử trùng lặp trong mảng Exams
        //for (int i = 0; i < examSetDto.exams.ToList().Count; i++)
        //{
        //    var exam = examSetDto?.exams.ToList()[i];

        //    // Kiểm tra trùng lặp dựa trên ExamCode hoặc tiêu chí khác
        //    var existingExamCode = await _context.Exams
        //        .FirstOrDefaultAsync(e => e.ExamCode == exam.code);

        //    var existingExamName = await _context.Exams
        //   .FirstOrDefaultAsync(e => e.ExamName == exam.name);

        //    if (existingExamCode != null)
        //    {
        //        var errs = new List<ErrorCodes>();
        //        // Trả về index của phần tử bị trùng
        //        var err = new ErrorCodes
        //        {
        //            message = "Exam đã tồn tại",
        //            code = $"exam_set.{i}.code"
        //        };
        //        return new BaseResponseId
        //        {
        //            errs = errs,
        //        };
        //    }

        //    if (existingExamName != null)
        //    {
        //        var errs = new List<ErrorCodes>();
        //        // Trả về index của phần tử bị trùng
        //        var err = new ErrorCodes
        //        {
        //            message = "Tên Exam đã tồn tại",
        //            code = $"exam_set.{i}.exam_name"
        //        };
        //        return new BaseResponseId
        //        {
        //            errs = errs,
        //        };
        //    }
        //}

        //// Nếu không có trùng lặp, tạo mới ExamSet
        //var newExamSet = new ExamSet
        //{
        //    ExamSetName = examSetDto.name,
        //    Description = examSetDto.description,
        //    ExamQuantity = examSetDto.exam_quantity,
        //    Major = examSetDto.major,
        //    Status = examSetDto.status,
        //    CourseId = examSetDto.course.id,
        //    ProposalId = examSetDto?.proposal?.id,
        //    Department = examSetDto.department,

        //    Exams = examSetDto.exams.Select(e => new Exam
        //    {
        //        AttachedFile = e.attached_file,
        //        Comment = e.comment,
        //        Description = e.description,
        //        ExamCode = e.code,
        //        ExamName = e.name,
        //        ExamSetId = examSetDto.id,
        //        Status = e.status,
        //        AcademicYearId = e.academic_year.id,
        //        UploadDate = DateOnly.FromDateTime(DateTime.Now),
        //    }).ToList()
        //};

        //// Thêm vào context và lưu vào database
        //_context.ExamSets.Add(newExamSet);
        //await _context.SaveChangesAsync();

        //// Trả về response thành công
        //return new BaseResponseId
        //{
        //    message = "ExamSet đã được tạo thành công",
        //    data = new DetailResponse { id = newExamSet.ExamSetId }
        //};


        public Task<BaseResponseId> UpdateExamSetAsync(ExamSetDTO examSetDTO)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponseId> UpdateStateAsync(int id, string status, string? comment)
        {
            try
            {
                var validStatuses = new List<string> { "in_progress", "rejected", "approved", "pending_approval" };
                var findExam = await _context.Exams.FindAsync(id);

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
                                message = $"Không tìm thấy bài thi {id}"
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

        public Task<BaseResponseId> RemoveChildAsync(int proposalId, int examSetId, string? comment)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<string>> DeleteExamSetAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
