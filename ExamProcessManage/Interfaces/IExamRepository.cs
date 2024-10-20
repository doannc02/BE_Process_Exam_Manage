﻿using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.RequestModels;

namespace ExamProcessManage.Interfaces
{
    public interface IExamRepository
    {
        Task<PageResponse<ExamDTO>> GetListExamsAsync(ExamRequestParams examRequest, int? userId);
        Task<BaseResponse<ExamDTO>> GetDetailExamAsync(int examId);
        Task<BaseResponse<List<DetailResponse>>> CreateExamsAsync(List<ExamDTO> examDTOs, int userId);
        Task<BaseResponseId> UpdateExamAsync(int userId, bool isAdmin, ExamDTO examDTO);
        Task<BaseResponseId> DeleteExamAsync(int userId, int examId);
    }
}
