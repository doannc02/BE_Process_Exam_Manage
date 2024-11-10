using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context) {
            _context = context;
        }
        public async Task<BaseResponse<UserDTO>> GetDetailUserAsync(int userId)
        {
            var findUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == (ulong)userId);
            if (findUser == null)
            {
                return new BaseResponse<UserDTO>
                {
                    data = null!,
                    message = "Khong tim thay user"
                };
            }
            var teachers = _context.Teachers.Where(t => t.Id == findUser.TeacherId).AsNoTracking().FirstOrDefault();
            var userDto = new UserDTO
            {
               avatarPath = findUser?.AvatarPath,
               email = findUser?.Email,
               fullname = teachers?.Name,
               name = findUser?.Name,
            };
            return new BaseResponse<UserDTO>
            {
                message = "Thành công",
                data = userDto
            };

        }

        public async Task<PageResponse<UserDTO>> GetListUsersAsync(QueryObject queryObject)
        {
            var startRow = (queryObject.page - 1) * queryObject.size;
            var userDtOs = new List<UserDTO>();
            var teachers = _context.Teachers.AsNoTracking().ToList();
            var queryAcademicYears = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(queryObject.search))
            {
                queryAcademicYears = queryAcademicYears.Where(p => p.Email.Contains(queryObject.search));
            }

            queryAcademicYears = queryAcademicYears.Where(u => u.RoleId.HasValue && u.RoleId.Value != 1);

            var totalCount = await queryAcademicYears.CountAsync();
            var listAcademicYears = await queryAcademicYears
                .Skip(startRow).Take(queryObject.size)
                .ToListAsync();

            foreach (var item in listAcademicYears)
            {
                var fullname = item.TeacherId.HasValue ? teachers.FirstOrDefault(i => i.Id == item.TeacherId.Value)?.Name : null;

                if (!string.IsNullOrEmpty(fullname)) // Kiểm tra nếu fullname không rỗng
                {
                    var academic = new UserDTO()
                    {
                        id = item.Id.ToString(),
                        name = item.Email,
                        fullname = fullname
                    };

                    userDtOs.Add(academic);
                }
            }

            return new PageResponse<UserDTO>()
            {
                content = userDtOs,
                totalElements = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                size = queryObject.size,
                page = queryObject.page,
                numberOfElements = userDtOs.Count
            };
        }


    }
}
