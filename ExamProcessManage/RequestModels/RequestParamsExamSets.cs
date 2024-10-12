using ExamProcessManage.Helpers;
using System.ComponentModel.DataAnnotations;

namespace ExamProcessManage.RequestModels
{
    public class RequestParamsExamSets : QueryObject
    {
        public string? stateExamSet { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public int? courseId { get; set; }
        public int? academicYearId { get; set; }
        public List<int>? exceptValues { get; set; }
        public Boolean? isParamAddProposal { get; set; } = false;
        public int? proposalId { get; set; }

    }
}
