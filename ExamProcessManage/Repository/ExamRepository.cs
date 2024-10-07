using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.RequestModels;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class ExamRepository : IExamRepository
    {
        private readonly ApplicationDbContext _context;
        public ExamRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<BaseResponse<int>> CreateExamAsync(ExamDTO examDTO)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<string>> DeleteExamAsync(int examId)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<ExamDTO>> GetExamAsync(int examId)
        {
            try
            {
                var p = await _context.Exams.FindAsync(examId);
                if (p == null) return null;
                var academic_years = _context.AcademicYears.AsNoTracking().ToList();
                var examDto = new ExamDTO
                {
                    comment = p.Comment,
                    attached_file = p.AttachedFile,
                    description = p.Description,
                    code = p.ExamCode,
                    id = p.ExamId,
                    name = p.ExamName,
                    status = p.Status,
                    upload_date = p.UploadDate,
                    academic_year = new CommonObject
                    {
                        id = academic_years.Find(a => a.AcademicYearId == p.AcademicYearId).AcademicYearId,
                        name = academic_years.Find(a => a.AcademicYearId == p.AcademicYearId).YearName
                    }
                };
                return new BaseResponse<ExamDTO>
                {
                    message = "Thành công",
                    data = examDto
                };

            }
            catch (Exception ex)
            {
                return new BaseResponse<ExamDTO>
                {
                    message = "Thành công",
                    data = null
                };
            }
        }

        public async Task<PageResponse<ExamDTO>> GetListExamAsync(ExamRequestParams queryObject)
        {
            try
            {
                var startRow = (queryObject.page.Value - 1) * queryObject.size;
                var query = _context.Exams.AsNoTracking().AsQueryable();
                var academic_years = _context.AcademicYears.AsNoTracking().ToList();

                if (!string.IsNullOrEmpty(queryObject.search))
                {
                    query = query.Where(p => p != null && p.ExamName.Contains(queryObject.search));
                    query = query.Where(p => p != null && p.ExamCode.Contains(queryObject.search));
                }

                if (queryObject.exam_set_id != null)
                {
                    query = query.Where(p => queryObject.exam_set_id == (p.ExamSetId));
                }

                var totalCount = query.Count();

                var exams = await query.OrderBy(p => p.ExamId).Skip(startRow).Take(queryObject.size).Select(p => new ExamDTO
                {
                    comment = p.Comment,
                    attached_file = p.AttachedFile,
                    description = p.Description,
                    code = p.ExamCode,
                    id = p.ExamId,
                    name = p.ExamName,
                    status = p.Status,
                    upload_date = p.UploadDate,
                    academic_year = new CommonObject
                    {
                        id = academic_years.Find(a => a.AcademicYearId == p.AcademicYearId).AcademicYearId,
                        name = academic_years.Find(a => a.AcademicYearId == p.AcademicYearId).YearName
                    }
                }).ToListAsync();

                var pageResponse = new PageResponse<ExamDTO>
                {
                    totalElements = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                    size = queryObject.size,
                    page = queryObject.page.Value,
                    content = exams.ToArray(),
                };

                return pageResponse;
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return null;
            }
        }

        public Task<BaseResponse<int>> UpdateExamAsync(ExamDTO examDTO)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<int>> UpdateState(int examId, string status, string comment)
        {
            try
            {
                var findExam = await _context.Exams.FindAsync(examId);
                if(findExam == null)
                {
                    return new BaseResponse<int>
                    {
                        message = "Có lỗi xảy ra",
                    };
                }
                List<string> validStatuses = new List<string> { "in_progress", "rejected", "approved", "pending_approval" };
                

                bool isValid = validStatuses.Contains(status);
               if (!isValid )   return new BaseResponse<int>
                {
                    message = "Có lỗi xảy ra",
                };
                findExam.Status = status;
                findExam.Comment = comment;
                return new BaseResponse<int>
                {
                    data = findExam.ExamId,
                    message = "Thành công",
                };
            }
            catch (Exception ex)
            {
                //todo handle log here 
                return new BaseResponse<int>
                {
                    message = "Có lỗi xảy ra",
                };

            }
        }
    }
}
