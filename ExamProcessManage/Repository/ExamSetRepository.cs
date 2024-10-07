using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Encodings;

namespace ExamProcessManage.Repository
{
    public class ExamSetRepository : IExamSetRepository
    {
        private readonly ApplicationDbContext _context;
        public ExamSetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BaseResponseId> CreateExamSetAsync(ExamSetDTO examSetDto)
        {
            // Kiểm tra dữ liệu đầu vào
            if (examSetDto == null || examSetDto.exams == null || !examSetDto.exams.Any())
            {
                return null; // Dữ liệu không hợp lệ
            }

            // Kiểm tra xem ExamSet đã tồn tại trong DB hay chưa
            var ckExisExamSets = await _context.ExamSets
                .FirstOrDefaultAsync(es => es.ExamSetId == examSetDto.id || es.ExamSetName == examSetDto.name);

            if (ckExisExamSets != null)
            {
                var errs = new List<ErrorCodes>();
                // Nếu ExamSet đã tồn tại, trả về thông báo tồn tại
                var err = new ErrorCodes
                {
                    message = "ExamSet đã tồn tại",
                    code= $"exam_set_name"
                };
                return new BaseResponseId
                {
                    message = "Exam set đã tồn tại",
                    errs = errs
                };
            }

            // Kiểm tra phần tử trùng lặp trong mảng Exams
            for (int i = 0; i < examSetDto.exams.ToList().Count; i++)
            {
                var exam = examSetDto?.exams.ToList()[i];

                // Kiểm tra trùng lặp dựa trên ExamCode hoặc tiêu chí khác
                var existingExamCode = await _context.Exams
                    .FirstOrDefaultAsync(e => e.ExamCode == exam.code);

                     var existingExamName = await _context.Exams
                    .FirstOrDefaultAsync(e => e.ExamName == exam.name);

                if (existingExamCode != null)
                {
                    var errs = new List<ErrorCodes>();
                    // Trả về index của phần tử bị trùng
                    var err = new ErrorCodes
                    {
                        message = "Exam đã tồn tại",
                        code = $"exam_set.{i}.code"
                    };
                    return new BaseResponseId
                    {
                        errs = errs,
                    };
                }

                if (existingExamName != null)
                {
                    var errs = new List<ErrorCodes>();
                    // Trả về index của phần tử bị trùng
                    var err = new ErrorCodes
                    {
                        message = "Tên Exam đã tồn tại",
                        code = $"exam_set.{i}.exam_name"
                    };
                    return new BaseResponseId
                    {
                        errs = errs,
                    };
                }
            }

            // Nếu không có trùng lặp, tạo mới ExamSet
            var newExamSet = new ExamSet
            {
                ExamSetName = examSetDto.name,
                Description = examSetDto.description,
                ExamQuantity = examSetDto.exam_quantity,
                Major = examSetDto.major,
                Status = examSetDto.status,
                CourseId = examSetDto.course.id,
                ProposalId = examSetDto?.proposal?.id,
                Department = examSetDto.department,

                Exams = examSetDto.exams.Select(e => new Exam
                {
                    AttachedFile = e.attached_file,
                    Comment = e.comment,
                    Description = e.description,
                    ExamCode = e.code,
                    ExamName = e.name,
                    ExamSetId = examSetDto.id,
                    Status = e.status,
                    AcademicYearId = e.academic_year.id,
                    UploadDate = DateOnly.FromDateTime(DateTime.Now),
                }).ToList()
            };

            // Thêm vào context và lưu vào database
            _context.ExamSets.Add(newExamSet);
            await _context.SaveChangesAsync();

            // Trả về response thành công
            return new BaseResponseId
            {
                message = "ExamSet đã được tạo thành công",
                data = new DetailResponse { id = newExamSet.ExamSetId }
            };
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
                    name  = examSet.ExamSetName,
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
                    id = p.ExamSetId,
                    major = p.Major,
                    name = p.ExamSetName,
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
