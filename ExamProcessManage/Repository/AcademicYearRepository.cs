using ExamProcessManage.Data;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
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
                    year_id = item.AcademicYearId,
                    year_name = item.YearName ?? string.Empty,
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
            var academicYear = await _context.AcademicYears.FindAsync(id);
            if (academicYear != null)
            {
                return new BaseResponse<AcademicYearResponse>()
                {
                    message = "Success",
                    data = new AcademicYearResponse
                    {
                        year_id = academicYear.AcademicYearId,
                        year_name = academicYear.YearName ?? string.Empty,
                        start_year = (int)academicYear.StartYear,
                        end_year = (int)academicYear.EndYear
                    }
                };
            }
            else
            {
                return new BaseResponse<AcademicYearResponse>
                {
                    message = $"academic_year with id = {id} could not be found"
                };
            }
        }

        public async Task<BaseResponse<AcademicYearResponse>> CreateAcademicYearAsync(AcademicYearResponse year)
        {
            year.end_year = year.end_year == 0 ? year.start_year + 1 : year.end_year;
            year.year_name = $"{year.start_year}-{year.end_year}";

            var response = new BaseResponse<AcademicYearResponse>();
            var existYear = _context.AcademicYears.FirstOrDefault(a => a.AcademicYearId == year.year_id || a.YearName == year.year_name);

            if (existYear == null)
            {
                var academicYear = new AcademicYear
                {
                    AcademicYearId = year.year_id,
                    YearName = year.year_name,
                    StartYear = year.start_year,
                    EndYear = year.end_year
                };

                await _context.AcademicYears.AddAsync(academicYear);
                await _context.SaveChangesAsync();

                response.message = "add academic_year successfully";
                response.data = year;
            }
            else
            {
                response.message = $"conflict data with id = {year.year_id} or name = {year.year_name}";
            }

            return response;
        }

        public async Task<BaseResponse<AcademicYearResponse>> UpdateAcademicYearAsync(AcademicYearResponse year)
        {
            year.year_name = $"{year.start_year}-{year.end_year}";

            var response = new BaseResponse<AcademicYearResponse>();
            var existYear = await _context.AcademicYears.SingleAsync(y => y.AcademicYearId == year.year_id);

            if (existYear != null)
            {
                if (existYear.YearName != year.year_name)
                {
                    existYear.StartYear = year.start_year;
                    existYear.EndYear = year.end_year;
                    existYear.YearName = year.year_name;

                    var yearByName = await _context.AcademicYears.AnyAsync(y => y.YearName == existYear.YearName);

                    if (!yearByName)
                    {
                        await _context.SaveChangesAsync();

                        response.message = "update successfully";
                        response.data = year;
                    }
                    else
                    {
                        response.message = $"academic_year with data = '{existYear.YearName}' already existed";
                    }
                }
                else
                {
                    response.message = "nothing change";
                }
            }
            else
            {
                response.message = $"could not found data with id = {year.year_id}";
            }

            return response;
        }

        public async Task<BaseResponse<AcademicYearResponse>> DeleteAcademicYearAsync(int yearId)
        {
            var response = new BaseResponse<AcademicYearResponse>();

            var existYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.AcademicYearId == yearId);

            if (existYear != null)
            {
                _context.AcademicYears.Remove(existYear);
                await _context.SaveChangesAsync();

                response.message = "delete successfully";
                response.data = new AcademicYearResponse
                {
                    year_id = existYear.AcademicYearId,
                    year_name = existYear.YearName,
                    start_year = (int)existYear.StartYear,
                    end_year = (int)existYear.EndYear
                };
            }
            else
            {
                response.message = $"could not found data with id = {yearId}";
            }

            return response;
        }
    }
}
