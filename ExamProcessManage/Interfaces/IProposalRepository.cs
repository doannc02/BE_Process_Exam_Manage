using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.RequestModels;

namespace ExamProcessManage.Interfaces
{
    public interface IProposalRepository
    {
        Task<PageResponse<ProposalDTO>> GetListProposalsAsync(int? userId, QueryObjectProposal queryObject);
        Task<BaseResponse<ProposalDTO>> GetDetailProposalAsync(int id);
        Task<BaseResponseId> CreateProposalAsync(int userId, ProposalDTO proposalDTO);
        Task<BaseResponseId> UpdateProposalAsync(ProposalDTO proposalDTO);
        Task<BaseResponseId> UpdateStateProposalAsync(int proposalId, string newState, string? comment = null);
        Task<BaseResponseId> DeleteProposalAsync(int id);
    }
}
