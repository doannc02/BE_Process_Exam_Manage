using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
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

        public Task<BaseResponseId> CreateExamSetAsync(ExamSetDTO examSetDTO)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<string>> DeleteExamSetAsync(int id)
        {
            throw new NotImplementedException();
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
                if(userId != null)
                {
                    if(userId != user.id)
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
                    exam_set_id = examSet.ExamSetId,
                    exam_set_name = examSet.ExamSetName,
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
                        exam_code = e.ExamCode,
                        exam_id = e.ExamId,
                        exam_name = e.ExamName,
                        status = e.Status,
                        upload_date = e.UploadDate
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
                    exam_set_id = p.ExamSetId,
                    major = p.Major,
                    exam_set_name = p.ExamSetName,
                    total_exams = p.Exams.Count(),
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

        public Task<BaseResponseId> UpdateExamSetAsync(ExamSetDTO examSetDTO)
        {
            throw new NotImplementedException();
        }
    }
}
