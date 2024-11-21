using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;

namespace ExamProcessManage.Interfaces
{
    public interface IProposalRepository
    {
        Task<PageResponse<ProposalDTO>> GetListProposalsAsync(int? userId, QueryObjectProposal queryObject);
        Task<BaseResponse<ProposalDTO>> GetDetailProposalAsync(int id);
        Task<BaseResponseId> CreateProposalAsync(int userId, ProposalDTO proposalDto, string role);
        Task<BaseResponseId> UpdateProposalAsync(int userId, ProposalDTO proposalDto, string role);
        Task<BaseResponseId> DeleteProposalAsync(int id, bool proposalOnly, bool withExam);
    }
}
