using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context) {
            _context = context;
        }
        public Task<BaseResponse<UserDTO>> GetDetailUserAsync(int userID)
        {
            throw new NotImplementedException();
        }

        public async Task<PageResponse<UserDTO>> GetListUsersAsync(QueryObject queryObject)
        {
            var yearResponses = new List<UserDTO>();
            var teachers = _context.Teachers.AsNoTracking().ToList();
            var queryAcademicYears = _context.Users.AsQueryable();
            var totalCount = await queryAcademicYears.CountAsync();
            var listAcademicYears = await queryAcademicYears
                .Skip((queryObject.page.Value - 1) * queryObject.size)
                .Take(queryObject.size)
                .ToListAsync();

            foreach (var item in listAcademicYears)
            {
                var academic = new UserDTO()
                {
                    id = item.Id.ToString(),
                    name = item.Email,
                    fullname = teachers.FirstOrDefault(i => i.Id == item.TeacherId).Name,
                };

                yearResponses.Add(academic);
            }

            return new PageResponse<UserDTO>()
            {
                content = yearResponses,
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                page = queryObject.page.Value,
                numberOfElements = yearResponses.Count
            };
        }
    }
}
