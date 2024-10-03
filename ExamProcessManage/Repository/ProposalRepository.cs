using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class ProposalRepository : IProposalRepository
    {
        private readonly ApplicationDbContext _context;

        public ProposalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BaseResponseId> CreateProposalAsync(ProposalDTO proposalDTO)
        {
            try
            {
                var ckExistProposal = await _context.Proposals.FirstOrDefaultAsync(id => id.ProposalId == proposalDTO.proposal_id || id.PlanCode == proposalDTO.plan_code);
                if (ckExistProposal == null)
                {
                    var newProposal = new Proposal
                    {
                        AcademicYear = proposalDTO.academic_year,
                        Content = proposalDTO.content,
                        EndDate = proposalDTO.end_date,
                        PlanCode = proposalDTO.plan_code,
                        StartDate = proposalDTO.start_date,
                        Semester = proposalDTO.semester,
                        Status = proposalDTO.status,
                        //ExamSets = (ICollection<ExamSet>)proposalDTO.exam_sets,
                        //TeacherProposals = (ICollection<TeacherProposal>)proposalDTO.teacher_roposals
                    };

                    _ = await _context.Proposals.AddAsync(newProposal);
                    _ = await _context.SaveChangesAsync();

                    var detailResponse = new DetailResponse { id = newProposal.ProposalId };
                    var baseResponseId = new BaseResponseId
                    {
                        message = "Thành công",
                        data = detailResponse
                    };
                    return baseResponseId;
                }
                else
                {
                    var detailResponse = new DetailResponse { id = ckExistProposal.ProposalId };
                    var baseResponseId = new BaseResponseId
                    {
                        message = "Đã tồn tại",
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

        public async Task<BaseResponse<ProposalDTO>> GetDetailProposalAsync(int id)
        {
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
                course = new CommonObject
                {
                    name = es.Course?.CourseName ?? string.Empty,
                    code = es.Course?.CourseCode ?? string.Empty,
                    id = es.Course?.CourseId ?? 0
                },
                department = es.Department,
                description = es.Description,
                exam_set_id = es.ExamSetId,
                exam_set_name = es.ExamSetName,
                exam_quantity = es.ExamQuantity,
                status = es.Status,
                exams = es.Exams.Select(e => new ExamDTO
                {
                    academic_year = new CommonObject
                    {
                        id = e.AcademicYear?.AcademicYearId ?? 0,
                        name = e.AcademicYear?.YearName ?? string.Empty,
                    },
                    attached_file = e.AttachedFile,
                    comment = e.Comment,
                    description = e.Description,
                    exam_code = e.ExamCode,
                    exam_id = e.ExamId,
                    exam_name = e.ExamName,
                    status = e.Status,
                    upload_date = e.UploadDate
                }).ToList(),
                major = es.Major,
            }).ToList();

            var teacherProposal = proposal.TeacherProposals.FirstOrDefault();
            var user = teacherProposal?.User;
            var teacher = user?.Teacher;

            return new BaseResponse<ProposalDTO>
            {
                data = new ProposalDTO
                {
                    proposal_id = proposal.ProposalId,
                    academic_year = proposal.AcademicYear,
                    content = proposal.Content,
                    end_date = proposal.EndDate,
                    start_date = proposal.StartDate,
                    plan_code = proposal.PlanCode,
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

                var proposals = await query.OrderBy(p => p.ProposalId).Skip(startRow).Take(queryObject.size).Include(p => p.TeacherProposals)
                    .ThenInclude(tp => tp.User).ThenInclude(u => u.Teacher)
                    .Select(p => new ProposalDTO
                    {
                        proposal_id = p.ProposalId,
                        academic_year = p.AcademicYear,
                        content = p.Content,
                        end_date = p.EndDate,
                        plan_code = p.PlanCode,
                        semester = p.Semester,
                        start_date = p.StartDate,
                        status = p.Status,
                        total_exam_set = p.TeacherProposals.Count(),
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

        public async Task<BaseResponseId> UpdateProposalAsync(ProposalDTO proposalDTO)
        {
            try
            {
                var existingProposal = await _context.Proposals.FirstOrDefaultAsync(id => id.ProposalId == proposalDTO.proposal_id);

                if (existingProposal != null && existingProposal.Status != "approved")
                {
                    existingProposal.AcademicYear = proposalDTO.academic_year;
                    existingProposal.Content = proposalDTO.content;
                    existingProposal.EndDate = proposalDTO.end_date;
                    existingProposal.PlanCode = proposalDTO.plan_code;
                    existingProposal.StartDate = proposalDTO.start_date;
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
                else if(existingProposal != null && existingProposal.Status == "approved")
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
    }
}
