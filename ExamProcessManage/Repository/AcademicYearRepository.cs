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
        private readonly ApplicationDbContext _context;

        public AcademicYearRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<AcademicYearResponse>> GetListAcademicYearAsync(QueryObject queryObject)
        {
            var yearResponses = new List<AcademicYearResponse>();
            var queryAcademicYears = _context.AcademicYears.AsQueryable();
            var totalCount = await queryAcademicYears.CountAsync();
            var listAcademicYears = await queryAcademicYears
                .Skip((queryObject.page.Value - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            foreach (var item in listAcademicYears)
            {
                var academic = new AcademicYearResponse()
                {
                    id = item.AcademicYearId,
                    name = item.YearName ?? string.Empty,
                    start_year = (int)item.StartYear,
                    end_year = (int)item.EndYear
                };

                yearResponses.Add(academic);
            }

            return new PageResponse<AcademicYearResponse>()
            {
                content = yearResponses,
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                page = queryObject.page.Value,
                numberOfElements = yearResponses.Count
            };
        }

        public async Task<BaseResponse<AcademicYearResponse>> GetDetailAcademicYearAsync(int id)
        {
            var academicYear = await _context.AcademicYears.FindAsync(id);

            if (academicYear != null)
            {
                return new BaseResponse<AcademicYearResponse>()
                {
                    message = "success",
                    data = new AcademicYearResponse
                    {
                        id = academicYear.AcademicYearId,
                        name = academicYear.YearName ?? string.Empty,
                        start_year = (int)academicYear.StartYear,
                        end_year = (int)academicYear.EndYear
                    }
                };
            }
            else
            {
                return new BaseResponse<AcademicYearResponse>
                {
                    message = $"academic_year with id = '{id}' could not be found"
                };
            }
        }

        public async Task<BaseResponse<AcademicYearResponse>> CreateAcademicYearAsync(AcademicYearResponse year)
        {
            var response = new BaseResponse<AcademicYearResponse>();

            try
            {
                year.end_year = year.end_year == 0 ? year.start_year + 1 : year.end_year;
                year.name = $"{year.start_year}-{year.end_year}";

                var existYear = await _context.AcademicYears.AnyAsync(a => a.AcademicYearId == year.id || a.YearName == year.name);

                if (!existYear)
                {
                    var academicYear = new AcademicYear
                    {
                        AcademicYearId = year.id,
                        YearName = year.name,
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
                    response.message = $"conflict data with id = '{year.id}' or name = '{year.name}'";
                }

            }
            catch (Exception ex)
            {
                response.message = $"an error occurred: {ex.Message}";
            }

            return response;
        }

        public async Task<BaseResponse<AcademicYearResponse>> UpdateAcademicYearAsync(AcademicYearResponse year)
        {
            var response = new BaseResponse<AcademicYearResponse>();

            try
            {
                year.name = $"{year.start_year}-{year.end_year}";

                var existYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.AcademicYearId == year.id);

                if (existYear != null)
                {
                    if (existYear.YearName != year.name)
                    {
                        var yearByNameExists = await _context.AcademicYears.AnyAsync(y => y.YearName == year.name);

                        if (!yearByNameExists)
                        {
                            existYear.StartYear = year.start_year;
                            existYear.EndYear = year.end_year;
                            existYear.YearName = year.name;

                            await _context.SaveChangesAsync();

                            response.message = "update successfully";
                            response.data = year;
                        }
                        else
                        {
                            response.message = $"an academic_year with the name '{year.name}' already exists";
                        }
                    }
                    else
                    {
                        response.message = "no changes detected";
                    }
                }
                else
                {
                    response.message = $"no academic_year found with ID = '{year.id}'";
                }
            }
            catch (Exception ex)
            {
                response.message = $"an error occurred: {ex.Message}";
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
                    id = existYear.AcademicYearId,
                    name = existYear.YearName,
                    start_year = (int)existYear.StartYear,
                    end_year = (int)existYear.EndYear
                };
            }
            else
            {
                response.message = $"no academic_year found with ID = '{yearId}'";
            }

            return response;
        }
    }
}
