using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.RequestModels;
using Microsoft.EntityFrameworkCore;
using Mysqlx;

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
                var departments = await _context.Departments.AsNoTracking().ToListAsync();
                var teachers = await _context.Teachers.AsNoTracking().ToListAsync();
                var courses = await _context.Courses.AsNoTracking().ToListAsync();
                var majors = await _context.Majors.AsNoTracking().ToListAsync();
                var startRow = (queryObject.page.Value - 1) * queryObject.size;
                var query = _context.ExamSets.AsNoTracking().AsQueryable();
                var users = await _context.Users.AsNoTracking().ToListAsync();
                if (!string.IsNullOrEmpty(queryObject.search))
                {
                    query = query.Where(p => p.ExamSetName.Contains(queryObject.search));
                }
                if (userId.HasValue)
                {
                    var proposalIds = await _context.TeacherProposals
                        .Where(tp => tp.UserId == (ulong)userId.Value)
                        .Select(tp => tp.ProposalId)
                        .ToListAsync();

                    if (proposalIds.Any())
                    {
                        query = query.Where(p => proposalIds.Contains(p.ProposalId));
                    }
                }

                if (queryObject.userId.HasValue && !userId.HasValue)
                {
                    var proposalIds = await _context.TeacherProposals
                        .Where(tp => tp.UserId == (ulong)queryObject.userId.Value)
                        .Select(tp => tp.ProposalId)
                        .ToListAsync();
                    if (proposalIds.Any())
                    {
                        query = query.Where(p => proposalIds.Contains(p.ProposalId));
                    }
                }

                if (queryObject.proposalId != null)
                {
                    query = query.Where(p => queryObject.proposalId == p.ProposalId);
                }

                var totalCount = query.Count();

                // Chuyển sang danh sách để xử lý bên client
                var examSet = await query
                    .OrderBy(p => p.ExamSetId)
                    .Skip(startRow)
                    .Take(queryObject.size)
                    .Include(p => p.Proposal)
                    .ThenInclude(tp => tp.TeacherProposals)
                    .ToListAsync();

                // Chuyển đổi sang ExamSetDTO
                var examSetDTOs = examSet.Select(p => new ExamSetDTO
                {

                    course = courses.FirstOrDefault(m => m.CourseId == p.CourseId) != null
                        ? new CommonObject
                        {
                            id = courses.FirstOrDefault(m => m.CourseId == p.CourseId).CourseId,
                            name = courses.FirstOrDefault(m => m.CourseId == p.CourseId).CourseName,
                            code = courses.FirstOrDefault(m => m.CourseId == p.CourseId).CourseCode
                        }
                        : null,
                    department = departments.Where(m => m.DepartmentId == p.DepartmentId).Select(m => new CommonObject
                    {
                        id = m.DepartmentId,
                        name = m.DepartmentName
                    }).FirstOrDefault(),
                    description = p.Description,
                    status = p.Status,
                    exam_quantity = p.ExamQuantity,
                    id = p.ExamSetId,
                    major = majors.FirstOrDefault(m => m.MajorId == p.MajorId) != null
                        ? new CommonObject
                        {
                            id = (int)p.MajorId,
                            name = majors.FirstOrDefault(m => m.MajorId == p.MajorId).MajorName
                        }
                        : null,
                    name = p.ExamSetName,
                    user = p.Proposal.TeacherProposals.FirstOrDefault() != null ? p.Proposal.TeacherProposals.Select(tp => new
                    {
                        id = (int)tp.UserId,
                        name = users.Where(u => u.Id == (ulong)tp.UserId).FirstOrDefault().Email ?? "",
                        fullname = teachers.Where(u => u.Id == users.Where(u => u.Id == (ulong)tp.UserId).FirstOrDefault().TeacherId).FirstOrDefault().Name ?? ""
                    }).FirstOrDefault() : null,
                }).ToList();

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
                var courses = await _context.Courses.AsNoTracking().ToListAsync();
                var departments = await _context.Departments.AsNoTracking().ToListAsync();
                var users = await _context.Users.AsNoTracking().ToListAsync();
                var majors = await _context.Majors.AsNoTracking().ToListAsync();
                var teachers = await _context.Teachers.AsNoTracking().ToListAsync();
                var examSet = await _context.ExamSets
               .AsNoTracking().Include(p => p.Proposal).ThenInclude(p => p.TeacherProposals)
               .FirstOrDefaultAsync(p => p.ExamSetId == id);
                var exams = _context.Exams.AsNoTracking().AsQueryable();
                var userIds = examSet.Proposal != null ? examSet.Proposal.TeacherProposals.Select(tp => tp.UserId).ToList() : null;


                //if (userId != null)
                //{
                //    if (userId != user.id)
                //    {
                //        return new BaseResponse<ExamSetDTO>
                //        {
                //            message = "403",
                //            data = null
                //        }; ;
                //    }
                //}
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
                    course = courses.Where(m => m.CourseId == examSet.CourseId).Select(m => new CommonObject
                    {
                        id = m.CourseId,
                        name = m.CourseName,
                        code = m.CourseCode
                    }).FirstOrDefault(),
                    user = userIds == null ? null : examSet.Proposal.TeacherProposals.Where(tp => userIds.Contains(tp.UserId))
                    .Select(tp => new
                    {
                        id = (int)tp.UserId,
                        name = users.Where(u => u.Id == (ulong)tp.UserId).FirstOrDefault().Email ?? "",
                        fullname = teachers.Where(u => u.Id == users.Where(u => u.Id == (ulong)tp.UserId).FirstOrDefault().TeacherId).FirstOrDefault().Name ?? ""
                    }).FirstOrDefault(),
                    department = departments.Where(m => m.DepartmentId == examSet.DepartmentId).Select(m => new CommonObject
                    {
                        id = m.DepartmentId,
                        name = m.DepartmentName
                    }).FirstOrDefault(),
                    description = examSet.Description,
                    id = examSet.ExamSetId,
                    name = examSet.ExamSetName,
                    exam_quantity = examSet.ExamQuantity,
                    status = examSet.Status,
                    exams = exams.Where(c => c.ExamSetId == examSet.ExamSetId).Select(e => new ExamDTO
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
                        upload_date = e.UploadDate.ToString()
                    }).ToList(),
                    major = majors.Where(m => m.MajorId == examSet.MajorId).Select(m => new CommonObject
                    {
                        id = m.MajorId,
                        name = m.MajorName
                    }).FirstOrDefault(),
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
                {
                    return new BaseResponseId { errorCode = 400, message = "Bộ đề rỗng" };
                }

                var errors = new List<ErrorDetail>();

                // Kiểm tra tên bộ đề
                if (!string.IsNullOrEmpty(examSetDTO.name) && examSetDTO.name != "string")
                {
                    bool isExistingName = await _context.ExamSets.AsNoTracking()
                        .AnyAsync(e => examSetDTO.name == e.ExamSetName);
                    if (isExistingName)
                    {
                        errors.Add(new ErrorDetail { field = "exam_set.name", message = "Tên bộ đề đã tồn tại" });
                    }
                }

                // Kiểm tra trạng thái bộ đề
                if (!validStatus.Contains(examSetDTO.status))
                {
                    errors.Add(new ErrorDetail { field = "exam_set.status", message = "Trạng thái bộ đề không hợp lệ" });
                }

                // Kiểm tra học phần
                var course = await _context.Courses.AsNoTracking()
                    .Include(c => c.Major).ThenInclude(m => m.Department)
                    .FirstOrDefaultAsync(c => c.CourseId == examSetDTO.course.id);

                if (course == null)
                {
                    errors.Add(new ErrorDetail { field = "exam_set.course", message = "Học phần không hợp lệ" });
                }
                else if (course.Major == null)
                {
                    errors.Add(new ErrorDetail { field = "exam_set.major", message = "Chuyên ngành không hợp lệ" });
                }
                else if (course.Major.Department == null)
                {
                    errors.Add(new ErrorDetail { field = "exam_set.department", message = "Khoa không hợp lệ" });
                }

                // Kiểm tra đề xuất
                if (examSetDTO.proposal != null && examSetDTO.proposal.id > 0)
                {
                    bool isExistingProposal = await _context.Proposals.AsNoTracking()
                        .AnyAsync(p => p.ProposalId == examSetDTO.proposal.id || p.PlanCode == examSetDTO.proposal.code);
                    if (!isExistingProposal)
                    {
                        errors.Add(new ErrorDetail { field = "exam_set.proposal", message = "Không tìm thấy Đề xuất" });
                    }
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
                        if (!examCodeSet.Add(examId))
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
                        errorCode = 400,
                        message = "Validation Failed",
                        errs = errors
                    };
                }

                // Tạo bộ đề mới
                var newExamSet = new ExamSet
                {
                    ExamSetName = examSetDTO.name,
                    DepartmentId = course?.Major?.Department?.DepartmentId,
                    MajorId = course?.Major?.MajorId,
                    ExamQuantity = examSetDTO.exams.Count(),
                    CreatorId = userId,
                    Description = examSetDTO.description ?? string.Empty,
                    Status = examSetDTO.status,
                    CourseId = course?.CourseId,
                    ProposalId = examSetDTO.proposal?.id > 0 ? examSetDTO.proposal.id : (int?)null,
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
                    errorCode = 500,
                    message = "Có lỗi xảy ra: " + ex.Message
                };
            }
        }

        public async Task<BaseResponseId> UpdateExamSetAsync(int userId, ExamSetDTO examSet)
        {
            var response = new BaseResponseId();
            var errorList = new List<ErrorDetail>();

            if (examSet == null) errorList.Add(new() { message = "Bo de rong" });
            else
            {
                if (examSet.id <= 0) errorList.Add(new()
                {
                    field = "exam_set.id",
                    message = "Ma bo de da nhap khong hop le"
                });
                if (!validStatus.Contains(examSet.status)) errorList.Add(new()
                {
                    field = "exam_set.major.id",
                    message = "Trang thai bo de da nhap khong hop le"
                });
                if (examSet.course.id <= 0) errorList.Add(new()
                {
                    field = "exam_set.course.id",
                    message = "Ma hoc phan da nhap khong hop le"
                });
                if (userId <= 0) errorList.Add(new()
                {
                    field = "exam_set.user",
                    message = "Nguoi dung khong hop le"
                });

                var examIds = examSet.exams?.ToList();
                if (examIds?.Count > 0)
                {
                    for (int i = 0; i < examIds.Count; i++)
                    {
                        if (examIds[i].id <= 0) errorList.Add(new()
                        {
                            field = $"exam_set.exams.{i}",
                            message = "Ma de thi khong hop le"
                        });
                    }
                }

                if (errorList.Any()) return new BaseResponseId
                {
                    errorCode = 400,
                    message = "Du lieu khong hop le",
                    errs = errorList
                };

                try
                {
                    var existExamSet = await _context.ExamSets.FirstOrDefaultAsync(e => e.ExamSetId == examSet.id);
                    if (existExamSet == null)
                    {
                        errorList.Add(new()
                        {
                            message = $"Khong tim thay bo de voi ma : {examSet.id}"
                        });
                    }
                    else
                    {
                        if (examSet.department != null && examSet.department.id > 0)
                        {
                            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == examSet.department.id)) errorList.Add(new()
                            {
                                field = "exam_set.department",
                                message = $"Khong tim thay khoa: {examSet.department.id}"
                            });
                            else existExamSet.DepartmentId = examSet.department.id;
                        }
                        if (examSet.major != null && examSet.major.id > 0)
                        {
                            if (!await _context.Majors.AnyAsync(m => m.MajorId == examSet.major.id)) errorList.Add(new()
                            {
                                field = "exam_set.major",
                                message = $"Khong tim thay chuyen nganh: {examSet.major.id}"
                            });
                            else existExamSet.MajorId = examSet.major.id;
                        }
                        if (examSet.proposal != null && examSet.proposal.id > 0)
                        {
                            if (!await _context.Proposals.AnyAsync(p => p.ProposalId == examSet.proposal.id)) errorList.Add(new()
                            {
                                field = "exam_set.proposal",
                                message = $"Khong tim thay de xuat: {examSet.proposal.id}"
                            });
                            else existExamSet.ProposalId = examSet.proposal.id;
                        }
                        if (examSet.course != null && examSet.course.id > 0)
                        {
                            if (!await _context.Courses.AnyAsync(c => c.CourseId == examSet.course.id))
                            {
                                errorList.Add(new()
                                {
                                    field = "exam_set.proposal",
                                    message = $"Khong tim thay hoc phan: {examSet.course.id}"
                                });
                            }
                            else
                            {
                                ///////////////////////////

                                existExamSet.CourseId = examSet.course.id;
                            }
                        }

                        existExamSet.ExamSetName = examSet.name == "string" || string.IsNullOrEmpty(examSet.name) ? existExamSet.ExamSetName : examSet.name;
                        existExamSet.ExamQuantity = examIds != null ? examIds.Count : existExamSet.ExamQuantity;
                        existExamSet.Description = examSet.description == "string" || string.IsNullOrEmpty(examSet.description)
                            ? existExamSet.Description : examSet.description;
                        existExamSet.Status = examSet.status;
                        existExamSet.CreatorId = userId;

                        var examList = new List<Exam>();
                        if (examSet.exams != null && examSet.exams.Any())
                        {
                            var examListId = examSet.exams.Select(e => e.id).ToList();
                            var existingExams = await _context.Exams.Where(e => examListId.Contains(e.ExamId)).ToListAsync();
                            var examCodeSet = new HashSet<int>();

                            foreach (var examId in examListId)
                            {
                                if (!examCodeSet.Add(examId))
                                {
                                    errorList.Add(new ErrorDetail
                                    {
                                        field = $"exam_set.exams.{examId}",
                                        message = $"Bài thi bị trùng lặp {examId}"
                                    });
                                }
                                else if (!existingExams.Any(e => e.ExamId == examId))
                                {
                                    errorList.Add(new ErrorDetail
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

                        if (errorList.Any()) return new BaseResponseId
                        {
                            errorCode = 400,
                            message = "Du lieu khong hop le",
                            errs = errorList
                        };

                        if (examIds != null && examList.Count == examIds.Count) existExamSet.Exams = examList;

                        response.message = "Cap nhat thanh cong";
                        response.data = new DetailResponse { id = existExamSet.ExamSetId };

                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    errorList.Add(new()
                    {
                        message = "Co loi xay ra: " + ex.Message + "\n" + ex.InnerException
                    });
                    response.errs = errorList;
                }
            }

            return response;
        }

        public async Task<BaseResponseId> UpdateStateAsync(int id, string status, string? comment = null)
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
                        errs = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "exam_id",
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
                        errs = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "status",
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
                        errs = new List<ErrorDetail>
                        {
                            new()
                            {
                                field = "status",
                                message = "status không thay đổi"
                            },
                            new()
                            {
                                field = "comment",
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
