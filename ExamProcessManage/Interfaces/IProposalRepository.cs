using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IProposalRepository
    {
        Task<PageResponse<ProposalDTO>> GetListProposalsAsync(int? userId, QueryObject queryObject);
        Task<BaseResponse<ProposalDTO>> GetDetailProposalAsync(int id);
        Task<BaseResponseId> CreateProposalAsync(ProposalDTO proposalDTO);
        Task<BaseResponseId> UpdateProposalAsync(ProposalDTO proposalDTO);
        Task<BaseResponse<string>> DeleteProposalAsync(int id);
    }
}
