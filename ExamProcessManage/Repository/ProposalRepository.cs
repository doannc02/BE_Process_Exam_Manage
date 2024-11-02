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
        private readonly List<string> _validStatus = new() { $"in_progress", $"rejected", $"approved", $"pending_approval" };

        public ProposalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ProposalDTO>> GetListProposalsAsync(int? userId, QueryObjectProposal queryObject)
        {
            try
            {
                var startRow = (queryObject.page!.Value - 1) * queryObject.size;
                var baseQuery = _context.Proposals.AsNoTracking().AsQueryable();

                // Search by PlanCode
                if (!string.IsNullOrEmpty(queryObject.search))
                    baseQuery = baseQuery.Where(p => p.PlanCode.Contains(queryObject.search));

                // Filter by status
                if (!string.IsNullOrEmpty(queryObject.status))
                    baseQuery = baseQuery.Where(p => p.Status == queryObject.status);

                // Filter by semester
                if (queryObject.semester.HasValue && queryObject.semester > 0)
                    baseQuery = baseQuery.Where(p => p.Semester == queryObject.semester.ToString());

                // Filter by creation month (StartDate)
                if (queryObject.create_month is > 0)
                    baseQuery = baseQuery.Where(p => p.CreateAt.HasValue && p.CreateAt.Value.Month == queryObject.create_month);

                // Filter by end month (EndDate)
                if (queryObject.month_end.HasValue && queryObject.month_end > 0)
                    baseQuery = baseQuery.Where(p => p.EndDate.HasValue && p.EndDate.Value.Month == queryObject.month_end);

                if (queryObject.day_expire.HasValue && queryObject.day_expire > 0)
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var oneWeekLater = DateOnly.FromDateTime(DateTime.Today.AddDays((double)queryObject.day_expire));

                    // Filter proposals expiring within the specified days and exclude completed ones
                    baseQuery = baseQuery.Where(p =>
                        p.EndDate.HasValue &&
                        p.EndDate.Value >= today &&
                        p.EndDate.Value <= oneWeekLater &&
                        p.Status != $"approved"); // Assuming "approved" is the status for finished proposals
                }

                // Filter by userId (related to TeacherProposals)
                if (userId.HasValue)
                {
                    var proposalIds = new List<int?>();
                    foreach (var i in _context.TeacherProposals.Where(tp => tp.UserId == (ulong)userId.Value)
                                 .Select(tp => tp.ProposalId))
                        proposalIds.Add(i);

                    baseQuery = baseQuery.Where(p => proposalIds.Contains(p.ProposalId));
                }

                // Filter by queryObject.userId if not filtering by userId
                if (queryObject.userId.HasValue && !userId.HasValue)
                {
                    var proposalIds = new List<int?>();
                    foreach (var i in _context.TeacherProposals.Where(tp => tp.UserId == (ulong)queryObject.userId.Value)
                                 .Select(tp => tp.ProposalId))
                        proposalIds.Add(i);

                    if (proposalIds.Any())
                    {
                        baseQuery = baseQuery.Where(p => proposalIds.Contains(p.ProposalId));
                    }
                }

                // Get total count before paging
                var totalCount = await baseQuery.CountAsync();

                // Academic Years for the DTO (optional)
                var academicYears = await _context.AcademicYears.AsNoTracking().ToListAsync();

                // Fetch paginated proposals
                var proposals = await baseQuery
                    .OrderBy(p => p.ProposalId)
                    .Skip(startRow)
                    .Take(queryObject.size)
                    .Include(p => p.TeacherProposals)
                        .ThenInclude(tp => tp.User)
                            .ThenInclude(u => u.Teacher)
                    .Select(p => new ProposalDTO
                    {
                        id = p.ProposalId,
                        academic_year = new CommonObject
                        {
                            // If you need to include ID based on academic_years, uncomment the next line
                            // id = academic_years.FirstOrDefault(a => a.YearName == p.AcademicYear)?.AcademicYearId ?? 0,
                            name = p.AcademicYear
                        },
                        content = p.Content,
                        end_date = p.EndDate.HasValue ? p.EndDate.Value.ToString("yyyy-MM-dd") : null,
                        code = p.PlanCode,
                        semester = p.Semester,
                        start_date = p.StartDate.HasValue ? p.StartDate.Value.ToString("yyyy-MM-dd") : null,
                        status = p.Status,
                        total_exam_set = p.ExamSets.Count,
                        create_at = p.CreateAt.ToString(),
                        update_at = p.UpdateAt.ToString(),
                        user = p.TeacherProposals.Select(tp => new CommonObject
                        {
                            id = (int)tp.User!.Id,
                            name = tp.User.Name + " - " + tp.User.Teacher!.Name
                        }).FirstOrDefault()
                    })
                    .ToListAsync();

                // Prepare the page response
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
            catch
            {
                return new PageResponse<ProposalDTO>
                {
                    totalElements = 0,
                    totalPages = 0,
                    size = queryObject.size,
                    page = queryObject.page!.Value,
                    content = Array.Empty<ProposalDTO>()
                };
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
                return new BaseResponse<ProposalDTO> { message = $"Proposal with id = {id} could not be found" };

            var examSetDtOs = proposal.ExamSets.Select(es => new ExamSetDTO
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
                create_at = es.CreateAt.ToString(),
                update_at = es.UpdateAt.ToString(),
                exams = es.Exams.Select(e => new ExamDTO
                {
                    code = e.ExamCode,
                    id = e.ExamId,
                    name = e.ExamName,
                    comment = e.Comment,
                    description = e.Description,
                    attached_file = e.AttachedFile,
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
                    exam_sets = examSetDtOs.Count == 0 ? Array.Empty<ExamSetDTO>() : examSetDtOs,
                }
            };
        }

        public async Task<BaseResponseId> CreateProposalAsync(int userId, ProposalDTO proposalDto, string? role = null)
        {
            try
            {
                var examSets = new List<ExamSet>();
                var errors = new List<ErrorDetail>();

                var existProposal = await _context.Proposals.FirstOrDefaultAsync(p => p.PlanCode == proposalDto.code);
                if (existProposal != null)
                    errors.Add(new() { field = "code", message = $"A similar record already exists: {proposalDto.code}" });

                if (string.IsNullOrEmpty(proposalDto.code) || proposalDto.code == "string")
                    errors.Add(new() { field = "code", message = "Invalid plan code" });

                if (string.IsNullOrEmpty(proposalDto.semester) || proposalDto.semester == "string")
                    errors.Add(new() { field = "semester", message = "Invalid semester" });

                if (!_validStatus.Contains(proposalDto.status))
                    errors.Add(new() { field = "status", message = "Invalid status" });

                var isAcademicYear = await _context.AcademicYears.AnyAsync(a => a.YearName == proposalDto.academic_year.name);
                if (!isAcademicYear)
                    errors.Add(new() { field = "academic_year", message = "Invalid academic year" });

                if (!DateOnly.TryParse(proposalDto.start_date, out var parseStart))
                    errors.Add(new() { field = "start_date", message = "Invalid start date format" });

                if (!DateOnly.TryParse(proposalDto.end_date, out var parseEnd))
                    errors.Add(new() { field = "end_date", message = "Invalid end date format" });

                if (proposalDto.exam_sets != null && proposalDto.exam_sets.Any())
                {
                    var examSetIds = proposalDto.exam_sets.Select(e => e.id).ToList();
                    var existExamSets = await _context.ExamSets.Where(e => examSetIds.Contains(e.ExamSetId)).ToDictionaryAsync(e => e.ExamSetId);
                    var examSetIdSets = new HashSet<int>();

                    foreach (var item in examSetIds)
                    {
                        if (!examSetIdSets.Add((int)item!))
                            errors.Add(new() { field = $"exam_sets.{item}", message = $"Duplicate exam set: {item}" });
                        else if (!existExamSets.ContainsKey((int)item))
                            errors.Add(new() { field = $"exam_sets.{item}", message = $"Exam set not found: {item}" });
                        else
                        {
                            var examSet = existExamSets[(int)item];
                            if (examSet.ProposalId == null)
                                examSets.Add(examSet);
                            else
                                errors.Add(new() { field = $"exam_sets.{item}", message = $"The exam set has been assigned to another proposal" });
                        }
                    }
                }

                var isExistUser = await _context.Users.AnyAsync(u => u.Id == (ulong)userId);
                if (!isExistUser) errors.Add(new() { field = "user", message = $"User does not exist: {proposalDto.user.id}" });

                if (errors.Any())
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Validation failed",
                        errors = errors
                    };

                var newProposal = new Proposal
                {
                    PlanCode = proposalDto.code,
                    Semester = proposalDto.semester,
                    StartDate = parseStart,
                    EndDate = parseEnd,
                    Content = string.IsNullOrEmpty(proposalDto.content) || proposalDto.content == "string" ? string.Empty : proposalDto.content,
                    Status = proposalDto.status,
                    AcademicYear = proposalDto.academic_year.name ?? string.Empty,
                    CreateAt = DateOnly.FromDateTime(DateTime.Now),
                    ExamSets = examSets,
                    IsCreatedByAdmin = role == "Admin" ? true : false,
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

        public async Task<BaseResponseId> UpdateProposalAsync(ProposalDTO proposalDto)
        {
            try
            {
                var errorList = new List<ErrorDetail>();
                var existingProposal = await _context.Proposals.FirstOrDefaultAsync(id => id.ProposalId == proposalDto.id);
                var examSetIds = proposalDto.exam_sets?.ToList();

                if (examSetIds?.Count > 0)
                {
                    for (int i = 0; i < examSetIds.Count; i++)
                    {
                        if (examSetIds[i].id <= 0) errorList.Add(new()
                        {
                            field = $"exam_sets.{i}.code",
                            message = "Ma bo de khong hop le"
                        });
                    }
                }
                if (existingProposal != null && existingProposal.Status != $"approved")
                {
                    existingProposal.AcademicYear = proposalDto.academic_year.name;
                    existingProposal.Content = proposalDto.content;
                    existingProposal.EndDate = DateOnly.Parse(proposalDto.end_date!);
                    existingProposal.PlanCode = proposalDto.code;
                    existingProposal.StartDate = DateOnly.Parse(proposalDto.start_date!);
                    existingProposal.Semester = proposalDto.semester;
                    existingProposal.Status = proposalDto.status;
                    existingProposal.UpdateAt = DateOnly.FromDateTime(DateTime.Now);

                    var examSetList = new List<ExamSet>();
                    if (proposalDto.exam_sets != null && proposalDto.exam_sets.Any())
                    {
                        var examSetsListId = proposalDto.exam_sets.Select(e => e.id).ToList();
                        var existingExamSets = await _context.ExamSets.Where(e => examSetsListId.Contains(e.ExamSetId)).ToListAsync();
                        var examCodeSet = new HashSet<int>();
                        var examsToRemove = existingExamSets.Where(e => !examSetsListId.Contains(e.ExamSetId)).ToList();

                        if (examsToRemove.Any())
                        {
                            foreach (var examToRemove in examsToRemove)
                            {
                                examToRemove.ProposalId = null;
                            }
                        }

                        foreach (var examSet in proposalDto.exam_sets)
                        {
                            if (!examCodeSet.Add((int)examSet.id!))
                            {
                                errorList.Add(new ErrorDetail
                                {
                                    field = $"exam_set.exams.{examSet.id}",
                                    message = $"Bài thi bị trùng lặp {examSet.id}"
                                });
                            }
                            else
                            {
                                bool all = true;
                                foreach (var e in existingExamSets)
                                {
                                    if (e.ExamSetId == examSet.id)
                                    {
                                        all = false;
                                        break;
                                    }
                                }

                                if (all)
                                {
                                    errorList.Add(new ErrorDetail
                                    {
                                        field = $"exam_set.exams.{examSet.id}",
                                        message = $"Không tồn tại bài thi {examSet.id}"
                                    });
                                }
                                else
                                {

                                    var existingExamSet = existingExamSets.First(e => e.ExamSetId == examSet.id);
                                    if(existingExamSet.Status != $"approved") existingExamSet.Status = proposalDto.status; // Cập nhật trạng thái của examSet

                                    foreach (var examDto in examSet.exams!)
                                    {
                                        var existingExam = await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examDto.id);
                                        if (existingExam != null && existingExam.Status != $"approved")
                                        {
                                            existingExam.Comment = examDto.comment;
                                            existingExam.Status = proposalDto.status; 
                                        }
                                        else
                                        {
                                            errorList.Add(new ErrorDetail
                                            {
                                                field = $"exam_set.exams.{examDto.id}",
                                                message = $"Không tồn tại bài thi {examDto.id}"
                                            });
                                        }
                                    }

                                    examSetList.Add(existingExamSet);
                                }
                            }
                        }
                    }

                    if (errorList.Any())
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Dữ liệu không hợp lệ",
                            errors = errorList
                        };
                    }

                    existingProposal.ExamSets = examSetList;

                    await _context.SaveChangesAsync();

                    var detailResponse = new DetailResponse { id = existingProposal.ProposalId };
                    var baseResponseId = new BaseResponseId
                    {
                        message = "Cập nhật thành công",
                        data = detailResponse
                    };
                    return baseResponseId;
                }
                else if (existingProposal != null && existingProposal.Status == $"approved")
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

        public async Task<BaseResponseId> DeleteProposalAsync(int proposalId, bool withExamSet, bool withExam)
        {
            try
            {
                var proposal = await _context.Proposals
                    .Include(es => es.ExamSets)
                    .FirstOrDefaultAsync(p => p.ProposalId == proposalId);

                if (proposal == null)
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Not found",
                        errors = new() { new() { message = $"Proposal not found {proposalId}" } }
                    };

                if (proposal.Status == $"approved")
                    return new BaseResponseId
                    {
                        status = 405,
                        message = "Forbidden",
                        errors = new() { new() { message = "The proposal has been approved and cannot be deleted." } }
                    };

                var examSets = proposal.ExamSets;
                if (examSets.Any())
                {
                    if (!withExamSet)
                    {
                        var approvedExamSets = examSets.Where(e => e.Status == $"approved").ToList();
                        if (approvedExamSets.Any())
                            return new BaseResponseId
                            {
                                status = 405,
                                message = $"Forbidden",
                                errors = new() { new() { message = "One or more exam sets have been approved, the proposal cannot be deleted." } }
                            };

                        var examSetIds = examSets.Select(e => e.ExamSetId).ToList();
                        await _context.ExamSets
                            .Where(e => examSetIds.Contains(e.ExamSetId))
                            .ForEachAsync(e => e.ProposalId = null);
                    }
                    else
                    {
                        var approvedExamSets = examSets.Where(e => e.Status == $"approved").ToList();
                        if (approvedExamSets.Any())
                            return new BaseResponseId
                            {
                                status = 405,
                                message = "Forbidden",
                                errors = new() { new() { message = "One or more exam sets have been approved, the proposal cannot be deleted." } }
                            };

                        foreach (var item in examSets)
                        {
                            var exams = await _context.Exams.Where(e => e.ExamSetId == item.ExamSetId).ToListAsync();

                            if (!withExam)
                            {
                                var approvedExams = exams.Where(e => e.Status == "approved").ToList();
                                if (approvedExams.Any())
                                    return new BaseResponseId
                                    {
                                        status = 405,
                                        message = $"Forbidden",
                                        errors = new() { new() { message = "One or more exams have been approved, the exam set cannot be deleted." } }
                                    };

                                var examIds = exams.Select(e => e.ExamId).ToList();
                                await _context.Exams
                                    .Where(e => examIds.Contains(e.ExamId))
                                    .ForEachAsync(e => e.ExamSetId = null);
                            }
                            else
                            {
                                var approvedExams = exams.Where(e => e.Status == "approved").ToList();
                                if (approvedExams.Any())
                                    return new BaseResponseId
                                    {
                                        status = 405,
                                        message = $"Forbidden",
                                        errors = new() { new() { message = "One or more exams have been approved, the exam set cannot be deleted." } }
                                    };

                                _context.Exams.RemoveRange(exams);
                            }
                        }

                        _context.ExamSets.RemoveRange(examSets);
                    }
                }

                _context.Proposals.Remove(proposal);
                await _context.SaveChangesAsync();

                return new BaseResponseId { status = 200, message = "Delete proposal successfully", data = new() { id = proposal.ProposalId } };
            }
            catch (Exception ex)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = $"An error occurred: {ex.Message}",
                    errors = new() { new() { message = ex.InnerException!.ToString() } }
                };
            }
        }
    }
}
