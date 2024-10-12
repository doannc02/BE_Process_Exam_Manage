using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.RequestModels;
using Microsoft.EntityFrameworkCore;
using Mysqlx;
using System.Linq;

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
                var query = _context.ExamSets.AsNoTracking().AsQueryable();
                var exams = _context.Exams.AsNoTracking().AsQueryable();
                if (queryObject.exceptValues != null && queryObject.exceptValues.Any())
                {
                    query = query.Where(p => !queryObject.exceptValues.Contains(p.ExamSetId));
                }
                // Apply filters
                if (!string.IsNullOrEmpty(queryObject.search))
                {
                    query = query.Where(p => p.ExamSetName.Contains(queryObject.search));
                }

                if (userId.HasValue)
                {
                    query = query.Where(q => q.CreatorId == userId);
                }

                if (queryObject.userId.HasValue && !userId.HasValue)
                {
                    var proposalIds = await _context.TeacherProposals
                    .Where(tp => tp.UserId == (ulong)queryObject.userId.Value)
                    .Select(tp => tp.ProposalId)
                    .ToListAsync();

                    if (proposalIds.Any())
                    {
                        query = query.Where(p => p.ProposalId.HasValue && proposalIds.Contains(p.ProposalId.Value));
                    }
                }

                if ((bool)queryObject.isParamAddProposal)
                {
                    query = query.Where(e => e.ProposalId == null);

                    // Lọc các bài thi dựa trên ExamSetId
                    var examSetIds = await query.Select(e => e.ExamSetId).ToListAsync();

                    exams = exams.Where(p => p.ExamSetId.HasValue && examSetIds.Contains(p.ExamSetId.Value));
                }

                if (queryObject.proposalId.HasValue)
                {
                    query = query.Where(p => p.ProposalId == queryObject.proposalId);
                }

                // Count total elements before pagination
                var totalCount = await query.CountAsync();

                // Fetch paginated data
                var examSets = await query
                .OrderBy(p => p.ExamSetId)
                .Skip(startRow)
                .Take(queryObject.size)
                .Include(p => p.Proposal)
                .ThenInclude(tp => tp.TeacherProposals)
                .ToListAsync();

                // Fetch additional data for DTO mapping
                var departments = await _context.Departments.AsNoTracking().ToDictionaryAsync(d => d.DepartmentId);
                var teachers = await _context.Teachers.AsNoTracking().ToDictionaryAsync(t => t.Id);
                var courses = await _context.Courses.AsNoTracking().ToDictionaryAsync(c => c.CourseId);
                var majors = await _context.Majors.AsNoTracking().ToDictionaryAsync(m => m.MajorId);
                var users = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);

                // Map to DTOs
                var examSetDTOs = examSets.Select(p => new ExamSetDTO
                {
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

                    description = p.Description,
                    status = p.Status,
                    exam_quantity = p.ExamQuantity,
                    id = p.ExamSetId,

                    major = p.MajorId.HasValue && majors.TryGetValue(p.MajorId.Value, out var major) ? new CommonObject
                    {
                        id = (int)p.MajorId.Value,
                        name = major.MajorName
                    } : null,

                    name = p.ExamSetName,
                    exams = (bool)queryObject.isParamAddProposal ? exams.Where(e => e.ExamSetId == p.ExamSetId).Select(e => new ExamDTO
                    {
                        code = e.ExamCode,
                        comment = e.Comment,
                        description = e.Description,
                        attached_file = e.AttachedFile,
                        upload_date = e.UploadDate.ToString(),
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
                var courses = await _context.Courses.AsNoTracking().ToListAsync();
                var departments = await _context.Departments.AsNoTracking().ToListAsync();
                // var users = await _context.Users.AsNoTracking().ToListAsync();
                var users = await _context.Users.AsNoTracking().ToDictionaryAsync(u => u.Id);
                var majors = await _context.Majors.AsNoTracking().ToListAsync();
                var teachers = await _context.Teachers.AsNoTracking().ToDictionaryAsync(t => t.Id);
                var examSet = await _context.ExamSets
               .AsNoTracking().Include(p => p.Proposal).ThenInclude(p => p.TeacherProposals)
               .FirstOrDefaultAsync(p => p.ExamSetId == id);
                var exams = _context.Exams.Where(ex => ex.ExamSetId == examSet.ExamSetId).AsNoTracking().AsQueryable();
                // var userIds = examSet.Proposal != null ? examSet.Proposal.TeacherProposals.Select(tp => tp.UserId).ToList() : null;
                //  var userIds = users.Where(e => e. == (ulong)examSet.CreatorId);

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
                    user = examSet.CreatorId.HasValue && users.TryGetValue((ulong)examSet.CreatorId.Value, out var user) ? new
                    {
                        id = (int)user.Id,
                        name = user.Email ?? "",
                        fullname = user.TeacherId.HasValue && teachers.TryGetValue(user.TeacherId.Value, out var teacher) ? teacher.Name : ""
                    } : null,
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
                    return new BaseResponseId { status = 400, message = "Bộ đề rỗng" };
                }

                var errors = new List<ErrorDetail>();

                // Kiểm tra tên bộ đề
                if (!string.IsNullOrEmpty(examSetDTO.name) && examSetDTO.name != "string")
                {
                    bool isExistingName = await _context.ExamSets.AsNoTracking()
                        .AnyAsync(e => examSetDTO.name == e.ExamSetName);
                    if (isExistingName)
                    {
                        errors.Add(new ErrorDetail { field = "name", message = "Tên bộ đề đã tồn tại" });
                    }
                }

                // Kiểm tra trạng thái bộ đề
                if (!validStatus.Contains(examSetDTO.status))
                {
                    errors.Add(new ErrorDetail { field = "status", message = "Trạng thái bộ đề không hợp lệ" });
                }

                // Kiểm tra học phần
                var course = await _context.Courses.AsNoTracking()
                    .Include(c => c.Major).ThenInclude(m => m.Department)
                    .FirstOrDefaultAsync(c => c.CourseId == examSetDTO.course.id);

                if (course == null)
                {
                    errors.Add(new ErrorDetail { field = "course", message = "Học phần không hợp lệ" });
                }
                else if (course.Major == null)
                {
                    errors.Add(new ErrorDetail { field = "major", message = "Chuyên ngành không hợp lệ" });
                }
                else if (course.Major.Department == null)
                {
                    errors.Add(new ErrorDetail { field = "department", message = "Khoa không hợp lệ" });
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
                    //ProposalId = examSetDTO.proposal?.id > 0 ? examSetDTO.proposal.id : (int?)null,
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
                    status = 400,
                    message = "Du lieu khong hop le",
                    errors = errorList
                };

                try
                {
                    var existExamSet = await _context.ExamSets.Include(t => t.Exams).FirstOrDefaultAsync(e => e.ExamSetId == examSet.id);
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

                        existExamSet.Description = examSet.description == "string" || string.IsNullOrEmpty(examSet.description)
                            ? existExamSet.Description : examSet.description;
                        existExamSet.Status = examSet.status;
                        //existExamSet.CreatorId = userId;

                        var examList = new List<Exam>();
                        if (examSet.exams != null && examSet.exams.Any())
                        {
                            var examsListId = examSet.exams.Select(e => e.id).ToList();
                            var existingExams = await _context.Exams.Where(e => examsListId.Contains(e.ExamId)).ToListAsync();
                            var examCodeSet = new HashSet<int>();
                            var examsToRemove = existingExams.Where(e => !examsListId.Contains(e.ExamId)).ToList();
                            if (examsToRemove.Count > 0 && examsToRemove.Any())
                            {

                                foreach (var examToRemove in examsToRemove)
                                {
                                    examToRemove.ExamSetId = null;
                                }

                            }

                            foreach (var examId in examsListId)
                            {
                                if (!examCodeSet.Add((int)examId))
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
                            status = 400,
                            message = "Du lieu khong hop le",
                            errors = errorList
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
                    response.errors = errorList;
                }
            }

            return response;
        }

        public async Task<BaseResponseId> UpdateStateAsync(int examSetId, string status, string? comment = null)
        {
            try
            {
                var findExamSet = await _context.ExamSets.FindAsync(examSetId);
                if (findExamSet == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Exam set not found",
                        errors = new List<ErrorDetail> { new() { field = "exam_id", message = $"Exam set not found: {examSetId}" } }
                    };
                }

                if (!validStatus.Contains(status))
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Invalid",
                        errors = new List<ErrorDetail> { new() { field = "status", message = "Input status is invalid" } }
                    };
                }

                // Check if the input status is the same as the current status
                if (findExamSet.Status == status)
                {
                    return new BaseResponseId
                    {
                        status = 204, // No Content
                        message = "No changes made as the status is the same."
                    };
                }

                if (findExamSet.Status == "approved")
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Exam set has been approved, cannot be modified"
                    };
                }

                // Check conditions for changing the ExamSet status
                switch (findExamSet.Status)
                {
                    case "in_progress":
                        if (status == "pending_approval")
                        {
                            findExamSet.Status = status; // Update status
                            break; // Exit switch
                        }
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Invalid status",
                            errors = new List<ErrorDetail> { new() { field = "status", message = "Can only update from 'in_progress' to 'pending_approval'." } }
                        };

                    case "pending_approval":
                        if (status == "approved" || status == "rejected" || status == "in_progress")
                        {
                            findExamSet.Status = status; // Update status
                            break; // Exit switch
                        }
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Invalid status",
                            errors = new List<ErrorDetail> { new() { field = "status", message = "Can only update from 'pending_approval' to 'approved', 'rejected', or 'in_progress'." } }
                        };

                    case "rejected":
                        if (status == "in_progress" || status == "pending_approval")
                        {
                            findExamSet.Status = status; // Update status
                            break; // Exit switch
                        }
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Invalid status",
                            errors = new List<ErrorDetail> { new() { field = "status", message = "Can only update from 'rejected' to 'in_progress' or 'pending_approval'." } }
                        };

                    default:
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Invalid status",
                            errors = new List<ErrorDetail> { new() { field = "status", message = "Unexpected status." } }
                        };
                }

                // Update ExamSet status
                var listExam = await _context.Exams.Where(e => e.ExamSetId == findExamSet.ExamSetId).ToListAsync();
                var errorList = new List<ErrorDetail>();

                if (!listExam.Any())
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Invalid exam list",
                        errors = new List<ErrorDetail> { new() { field = "exam_set.exams", message = "No exams found" } }
                    };
                }

                for (int i = 0; i < listExam.Count; i++)
                {
                    if (listExam[i].Status != "approved")
                    {
                        // Check and update status of each Exam
                        if ((status == "approved" || status == "rejected") && listExam[i].Status != "pending_approval")
                        {
                            errorList.Add(new ErrorDetail
                            {
                                field = $"exam_set.exams.{i}",
                                message = "Invalid status"
                            });
                        }
                        else
                        {
                            listExam[i].Status = status; // Update status
                        }
                    }
                }

                // If there are errors in updating the Exams
                if (errorList.Any())
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "An error occurred",
                        errors = errorList
                    };
                }

                // Save changes to the database
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Update successful",
                    data = new DetailResponse { id = findExamSet.ExamSetId }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = "An error occurred: " + ex.Message,
                    errors = new List<ErrorDetail> { new() { field = "exception", message = ex.InnerException?.Message ?? ex.Message } }
                };
            }
        }
        public Task<BaseResponse<string>> DeleteExamSetAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
