using ExamProcessManage.Data;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class AcademicYearRepository : IAcademicYearRepository
    {
        public ApplicationDbContext _context;

        public AcademicYearRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<AcademicYearResponse>> GetListAcademicYearAsync(QueryObject queryObject)
        {
            var academicYearsQuery = _context.AcademicYears.AsQueryable();
            var totalCount = await academicYearsQuery.CountAsync();
            var academicYears = await academicYearsQuery
                .Skip((queryObject.page.Value - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            var yearResponses = new List<AcademicYearResponse>();

            foreach (var item in academicYears)
            {
                var academic = new AcademicYearResponse()
                {
                    academic_year_id = item.AcademicYearId,
                    year_name = item.YearName??string.Empty,
                    start_year = (int)item.StartYear,
                    end_year = (int)item.EndYear
                };

                yearResponses.Add(academic);
            }

            var pageResponse = new PageResponse<AcademicYearResponse>()
            {
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                content = yearResponses
            };

            return pageResponse;
        }

        public async Task<BaseResponse<AcademicYearResponse>> GetDetailAcademicYearAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
