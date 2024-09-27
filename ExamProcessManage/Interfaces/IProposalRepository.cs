using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.ResponseModels;

namespace ExamProcessManage.Interfaces
{
    public interface IProposalRepository
    {
        Task<PageResponse<ProposalDTO>> GetListProposalsAsync(QueryObject queryObject);
        Task<BaseResponse<ProposaResponse>> GetDetailProposalAsync(int id);
        //Task<BaseResponse<BaseResponseId>> CreateProposalAsync(AcademicYearResponse academicYear);
        //Task<BaseResponse<BaseResponseId>> UpdateProposalAsync(AcademicYearResponse academicYear);
        //Task<BaseResponse<BaseResponseId>> DeleteProposalAsync(int yearId);
    }
}
