using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;

namespace ExamProcessManage.Interfaces
{
    public interface IUserRepository
    {
        Task<PageResponse<UserDTO>> GetListUsersAsync(QueryObject query);
        Task<BaseResponse<UserDTO>> GetDetailUserAsync(int userID);
    }
}
