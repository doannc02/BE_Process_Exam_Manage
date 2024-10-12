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

        //public async Task<BaseResponseId> CreateProposalAsync(int userId, ProposalDTO proposalDTO)
        //{
        //    try
        //    {
        //        var existProposal = await _context.Proposals.FirstOrDefaultAsync(p => p.PlanCode == proposalDTO.code);
        //        if (existProposal != null) return new BaseResponseId { message = $"A similar record already exists: {proposalDTO.code}" };
        //        else
        //        {
        //            var examSets = new List<ExamSet>();
        //            var errors = new List<ErrorDetail>();

        //            if (string.IsNullOrEmpty(proposalDTO.code) || proposalDTO.code == "string")
        //                errors.Add(new() { field = "proposal.code", message = "In valid plan code" });

        //            if (string.IsNullOrEmpty(proposalDTO.semester) || proposalDTO.semester == "string")
        //                errors.Add(new() { field = "proposal.semester", message = "Invalid semester" });

        //            if (!validStatus.Contains(proposalDTO.status))
        //                errors.Add(new() { field = "proposal.status", message = "Invalid status" });

        //            // Lay ten nam hoc, vd: '2022-2023'
        //            var isAcademicYear = await _context.AcademicYears.AnyAsync(a => a.YearName == proposalDTO.academic_year.name);
        //            if (!isAcademicYear)
        //                errors.Add(new() { field = "proposal.academic_year", message = "Invalid academic year" });

        //            if (proposalDTO.exam_sets != null && proposalDTO.exam_sets.Any())
        //            {
        //                var examSetIds = proposalDTO.exam_sets.Select(e => e.id).ToList();
        //                var existExamSets = await _context.ExamSets.Where(e => examSetIds.Contains(e.ExamSetId)).ToListAsync();
        //                var examSetIdSets = new HashSet<int>();

        //                foreach (var examSetIdSet in examSetIds)
        //                {
        //                    if (!examSetIdSets.Add(examSetIdSet))
        //                        errors.Add(new() { field = $"proposal.exam_sets.{examSetIdSet}", message = $"Duplicate exam set: {examSetIdSet}" });
        //                    else if (!existExamSets.Any(e => e.ExamSetId == examSetIdSet))
        //                        errors.Add(new() { field = $"proposal.exam_sets.{examSetIdSet}", message = $"Exam set not found: {examSetIdSet}" });
        //                    else
        //                        examSets.Add(existExamSets.First(e => e.ExamSetId == examSetIdSet));
        //                }
        //            }

        //            if (errors.Any())
        //                return new BaseResponseId
        //                {
        //                    status = 400,
        //                    message = "Validation failed",
        //                    errors = errors
        //                };

        //            var newProposal = new Proposal
        //            {
        //                PlanCode = proposalDTO.code,
        //                Semester = proposalDTO.semester,
        //                StartDate = DateOnly.TryParse(proposalDTO.start_date, out var parseStart) ? parseStart : default,
        //                EndDate = DateOnly.TryParse(proposalDTO.end_date, out var parseEnd) ? parseEnd : default,
        //                Content = string.IsNullOrEmpty(proposalDTO.content) || proposalDTO.content == "string" ? string.Empty : proposalDTO.content,
        //                Status = proposalDTO.status,
        //                AcademicYear = proposalDTO.academic_year.name ?? "",
        //                ExamSets = examSets,
        //            };

        //            await _context.Proposals.AddAsync(newProposal);
        //            await _context.SaveChangesAsync();

        //            return new BaseResponseId { message = "Thành công", data = new DetailResponse { id = newProposal.ProposalId } };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new BaseResponseId { status = 500, message = "An error occurred: " + ex.Message + "\n" + ex.InnerException };
        //    }
        //}

        public async Task<BaseResponseId> CreateProposalAsync(int userId, ProposalDTO proposalDTO)
        {
            try
            {
                var examSets = new List<ExamSet>();
                var errors = new List<ErrorDetail>();

                var existProposal = await _context.Proposals.FirstOrDefaultAsync(p => p.PlanCode == proposalDTO.code);
                if (existProposal != null)
                    errors.Add(new() { field = "proposal.code", message = $"A similar record already exists: {proposalDTO.code}" });

                if (string.IsNullOrEmpty(proposalDTO.code) || proposalDTO.code == "string")
                    errors.Add(new() { field = "proposal.code", message = "Invalid plan code" });

                if (string.IsNullOrEmpty(proposalDTO.semester) || proposalDTO.semester == "string")
                    errors.Add(new() { field = "proposal.semester", message = "Invalid semester" });

                if (!validStatus.Contains(proposalDTO.status))
                    errors.Add(new() { field = "proposal.status", message = "Invalid status" });

                var isAcademicYear = await _context.AcademicYears.AnyAsync(a => a.YearName == proposalDTO.academic_year.name);
                if (!isAcademicYear)
                    errors.Add(new() { field = "proposal.academic_year", message = "Invalid academic year" });

                if (!DateOnly.TryParse(proposalDTO.start_date, out var parseStart))
                    errors.Add(new() { field = "proposal.start_date", message = "Invalid start date format" });

                if (!DateOnly.TryParse(proposalDTO.end_date, out var parseEnd))
                    errors.Add(new() { field = "proposal.end_date", message = "Invalid end date format" });

                if (proposalDTO.exam_sets != null && proposalDTO.exam_sets.Any())
                {
                    var examSetIds = proposalDTO.exam_sets.Select(e => e.id).ToList();
                    var existExamSets = await _context.ExamSets.Where(e => examSetIds.Contains(e.ExamSetId)).ToDictionaryAsync(e => e.ExamSetId);
                    var examSetIdSets = new HashSet<int>();

                    foreach (var item in examSetIds)
                    {
                        if (!examSetIdSets.Add(item))
                            errors.Add(new() { field = $"proposal.exam_sets.{item}", message = $"Duplicate exam set: {item}" });
                        else if (!existExamSets.ContainsKey(item))
                            errors.Add(new() { field = $"proposal.exam_sets.{item}", message = $"Exam set not found: {item}" });
                        else
                        {
                            var examSet = existExamSets[item];
                            if (examSet.ProposalId == null)
                                examSets.Add(examSet);
                            else
                                errors.Add(new() { field = $"proposal.exam_sets.{item}", message = $"The exam set has been assigned to another proposal" });
                        }
                    }
                }

                var isExistUser = await _context.Users.AnyAsync(u => u.Id == (ulong)userId);
                if (!isExistUser) errors.Add(new() { field = "proposal.user", message = $"User does not exist: {proposalDTO.user.id}" });

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

        public async Task<BaseResponseId> UpdateStateProposalAsync(int proposalId, string newState)
        {
            try
            {
                var existProposal = await _context.Proposals.FirstOrDefaultAsync(p => p.ProposalId == proposalId);

                if (existProposal != null && existProposal.Status == "approved")
                {
                    return new BaseResponseId
                    {
                        message = "Kế hoạch đã phê duyệt không được sửa",
                        errors = new List<ErrorDetail>()
                        {
                            new()
                            {
                                field = "proposal.new_state",
                                message = "không được sửa"
                            }
                        }
                    };
                }
                else if (existProposal != null && existProposal.Status == newState)
                {
                    return new BaseResponseId
                    {
                        message = "Không có thay đổi",
                        data = new DetailResponse { id = existProposal.ProposalId },
                    };
                }
                else if (existProposal != null)
                {
                    existProposal.Status = newState;

                    //await _context.SaveChangesAsync();

                    return new BaseResponseId
                    {
                        message = "Cập nhật trạng thái thành công",
                        data = new DetailResponse { id = existProposal.ProposalId }
                    };
                }
                else
                {
                    return new BaseResponseId
                    {
                        message = "Không tìm thấy đề xuất",
                        data = new DetailResponse { id = null }
                    };
                }
            }
            catch (Exception ex)
            {
                var baseResponseId = new BaseResponseId
                {
                    message = "An error occurred: " + ex.Message,
                    data = new DetailResponse { id = null }
                };

                return baseResponseId;
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
