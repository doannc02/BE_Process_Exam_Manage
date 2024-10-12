using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class ProposalRepository : IProposalRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly List<string> validStatus = new() { "in_progress", "rejected", "approved", "pending_approval" };

        public ProposalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ProposalDTO>> GetListProposalsAsync(int? userId, QueryObject queryObject)
        {
            try
            {
                var startRow = (queryObject.page.Value - 1) * queryObject.size;
                var query = _context.Proposals.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(queryObject.search))
                {
                    query = query.Where(p => p.PlanCode.Contains(queryObject.search));
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

                var totalCount = await query.CountAsync();
                var academic_years = _context.AcademicYears.AsNoTracking().ToList();
                var proposals = await query.OrderBy(p => p.ProposalId).Skip(startRow).Take(queryObject.size).Include(p => p.TeacherProposals)
                    .ThenInclude(tp => tp.User).ThenInclude(u => u.Teacher)
                    .Select(p => new ProposalDTO
                    {
                        id = p.ProposalId,
                        academic_year = new CommonObject
                        {
                            // id = academic_years.FirstOrDefault(a => a.YearName == p.AcademicYear).AcademicYearId ,
                            name = p.AcademicYear
                        },
                        content = p.Content,
                        end_date = p.EndDate.ToString(),
                        code = p.PlanCode,
                        semester = p.Semester,
                        start_date = p.StartDate.ToString(),
                        status = p.Status,
                        // total_exam_set = p.TeacherProposals.Count(),
                        user = p.TeacherProposals.Select(tp => new CommonObject
                        {
                            id = (int)tp.User.Id,
                            name = tp.User.Name + " - " + tp.User.Teacher.Name
                        }).FirstOrDefault()
                    }).ToListAsync();

                var pageResponse = new PageResponse<ProposalDTO>
                {
                    totalElements = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                    size = queryObject.size,
                    page = queryObject.page.Value,
                    content = proposals.ToArray()
                };

                return pageResponse;
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return null;
            }
        }

        public async Task<BaseResponse<ProposalDTO>> GetDetailProposalAsync(int id)
        {
            var courses = await _context.Courses.AsNoTracking().ToListAsync();
            var departments = await _context.Departments.AsNoTracking().ToListAsync();
            var majors = await _context.Majors.AsNoTracking().ToListAsync();
            var proposal = await _context.Proposals
            .AsNoTracking()
            .Include(p => p.ExamSets)
            .ThenInclude(es => es.Exams)
            .Include(p => p.TeacherProposals)
            .ThenInclude(tp => tp.User)
            .ThenInclude(u => u.Teacher)
            .FirstOrDefaultAsync(p => p.ProposalId == id);

            if (proposal == null)
            {
                return new BaseResponse<ProposalDTO>
                {
                    message = $"Proposal with id = {id} could not be found",
                    data = null
                };
            }

            var examSetDTOs = proposal.ExamSets.Select(es => new ExamSetDTO
            {
                //course = new CommonObject
                //{
                //    name = es.Course?.CourseName ?? string.Empty,
                //    code = es.Course?.CourseCode ?? string.Empty,
                //    id = es.Course?.CourseId ?? 0
                //},
                course = courses.FirstOrDefault(m => m.CourseId == es.CourseId) switch
                {
                    var courseObj when courseObj != null => new CommonObject
                    {
                        id = courseObj.CourseId,
                        name = courseObj.CourseName,
                        code = courseObj.CourseCode
                    },
                    _ => null // Nếu không tìm thấy
                },
                department = departments.Where(m => m.DepartmentId == es.DepartmentId).Select(m => new CommonObject
                {
                    id = m.DepartmentId,
                    name = m.DepartmentName
                }).FirstOrDefault(),
                description = es.Description,
                id = es.ExamSetId,
                name = es.ExamSetName,
                exam_quantity = es.ExamQuantity,
                status = es.Status,
                exams = es.Exams.Select(e => new ExamDTO
                {
                    code = e.ExamCode,
                    id = e.ExamId,
                    name = e.ExamName
                }).ToList(),
                major = majors.FirstOrDefault(m => m.MajorId == es.MajorId) switch
                {
                    var majorObj when majorObj != null => new CommonObject
                    {
                        id = majorObj.MajorId,
                        name = majorObj.MajorName
                    },
                    _ => null // Nếu không tìm thấy
                }
            }).ToList();

            var teacherProposal = proposal.TeacherProposals.FirstOrDefault();
            var user = teacherProposal?.User;
            var teacher = user?.Teacher;

            return new BaseResponse<ProposalDTO>
            {
                data = new ProposalDTO
                {
                    id = proposal.ProposalId,
                    academic_year = new CommonObject
                    {
                        name = proposal.AcademicYear
                    },
                    content = proposal.Content,
                    end_date = proposal.EndDate.ToString(),
                    start_date = proposal.StartDate.ToString(),
                    code = proposal.PlanCode,
                    status = proposal.Status,
                    semester = proposal.Semester,
                    user = new CommonObject
                    {
                        id = (int)(user?.Id ?? 0),
                        name = user != null && teacher != null ? $"{user.Name} - {teacher.Name}" : string.Empty
                    },
                    exam_sets = examSetDTOs.Count == 0 ? Array.Empty<ExamSetDTO>() : examSetDTOs,
                }
            };
        }

        public async Task<BaseResponseId> CreateProposalAsync(int userId, ProposalDTO proposalDTO)
        {
            try
            {
                var examSets = new List<ExamSet>();
                var errors = new List<ErrorDetail>();

                var existProposal = await _context.Proposals.FirstOrDefaultAsync(p => p.PlanCode == proposalDTO.code);
                if (existProposal != null)
                    errors.Add(new() { field = "code", message = $"A similar record already exists: {proposalDTO.code}" });

                if (string.IsNullOrEmpty(proposalDTO.code) || proposalDTO.code == "string")
                    errors.Add(new() { field = "code", message = "Invalid plan code" });

                if (string.IsNullOrEmpty(proposalDTO.semester) || proposalDTO.semester == "string")
                    errors.Add(new() { field = "semester", message = "Invalid semester" });

                if (!validStatus.Contains(proposalDTO.status))
                    errors.Add(new() { field = "status", message = "Invalid status" });

                var isAcademicYear = await _context.AcademicYears.AnyAsync(a => a.YearName == proposalDTO.academic_year.name);
                if (!isAcademicYear)
                    errors.Add(new() { field = "academic_year", message = "Invalid academic year" });

                if (!DateOnly.TryParse(proposalDTO.start_date, out var parseStart))
                    errors.Add(new() { field = "start_date", message = "Invalid start date format" });

                if (!DateOnly.TryParse(proposalDTO.end_date, out var parseEnd))
                    errors.Add(new() { field = "end_date", message = "Invalid end date format" });

                if (proposalDTO.exam_sets != null && proposalDTO.exam_sets.Any())
                {
                    var examSetIds = proposalDTO.exam_sets.Select(e => e.id).ToList();
                    var existExamSets = await _context.ExamSets.Where(e => examSetIds.Contains(e.ExamSetId)).ToDictionaryAsync(e => e.ExamSetId);
                    var examSetIdSets = new HashSet<int>();

                    foreach (var item in examSetIds)
                    {
                        if (!examSetIdSets.Add(item))
                            errors.Add(new() { field = $"exam_sets.{item}", message = $"Duplicate exam set: {item}" });
                        else if (!existExamSets.ContainsKey(item))
                            errors.Add(new() { field = $"exam_sets.{item}", message = $"Exam set not found: {item}" });
                        else
                        {
                            var examSet = existExamSets[item];
                            if (examSet.ProposalId == null)
                                examSets.Add(examSet);
                            else
                                errors.Add(new() { field = $"exam_sets.{item}", message = $"The exam set has been assigned to another proposal" });
                        }
                    }
                }

                var isExistUser = await _context.Users.AnyAsync(u => u.Id == (ulong)userId);
                if (!isExistUser) errors.Add(new() { field = "user", message = $"User does not exist: {proposalDTO.user.id}" });

                if (errors.Any())
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Validation failed",
                        errors = errors
                    };

                var newProposal = new Proposal
                {
                    PlanCode = proposalDTO.code,
                    Semester = proposalDTO.semester,
                    StartDate = parseStart,
                    EndDate = parseEnd,
                    Content = string.IsNullOrEmpty(proposalDTO.content) || proposalDTO.content == "string" ? string.Empty : proposalDTO.content,
                    Status = proposalDTO.status,
                    AcademicYear = proposalDTO.academic_year.name ?? string.Empty,
                    ExamSets = examSets
                };

                await _context.Proposals.AddAsync(newProposal);
                await _context.SaveChangesAsync();

                var justProposal = await _context.Proposals.FirstOrDefaultAsync(p => p.PlanCode == newProposal.PlanCode);
                if (justProposal != null)
                {
                    var newTeacherProposal = new TeacherProposal { UserId = (ulong)userId, ProposalId = justProposal.ProposalId };
                    await _context.TeacherProposals.AddAsync(newTeacherProposal);
                    await _context.SaveChangesAsync();
                }

                return new BaseResponseId { status = 200, message = "Success", data = new DetailResponse { id = newProposal.ProposalId } };
            }
            catch (DbUpdateException dbEx)
            {
                return new BaseResponseId { status = 500, message = $"Database error: {dbEx.Message} \n {dbEx.InnerException}" };
            }
            catch (Exception ex)
            {
                return new BaseResponseId { status = 500, message = $"An error occurred: {ex.Message} \n {ex.InnerException}" };
            }
        }

        public async Task<BaseResponseId> UpdateProposalAsync(ProposalDTO proposalDTO)
        {
            try
            {
                var existingProposal = await _context.Proposals.FirstOrDefaultAsync(id => id.ProposalId == proposalDTO.id);

                if (existingProposal != null && existingProposal.Status != "approved")
                {
                    existingProposal.AcademicYear = proposalDTO.academic_year.name;
                    existingProposal.Content = proposalDTO.content;
                    existingProposal.EndDate = DateOnly.Parse(proposalDTO.end_date);
                    existingProposal.PlanCode = proposalDTO.code;
                    existingProposal.StartDate = DateOnly.Parse(proposalDTO.start_date);
                    existingProposal.Semester = proposalDTO.semester;
                    existingProposal.Status = proposalDTO.status;
                    // Update other properties as needed
                    // existingProposal.ExamSets = (ICollection<ExamSet>)proposalDTO.exam_sets;
                    // existingProposal.TeacherProposals = (ICollection<TeacherProposal>)proposalDTO.teacher_proposals;

                    _context.Proposals.Update(existingProposal);
                    await _context.SaveChangesAsync();

                    var detailResponse = new DetailResponse { id = existingProposal.ProposalId };
                    var baseResponseId = new BaseResponseId
                    {
                        message = "Cập nhật thành công",
                        data = detailResponse
                    };
                    return baseResponseId;
                }
                else if (existingProposal != null && existingProposal.Status == "approved")
                {
                    var detailResponse = new DetailResponse { id = null };
                    var baseResponseId = new BaseResponseId
                    {
                        message = "Kế hoạch đã phê duyệt không được sửa",
                        data = detailResponse
                    };
                    return baseResponseId;
                }
                else
                {
                    var detailResponse = new DetailResponse { id = null };
                    var baseResponseId = new BaseResponseId
                    {
                        message = "Không tìm thấy đề xuất",
                        data = detailResponse
                    };
                    return baseResponseId;
                }
            }
            catch (Exception ex)
            {
                var detailResponse = new DetailResponse { id = null };
                var baseResponseId = new BaseResponseId
                {
                    message = ex.Message,
                    data = detailResponse
                };
                return baseResponseId;
            }
        }

        public async Task<BaseResponseId> UpdateStateProposalAsync(int proposalId, string status, string? comment = null)
        {
            try
            {
                var findProposal = await _context.Proposals.FindAsync(proposalId);
                if (findProposal == null)
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Exam set not found",
                        errors = new List<ErrorDetail> { new() { field = "exam_id", message = $"Exam set not found: {proposalId}" } }
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
                if (findProposal.Status == status)
                {
                    return new BaseResponseId
                    {
                        status = 204, // No Content
                        message = "No changes made as the status is the same."
                    };
                }

                if (findProposal.Status == "approved")
                {
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Exam set has been approved, cannot be modified"
                    };
                }

                // Check conditions for changing the ExamSet status
                switch (findProposal.Status)
                {
                    case "in_progress":
                        if (status == "pending_approval")
                        {
                            findProposal.Status = status; // Update status
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
                            findProposal.Status = status; // Update status
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
                            findProposal.Status = status; // Update status
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
                var listExamSet = await _context.ExamSets.Where(e => e.ProposalId == findProposal.ProposalId).ToListAsync();
                var errorList = new List<ErrorDetail>();

                if (!listExamSet.Any())
                {
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Invalid exam list",
                        errors = new List<ErrorDetail> { new() { field = "exam_sets", message = "No exam set found" } }
                    };
                }

                for (int i = 0; i < listExamSet.Count; i++)
                {
                    var examSet = listExamSet[i];
                    if (examSet.Status != "approved")
                    {
                        // Only proceed if status is "approved" or "rejected" and current status is not "pending_approval"
                        if ((status == "approved" || status == "rejected") && examSet.Status != "pending_approval")
                        {
                            errorList.Add(new() { field = $"exam_set.{i}", message = "Invalid status" });
                            continue;
                        }

                        examSet.Status = status; // Update status

                        // Retrieve all exams in one query before loop
                        var listExam = await _context.Exams
                            .Where(e => e.ExamSetId == examSet.ExamSetId).ToListAsync();

                        if (!listExam.Any())
                            return new BaseResponseId
                            {
                                status = 404,
                                message = "Invalid exam list",
                                errors = new List<ErrorDetail> { new() {
                                    field = $"exam_set.{i}.exams",
                                    message = $"No exams found for exam set {examSet.ExamSetId}" } }
                            };

                        if (listExam.Count < examSet.ExamQuantity)
                            return new BaseResponseId
                            {
                                status = 400,
                                message = "Not enough exams",
                                errors = new List<ErrorDetail> { new() {
                                    field = $"exam_set.{i}.exams",
                                    message = $"Not enough exams: id = {examSet.ExamSetId} ({listExam.Count}/{examSet.ExamQuantity})" } }
                            };

                        for (int j = 0; j < listExam.Count; j++)
                        {
                            var exam = listExam[j];
                            if (exam.Status != "approved")
                                if ((status == "approved" || status == "rejected") && exam.Status != "pending_approval")
                                    errorList.Add(new() { field = $"exam_set.{i}.exams.{j}", message = "Invalid status" });
                                else
                                    exam.Status = status; // Update status
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
                    data = new DetailResponse { id = findProposal.ProposalId }
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

        public async Task<BaseResponse<string>> DeleteProposalAsync(int proposalId)
        {
            try
            {
                var proposal = await _context.Proposals.FirstOrDefaultAsync(p => p.ProposalId == proposalId);

                if (proposal != null)
                {
                    _context.Proposals.Remove(proposal);
                    await _context.SaveChangesAsync();


                    var baseResponseId = new BaseResponse<string>
                    {
                        message = "Xóa thành công",
                    };
                    return baseResponseId;
                }
                else
                {

                    var baseResponseId = new BaseResponse<string>
                    {
                        message = "Không tìm thấy đề xuất",
                    };
                    return baseResponseId;
                }
            }
            catch (Exception ex)
            {

                var baseResponseId = new BaseResponse<string>
                {
                    message = ex.Message,

                };
                return baseResponseId;
            }
        }
    }
}
