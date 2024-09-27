using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.ResponseModels;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Minio.DataModel;
using System.Linq;
using System.Net.WebSockets;

namespace ExamProcessManage.Repository
{
    public class ProposalRepository : IProposalRepository
    {
        private readonly ApplicationDbContext  _context;

        public ProposalRepository(ApplicationDbContext context) { 
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
                        ExamSets = (ICollection<ExamSet>)proposalDTO.exam_sets,
                        TeacherProposals = (ICollection<TeacherProposal>)proposalDTO.teacher_roposals
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

        public Task<BaseResponse<string>> DeleteProposalAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<ProposalDTO>> GetDetailProposalAsync(int id)
        {
            var proposal = await _context.Proposals.AsNoTracking().FirstOrDefaultAsync(p => p.ProposalId == id);
            if (proposal == null)
            {
                return new BaseResponse<ProposalDTO>
                {
                    message = $"Proposal with id = {id} could not be found",
                    data = null
                };
            }

            var examSets = await _context.ExamSets
            .Where(e => e.ProposalId == id)
            .AsNoTracking()
            .ToListAsync();

            var academicYears = await _context.AcademicYears.AsNoTracking().ToListAsync();
            var courses = await _context.Courses.AsNoTracking().ToListAsync();
            var exams = await _context.Exams.AsNoTracking().ToListAsync();

            var examSetDTOs = examSets.Select(item =>
            {
                var course = courses.FirstOrDefault(c => c.CourseId == item.CourseId);
                var examInfos = exams.Where(ex => ex.ExamSetId == item.ExamSetId).ToList();

                var examDTOs = examInfos.Select(i =>
                {
                    var academicYear = academicYears.FirstOrDefault(a => a.AcademicYearId == i.AcademicYearId);
                    return new ExamDTO
                    {
                        academic_year = new CommonObject
                        {
                            id = academicYear?.AcademicYearId ?? 0,
                            name = academicYear?.YearName ?? string.Empty,
                        },
                        attached_file = i.AttachedFile,
                        comment = i.Comment,
                        description = i.Description,
                        exam_code = i.ExamCode,
                        exam_id = i.ExamId,
                        exam_name = i.ExamName,
                        status = i.Status,
                        upload_date = i.UploadDate
                    };
                }).ToList();

                return new ExamSetDTO
                {
                    course = new CommonObject
                    {
                        name = course?.CourseName ?? string.Empty,
                        code = course?.CourseCode ?? string.Empty,
                        id = course?.CourseId ?? 0
                    },
                    department = item.Department,
                    description = item.Description,
                    exam_set_id = item.ExamSetId,
                    exam_set_name = item.ExamSetName,
                    exam_quantity = item.ExamQuantity,
                    status = item.Status,
                    exams = examDTOs,
                    major = item.Major,
                };
            }).ToList();

            var teacherProposals = await _context.TeacherProposals
            .Where(tp => tp.ProposalId == id)
            .AsNoTracking()
            .ToListAsync();

            var userIds = teacherProposals.Select(tp => tp.UserId).ToList();
            var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync();

            var teacherIds = users.Select(u => u.TeacherId).ToList();
            var teachers = await _context.Teachers
            .Where(t => teacherIds.Contains(t.Id))
            .AsNoTracking()
            .ToListAsync();

            var teaObj = teacherProposals.FirstOrDefault(tp => tp.ProposalId == proposal.ProposalId);
            if (teaObj == null) return null;

            var userObj = users.FirstOrDefault(u => u.Id == teaObj.UserId);
            if (userObj == null) return null;

            var teacherObj = teachers.FirstOrDefault(t => t.Id == userObj.TeacherId);
            if (teacherObj == null) return null;

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
                        id = (int)userObj.Id,
                        name = $"{userObj.Name} - {teacherObj.Name}"
                    },
                    exam_sets = examSetDTOs.Count == 0 ? Array.Empty<ExamSetDTO>() : examSetDTOs,
                }
            };
        }

        public async Task<PageResponse<ProposalDTO>> GetListProposalsAsync(QueryObject queryObject)
        {
            try
            {
                var startRow = (queryObject.page.Value - 1) * queryObject.size + 1;
                var endRow = queryObject.page.Value * queryObject.size;

                var proposals = await _context.Proposals.FromSqlRaw(@"
WITH CTE AS (
SELECT *, ROW_NUMBER() OVER (ORDER BY proposal_id) AS RowNum
FROM proposals
)
SELECT *
FROM CTE
WHERE RowNum BETWEEN {0} AND {1}", startRow, endRow)
                .AsNoTracking()
                .ToListAsync();

                var proposalIds = proposals.Select(p => p.ProposalId).ToList();

                var teacherProposals = await _context.TeacherProposals
                .Where(tp => proposalIds.Contains((int)tp.ProposalId))
                .AsNoTracking()
                .ToListAsync();

                var userIds = teacherProposals.Select(tp => tp.UserId).ToList();

                var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .AsNoTracking()
                .ToListAsync();

                var teacherIds = users.Select(u => u.TeacherId).ToList();

                var teachers = await _context.Teachers
                .Where(t => teacherIds.Contains(t.Id))
                .AsNoTracking()
                .ToListAsync();

                var proDTOs = proposals.Select(item =>
                {
                    var teaObj = teacherProposals.FirstOrDefault(tp => tp.ProposalId == item.ProposalId);
                    if (teaObj == null) return null;

                    var userObj = users.FirstOrDefault(u => u.Id == teaObj.UserId);
                    if (userObj == null) return null;

                    var teacherObj = teachers.FirstOrDefault(t => t.Id == userObj.TeacherId);
                    if (teacherObj == null) return null;

                    return new ProposalDTO
                    {
                        proposal_id = item.ProposalId,
                        academic_year = item.AcademicYear,
                        content = item.Content,
                        end_date = item.EndDate,
                        plan_code = item.PlanCode,
                        semester = item.Semester,
                        start_date = item.StartDate,
                        status = item.Status,
                        user = new CommonObject
                        {
                            id = (int)userObj.Id,
                            name = userObj.Name + " - " + teacherObj.Name,
                        }
                    };
                }).Where(dto => dto != null).ToList();

                var totalCount = await _context.AcademicYears.CountAsync();
                var pageResponse = new PageResponse<ProposalDTO>
                {
                    totalElements = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                    size = queryObject.size,
                    page = (int)queryObject.page,
                    content = proDTOs.ToArray()
                };

                return pageResponse;
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return null;
            }
        }

        public Task<BaseResponseId> UpdateProposalAsync(ProposalDTO proposalDTO)
        {
            throw new NotImplementedException();
        }
    }
}
