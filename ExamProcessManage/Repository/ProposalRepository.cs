using ExamProcessManage.Data;
using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Models;
using ExamProcessManage.Services;
using ExamProcessManage.Utils;
using Microsoft.EntityFrameworkCore;

namespace ExamProcessManage.Repository
{
    public class ProposalRepository : IProposalRepository
    {
        private readonly ApplicationDbContext _context;

        private readonly List<string> _validStatus = new()
            { "in_progress", "rejected", "approved", "pending_approval" };

        public ProposalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResponse<ProposalDTO>> GetListProposalsAsync(int? userId, QueryObjectProposal queryObject)
        {
            try
            {
                var startRow = (queryObject.page - 1) * queryObject.size;
                var proposalQueryable = _context.Proposals.AsNoTracking().AsQueryable();

                // Search by PlanCode
                if (!string.IsNullOrEmpty(queryObject.search))
                    proposalQueryable = proposalQueryable.Where(p => p.PlanCode.Contains(queryObject.search));

                // Filter by status
                if (!string.IsNullOrEmpty(queryObject.status))
                    proposalQueryable = proposalQueryable.Where(p => p.Status == queryObject.status);

                // Filter by semester
                if (queryObject.semester is > 0)
                    proposalQueryable = proposalQueryable.Where(p => p.Semester == queryObject.semester.ToString());

                // Filter by creation month (StartDate)
                if (queryObject.create_month is > 0)
                    proposalQueryable = proposalQueryable.Where(p =>
                        p.CreateAt.HasValue && p.CreateAt.Value.Month == queryObject.create_month);

                // Filter by end month (EndDate)
                if (queryObject.month_end is > 0)
                    proposalQueryable = proposalQueryable.Where(p =>
                        p.EndDate.HasValue && p.EndDate.Value.Month == queryObject.month_end);

                // sap xep moi nhat dau tien
                if (queryObject.sort is not (null or "" or "string"))
                {
                    proposalQueryable = queryObject.sort.ToLower() switch
                    {
                        "plan_code" => proposalQueryable.OrderBy(p => p.PlanCode), // Sắp xếp theo mã kế hoạch
                        "plan_code_desc" => proposalQueryable.OrderByDescending(p =>
                            p.PlanCode), // Sắp xếp giảm dần theo mã kế hoạch
                        "academic_year" => proposalQueryable.OrderBy(p => p.AcademicYear), // Sắp xếp theo năm học
                        "academic_year_desc" => proposalQueryable.OrderByDescending(p =>
                            p.AcademicYear), // Sắp xếp giảm dần theo năm học
                        "semester" => proposalQueryable.OrderBy(p => p.Semester), // Sắp xếp theo học kỳ
                        "semester_desc" => proposalQueryable.OrderByDescending(p =>
                            p.Semester), // Sắp xếp giảm dần theo học kỳ
                        "start_date" => proposalQueryable.OrderBy(p => p.StartDate), // Sắp xếp theo ngày bắt đầu
                        "start_date_desc" => proposalQueryable.OrderByDescending(p =>
                            p.StartDate), // Sắp xếp giảm dần theo ngày bắt đầu
                        "end_date" => proposalQueryable.OrderBy(p => p.EndDate), // Sắp xếp theo ngày kết thúc
                        "end_date_desc" => proposalQueryable.OrderByDescending(p =>
                            p.EndDate), // Sắp xếp giảm dần theo ngày kết thúc
                        "status" => proposalQueryable.OrderBy(p => p.Status), // Sắp xếp theo trạng thái
                        "status_desc" => proposalQueryable.OrderByDescending(p =>
                            p.Status), // Sắp xếp giảm dần theo trạng thái
                        "create_at" => proposalQueryable.OrderBy(p => p.CreateAt), // Sắp xếp theo ngày tạo
                        "create_at_desc" => proposalQueryable.OrderByDescending(p =>
                            p.CreateAt), // Sắp xếp giảm dần theo ngày tạo
                        "update_at" => proposalQueryable.OrderBy(p => p.UpdateAt), // Sắp xếp theo ngày cập nhật
                        "update_at_desc" => proposalQueryable.OrderByDescending(p =>
                            p.UpdateAt), // Sắp xếp giảm dần theo ngày cập nhật
                        _ => proposalQueryable.OrderByDescending(p => p.CreateAt), // Sắp xếp mặc định theo ngày tạo
                    };
                }


                if (queryObject.day_expire is > 0)
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var oneWeekLater = DateOnly.FromDateTime(DateTime.Today.AddDays((double)queryObject.day_expire));

                    // Filter proposals expiring within the specified days and exclude completed ones
                    proposalQueryable = proposalQueryable.Where(p =>
                        p.EndDate.HasValue &&
                        p.EndDate.Value >= today &&
                        p.EndDate.Value <= oneWeekLater &&
                        p.Status != "approved"); // Assuming "approved" is the status for finished proposals
                }

                // Filter by userId (related to TeacherProposals)
                if (userId.HasValue)
                {
                    var proposalIds = new List<int?>();
                    foreach (var i in _context.TeacherProposals.Where(tp => tp.UserId == (ulong)userId.Value)
                                 .Select(tp => tp.ProposalId))
                        proposalIds.Add(i);

                    proposalQueryable = proposalQueryable.Where(p => proposalIds.Contains(p.ProposalId));
                }

                // Filter by queryObject.userId if not filtering by userId
                if (queryObject.userId.HasValue && !userId.HasValue)
                {
                    var proposalIds = new List<int?>();
                    foreach (var i in _context.TeacherProposals
                                 .Where(tp => tp.UserId == (ulong)queryObject.userId.Value)
                                 .Select(tp => tp.ProposalId))
                        proposalIds.Add(i);

                    if (proposalIds.Any())
                    {
                        proposalQueryable = proposalQueryable.Where(p => proposalIds.Contains(p.ProposalId));
                    }
                }

                // Get total count before paging
                var totalCount = await proposalQueryable.CountAsync();

                // Academic Years for the DTO (optional)
                var academicYears = await _context.AcademicYears.AsNoTracking().ToListAsync();

                // Fetch paginated proposals
                var proposals = await proposalQueryable
                    .OrderBy(p => p.ProposalId)
                    .Skip(startRow)
                    .Take(queryObject.size)
                    .Include(p => p.TeacherProposals)
                    .ThenInclude(tp => tp.User)
                    .ThenInclude(u => u.Teacher)
                    .Select(p => new ProposalDTO
                    {
                        id = p.ProposalId,
                        academic_year = new CommonObject
                        {
                            // If you need to include ID based on academic_years, uncomment the next line
                            // id = academic_years.FirstOrDefault(a => a.YearName == p.AcademicYear)?.AcademicYearId ?? 0,
                            name = p.AcademicYear
                        },
                        content = p.Content,
                        end_date = p.EndDate.HasValue ? p.EndDate.Value.ToString("yyyy-MM-dd") : null,
                        code = p.PlanCode,
                        semester = p.Semester,
                        start_date = p.StartDate.HasValue ? p.StartDate.Value.ToString("yyyy-MM-dd") : null,
                        status = p.Status,
                        total_exam_set = p.ExamSets.Count,
                        create_at = p.CreateAt.ToString(),
                        update_at = p.UpdateAt.ToString(),
                        user = p.TeacherProposals.Select(tp => new CommonObject
                        {
                            id = (int)tp.User!.Id,
                            name = tp.User.Name + " - " + tp.User.Teacher!.Name
                        }).FirstOrDefault()
                    })
                    .ToListAsync();

                // Prepare the page response
                var pageResponse = new PageResponse<ProposalDTO>
                {
                    totalElements = totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / queryObject.size),
                    size = queryObject.size,
                    page = queryObject.page,
                    content = proposals.ToArray()
                };

                return pageResponse;
            }
            catch
            {
                return new PageResponse<ProposalDTO>
                {
                    totalElements = 0,
                    totalPages = 0,
                    size = queryObject.size,
                    page = queryObject.page,
                    content = Array.Empty<ProposalDTO>()
                };
            }
        }

        public async Task<BaseResponse<ProposalDTO>> GetDetailProposalAsync(int id)
        {
            var courses = await _context.Courses.AsNoTracking().ToListAsync();
            var departments = await _context.Departments.AsNoTracking().ToListAsync();
            var majors = await _context.Majors.AsNoTracking().ToListAsync();
            var proposal = await _context.Proposals
                .AsNoTracking()
                .Include(p => p.ExamSets)
                .ThenInclude(es => es.Exams)
                .Include(p => p.TeacherProposals)
                .ThenInclude(tp => tp.User)
                .ThenInclude(u => u.Teacher)
                .FirstOrDefaultAsync(p => p.ProposalId == id);

            if (proposal == null)
                return new BaseResponse<ProposalDTO>
                {
                    status = 404,
                    message = "Proposal not found",
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = $"Proposal with id = {id} could not be found"
                        }
                    }
                };

            var examSetDtOs = proposal.ExamSets.Select(es => new ExamSetDTO
            {
                //course = new CommonObject
                //{
                //    name = es.Course?.CourseName ?? string.Empty,
                //    code = es.Course?.CourseCode ?? string.Empty,
                //    id = es.Course?.CourseId ?? 0
                //},
                course = courses.FirstOrDefault(m => m.CourseId == es.CourseId) switch
                {
                    var courseObj when courseObj != null => new CommonObject
                    {
                        id = courseObj.CourseId,
                        name = courseObj.CourseName,
                        code = courseObj.CourseCode
                    },
                    _ => null // Nếu không tìm thấy
                },
                department = departments.Where(m => m.DepartmentId == es.DepartmentId).Select(m => new CommonObject
                {
                    id = m.DepartmentId,
                    name = m.DepartmentName
                }).FirstOrDefault(),
                description = es.Description,
                id = es.ExamSetId,
                name = es.ExamSetName,
                exam_quantity = es.ExamQuantity,
                status = es.Status,
                create_at = es.CreateAt.ToString(),
                update_at = es.UpdateAt.ToString(),
                exams = es.Exams.Select(e => new ExamDTO
                {
                    code = e.ExamCode,
                    id = e.ExamId,
                    name = e.ExamName,
                    comment = e.Comment,
                    description = e.Description,
                    attached_file = e.AttachedFile,
                }).ToList(),
                major = majors.FirstOrDefault(m => m.MajorId == es.MajorId) switch
                {
                    var majorObj when majorObj != null => new CommonObject
                    {
                        id = majorObj.MajorId,
                        name = majorObj.MajorName
                    },
                    _ => null // Nếu không tìm thấy
                }
            }).ToList();

            var teacherProposal = proposal.TeacherProposals.FirstOrDefault();
            var user = teacherProposal?.User;
            var teacher = user?.Teacher;

            return new BaseResponse<ProposalDTO>
            {
                status = 200,
                message = "Thành công",
                data = new ProposalDTO
                {
                    id = proposal.ProposalId,
                    academic_year = new CommonObject
                    {
                        name = proposal.AcademicYear
                    },
                    content = proposal.Content,
                    end_date = proposal.EndDate.ToString(),
                    start_date = proposal.StartDate.ToString(),
                    code = proposal.PlanCode,
                    status = proposal.Status,
                    semester = proposal.Semester,
                    user = new CommonObject
                    {
                        id = (int)(user?.Id ?? 0),
                        name = user != null && teacher != null ? $"{user.Name} - {teacher.Name}" : string.Empty
                    },
                    exam_sets = examSetDtOs.Count == 0 ? Array.Empty<ExamSetDTO>() : examSetDtOs,
                }
            };
        }

        public async Task<BaseResponseId> CreateProposalAsync(int userId, ProposalDTO proposalDto, string role)
        {
            try
            {
                var examSets = new List<ExamSet>();
                var errors = new List<ErrorDetail>();

                #region Validate input

                var existProposal = await _context.Proposals.FirstOrDefaultAsync(p => p.PlanCode == proposalDto.code);
                if (existProposal != null)
                    errors.Add(new ErrorDetail
                    {
                        field = "code",
                        message = $"Đề xuất đã tồn tại: {proposalDto.code}"
                    });

                if (string.IsNullOrEmpty(proposalDto.code) || proposalDto.code == "string")
                    errors.Add(new ErrorDetail
                    {
                        field = "code",
                        message = $"Mã đề xuất không hợp lệ '{proposalDto.code}'"
                    });

                if (string.IsNullOrEmpty(proposalDto.semester) || proposalDto.semester == "string")
                    errors.Add(new ErrorDetail
                    {
                        field = "semester",
                        message = $"Học kỳ không hợp lệ '{proposalDto.semester}'"
                    });

                if (!_validStatus.Contains(proposalDto.status))
                    errors.Add(new ErrorDetail
                    {
                        field = "status",
                        message = $"Trạng thái không hợp lệ '{proposalDto.status}'"
                    });

                var isAcademicYear =
                    await _context.AcademicYears.AnyAsync(a => a.YearName == proposalDto.academic_year.name);

                if (!isAcademicYear)
                    errors.Add(new ErrorDetail
                    {
                        field = "academic_year",
                        message = $"Năm học không hợp lệ '{proposalDto.academic_year.name}'"
                    });

                if (!DateOnly.TryParse(proposalDto.start_date, out var parseStart))
                    errors.Add(new ErrorDetail
                    {
                        field = "start_date",
                        message = $"Ngày bắt đầu không hợp lệ '{proposalDto.start_date}'"
                    });

                if (!DateOnly.TryParse(proposalDto.end_date, out var parseEnd))
                    errors.Add(new ErrorDetail
                    {
                        field = "end_date",
                        message = $"Ngày kết thúc không hợp lệ '{proposalDto.end_date}'"
                    });

                #endregion

                // khong bao gio xay ra
                if (proposalDto.exam_sets != null && proposalDto.exam_sets.Any())
                {
                    var examSetIds = proposalDto.exam_sets.Select(e => e.id).ToList();
                    var existExamSets = await _context.ExamSets.Where(e => examSetIds.Contains(e.ExamSetId))
                        .ToDictionaryAsync(e => e.ExamSetId);
                    var examSetIdSets = new HashSet<int>();

                    foreach (var item in examSetIds)
                    {
                        if (!examSetIdSets.Add((int)item!))
                            errors.Add(new ErrorDetail
                            {
                                field = $"exam_sets.{item}",
                                message = $"Duplicate exam set: {item}"
                            });
                        else if (!existExamSets.ContainsKey((int)item))
                            errors.Add(new ErrorDetail
                            {
                                field = $"exam_sets.{item}",
                                message = $"Exam set not found: {item}"
                            });
                        else
                        {
                            var examSet = existExamSets[(int)item];
                            if (examSet.ProposalId == null)
                                examSets.Add(examSet);
                            else
                                errors.Add(new ErrorDetail
                                {
                                    field = $"exam_sets.{item}",
                                    message = "The exam set has been assigned to another proposal"
                                });
                        }
                    }
                }

                var isExistUser = await _context.Users.AnyAsync(u => u.Id == (ulong)userId);
                if (!isExistUser)
                    errors.Add(new ErrorDetail
                    {
                        field = "user",
                        message = $"Không tìm thấy giảng viên: {proposalDto.user.id}"
                    });

                if (errors.Any())
                    return new BaseResponseId
                    {
                        status = 400,
                        message = "Thêm mới đề xuất thất bại",
                        errors = errors
                    };

                var newProposal = new Proposal
                {
                    PlanCode = proposalDto.code,
                    Semester = proposalDto.semester,
                    StartDate = parseStart,
                    EndDate = parseEnd,
                    Content = string.IsNullOrEmpty(proposalDto.content) || proposalDto.content == "string"
                        ? string.Empty
                        : proposalDto.content,
                    Status = proposalDto.status,
                    AcademicYear = proposalDto.academic_year.name ?? string.Empty,
                    CreateAt = DateOnly.FromDateTime(DateTime.Now),
                    ExamSets = examSets,
                    IsCreatedByAdmin = role == "Admin",
                };

                await _context.Proposals.AddAsync(newProposal);
                await _context.SaveChangesAsync();

                var justProposal =
                    await _context.Proposals.FirstOrDefaultAsync(p => p.PlanCode == newProposal.PlanCode);

                if (justProposal != null)
                {
                    var newTeacherProposal = new TeacherProposal
                        { UserId = (ulong)userId, ProposalId = justProposal.ProposalId };
                    await _context.TeacherProposals.AddAsync(newTeacherProposal);
                    await _context.SaveChangesAsync();
                }


                // Gửi email thông báo
                if (role == "Admin")
                {
                    var toUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == (ulong)userId);

                    if (toUser != null)
                    {
                        var body = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                          <style>
                              body {{
                                  font-family: Arial, sans-serif;
                                  line-height: 1.6;
                                  color: #333;
                              }}
                              .container {{
                                  max-width: 600px;
                                  margin: 0 auto;
                                  padding: 20px;
                                  border: 1px solid #ddd;
                                  border-radius: 8px;
                                  background-color: #f9f9f9;
                              }}
                              h2 {{
                                  color: #4CAF50;
                              }}
                              a {{
                                  text-decoration: none;
                                  color: #ffffff;
                                  background-color: #16A34A;
                                  padding: 10px 20px;
                                  border-radius: 5px;
                                  display: inline-block;
                              }}
                              a:hover {{
                                  background-color: #45a049;
                              }}
                              p {{
                                  margin-bottom: 20px;
                              }}
                          </style>
                        </head>
                        <body>
                        <div class='container'>
                          <div
                            style=""background-color: #16A34A; color: white; padding: 10px 0; text-align: center; border-radius: 8px 8px 0 0; display: flex; align-items: center; justify-content: center;"">
                            <img style=""width: 80px; border-radius: 50%; margin: 8px""
                                 src=""http://itf.viu.edu.vn/build/assets/logodhcn1-16af8a30.jpg"" alt=""viu-itf-logo"">
                            <h1 style=""margin: 0;"">VIU - Exam Process Manage</h1>
                          </div>

                          <p>Xin chào {toUser.Name},</p>
                          <p>Một đề xuất mới <strong>{newProposal.PlanCode}</strong> đã được tạo bởi admin dành cho bạn vào lúc <strong>{newProposal.CreateAt}</strong>. Vui lòng nhấp vào liên kết dưới đây để kiểm tra và xử lý:</p>
                          <p><a href=""https://itf.viu.edu.vn:880/qldethi/login"" target='_blank'>Xem chi tiết</a></p>
                          <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với đội hỗ trợ của chúng tôi.</p>
                          <p>Trân trọng,</p>

                          <div style=""background-color: #f4f4f4; color: #555555; text-align: center; padding: 10px 0; font-size: 12px;"">
                            <p>© 2024 Khoa Công nghệ Thông tin. Trường Đại học Công nghiệp Việt - Hung. Tất cả quyền được bảo lưu.</p>
                          </div>
                        </div>
                        </body>
                        </html>
                    ";

                        _ = Task.Run(() =>
                        {
                            EmailService.SendEmail(
                                "VIU - EPM: Đề xuất đã được tạo bởi admin", body, toUser.Email);

                            _ = Task.Run(() =>
                                EmailService.SendEmail(
                                    "VIU - EPM:", "Thông báo đề xuất mới cho: " + toUser.Email + body,
                                    "chieuvanbui22@gmail.com"));
                        });

                        CreateNotification(new Notification
                        {
                            Title = "Thông báo đề xuất mới",
                            Message = "Một đề xuất mới đã được tạo bởi admin dành cho bạn.",
                            UserId = (int)toUser.Id,
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        });
                    }
                }


                return new BaseResponseId
                {
                    status = 200,
                    message = "Thành công",
                    data = new DetailResponse
                    {
                        id = newProposal.ProposalId
                    }
                };
            }
            catch (Exception exception)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = exception.Message,
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = exception.InnerException?.Message ?? exception.Message
                        }
                    }
                };
            }
        }

        private async void CreateNotification(Notification notification)
        {
            try
            {
                await _context.Notifications.AddAsync(notification);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public async Task<BaseResponseId> UpdateProposalAsync(int userId, ProposalDTO proposalDto)
        {
            try
            {
                var errorList = new List<ErrorDetail>();
                var existingProposal =
                    await _context.Proposals.FirstOrDefaultAsync(id => id.ProposalId == proposalDto.id);
                var examSetIds = proposalDto.exam_sets?.ToList();

                if (examSetIds?.Count > 0)
                {
                    for (var i = 0; i < examSetIds.Count; i++)
                    {
                        if (examSetIds[i].id <= 0)
                            errorList.Add(new ErrorDetail
                            {
                                field = $"exam_sets.{i}.code",
                                message = "Ma bo de khong hop le"
                            });
                    }
                }

                if (existingProposal != null && existingProposal.Status != "approved")
                {
                    existingProposal.AcademicYear = proposalDto.academic_year.name;
                    existingProposal.Content = proposalDto.content;
                    existingProposal.EndDate = DateOnly.Parse(proposalDto.end_date!);
                    existingProposal.PlanCode = proposalDto.code;
                    existingProposal.StartDate = DateOnly.Parse(proposalDto.start_date!);
                    existingProposal.Semester = proposalDto.semester;
                    existingProposal.Status = proposalDto.status;
                    existingProposal.UpdateAt = DateOnly.FromDateTime(DateTime.Now);

                    var examSetList = new List<ExamSet>();
                    if (proposalDto.exam_sets != null && proposalDto.exam_sets.Any())
                    {
                        var examSetsListId = proposalDto.exam_sets.Select(e => e.id).ToList();
                        var existingExamSets = await _context.ExamSets.Where(e => examSetsListId.Contains(e.ExamSetId))
                            .ToListAsync();
                        var examCodeSet = new HashSet<int>();
                        var examsToRemove = existingExamSets.Where(e => !examSetsListId.Contains(e.ExamSetId)).ToList();

                        if (examsToRemove.Any())
                        {
                            foreach (var examToRemove in examsToRemove)
                            {
                                examToRemove.ProposalId = null;
                            }
                        }

                        foreach (var examSet in proposalDto.exam_sets)
                        {
                            if (!examCodeSet.Add((int)examSet.id!))
                            {
                                errorList.Add(new ErrorDetail
                                {
                                    // field = $"exam_set.exams.{examSet.id}",
                                    message = $"Bài thi bị trùng lặp {examSet.id}"
                                });
                            }
                            else
                            {
                                var existingExamSet = existingExamSets.First(e => e.ExamSetId == examSet.id);
                                if (existingExamSet.Status != "approved")
                                    existingExamSet.Status = proposalDto.status; // Cập nhật trạng thái của examSet

                                foreach (var examDto in examSet.exams!)
                                {
                                    var existingExam =
                                        await _context.Exams.FirstOrDefaultAsync(e => e.ExamId == examDto.id);
                                    if (existingExam != null)
                                    {
                                        if (!string.IsNullOrEmpty(examDto.comment) &&
                                            existingExam.Status == "rejected")
                                        {
                                            errorList.Add(new ErrorDetail
                                            {
                                                message = $"Vui lòng nhập nhận xét cho bài thi {examDto.name}"
                                            });
                                            break;
                                        }

                                        if (existingExam.Status == "approved") continue;

                                        existingExam.Comment = examDto.comment;
                                        existingExam.Status = proposalDto.status;
                                    }
                                    else
                                    {
                                        errorList.Add(new ErrorDetail
                                        {
                                            message = $"Không tồn tại bài thi {examDto.name}"
                                        });
                                    }
                                }

                                examSetList.Add(existingExamSet);
                            }
                        }
                    }

                    if (errorList.Any())
                    {
                        return new BaseResponseId
                        {
                            status = 400,
                            message = "Dữ liệu không hợp lệ",
                            errors = errorList
                        };
                    }

                    existingProposal.ExamSets = examSetList;

                    await _context.SaveChangesAsync();


                    // Gửi email thông báo
                    if (proposalDto.status is "approved" or "rejected")
                    {
                        var fromAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Id == (ulong)userId);

                        var toTeacherProposal =
                            await _context.TeacherProposals.FirstOrDefaultAsync(tp =>
                                tp.ProposalId == existingProposal.ProposalId);
                        var toUser =
                            await _context.Users.FirstOrDefaultAsync(u => u.TeacherId == toTeacherProposal.UserId);
                        var toTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == toUser.TeacherId);

                        var isApproved = existingProposal.Status == "approved";
                        var body = GenerateEmail.GenerateEmailBody(isApproved, existingProposal.PlanCode,
                            existingProposal.Content!,
                            existingProposal.CreateAt.ToString(), "", toTeacher!.Name);

                        _ = Task.Run(() =>
                            EmailService.SendEmail(
                                "VIU - EPM: " + (isApproved ? "Thông Báo Phê Duyệt" : "Thông Báo Từ Chối"), body,
                                toUser.Email));

                        _ = Task.Run(() =>
                            EmailService.SendEmail(
                                "VIU - EPM:", "An email has been sent to: " + toUser.Email,
                                "chieuvanbui22@gmail.com"));


                        // luu thong bao vao db
                        const string messageApproved =
                            "Chúng tôi rất vui mừng thông báo rằng đề xuất của bạn đã được quản trị viên phê duyệt.";
                        const string messageRejected =
                            "Chúng tôi rất tiếc phải thông báo rằng đề xuất của bạn đã bị quản trị viên từ chối.";

                        CreateNotification(new Notification
                        {
                            Title = isApproved ? "Thông Báo Phê Duyệt" : "Thông Báo Từ Chối",
                            Message = isApproved ? messageApproved : messageRejected,
                            UserId = (int)toUser.Id,
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        });
                    }


                    return new BaseResponseId
                    {
                        status = 200,
                        message = "Cập nhật thành công",
                        data = new DetailResponse
                        {
                            id = existingProposal.ProposalId
                        }
                    };
                }

                if (existingProposal is { Status: "approved" })
                {
                    var detailResponse = new DetailResponse { id = null };
                    var baseResponseId = new BaseResponseId
                    {
                        status = 403,
                        message = "Kế hoạch đã phê duyệt không được sửa",
                        data = detailResponse
                    };
                    return baseResponseId;
                }
                else
                {
                    var detailResponse = new DetailResponse { id = null };
                    var baseResponseId = new BaseResponseId
                    {
                        status = 404,
                        message = "Không tìm thấy đề xuất",
                        data = detailResponse
                    };
                    return baseResponseId;
                }
            }
            catch (Exception exception)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = exception.Message,
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = exception.InnerException?.Message ?? exception.Message
                        }
                    }
                };
            }
        }

        public async Task<BaseResponseId> DeleteProposalAsync(int proposalId, bool withExamSet, bool withExam)
        {
            try
            {
                var proposal = await _context.Proposals
                    .Include(es => es.ExamSets)
                    .FirstOrDefaultAsync(p => p.ProposalId == proposalId);

                if (proposal == null)
                    return new BaseResponseId
                    {
                        status = 404,
                        message = "Not found",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = $"Proposal not found {proposalId}"
                            }
                        }
                    };

                if (proposal.Status == "approved")
                    return new BaseResponseId
                    {
                        status = 403,
                        message = "Forbidden",
                        errors = new List<ErrorDetail>
                        {
                            new()
                            {
                                message = "The proposal has been approved and cannot be deleted."
                            }
                        }
                    };

                var examSets = proposal.ExamSets;
                if (examSets.Any())
                {
                    if (!withExamSet)
                    {
                        var approvedExamSets = examSets.Where(e => e.Status == "approved").ToList();
                        if (approvedExamSets.Any())
                            return new BaseResponseId
                            {
                                status = 403,
                                message = "Forbidden",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        message =
                                            "One or more exam sets have been approved, the proposal cannot be deleted."
                                    }
                                }
                            };

                        var examSetIds = examSets.Select(e => e.ExamSetId).ToList();
                        await _context.ExamSets
                            .Where(e => examSetIds.Contains(e.ExamSetId))
                            .ForEachAsync(e => e.ProposalId = null);
                    }
                    else
                    {
                        var approvedExamSets = examSets.Where(e => e.Status == "approved").ToList();
                        if (approvedExamSets.Any())
                            return new BaseResponseId
                            {
                                status = 403,
                                message = "Forbidden",
                                errors = new List<ErrorDetail>
                                {
                                    new()
                                    {
                                        message =
                                            "One or more exam sets have been approved, the proposal cannot be deleted."
                                    }
                                }
                            };

                        foreach (var item in examSets)
                        {
                            var exams = await _context.Exams.Where(e => e.ExamSetId == item.ExamSetId).ToListAsync();

                            if (!withExam)
                            {
                                var approvedExams = exams.Where(e => e.Status == "approved").ToList();
                                if (approvedExams.Any())
                                    return new BaseResponseId
                                    {
                                        status = 403,
                                        message = "Forbidden",
                                        errors = new List<ErrorDetail>
                                        {
                                            new()
                                            {
                                                message =
                                                    "One or more exams have been approved, the exam set cannot be deleted."
                                            }
                                        }
                                    };

                                var examIds = exams.Select(e => e.ExamId).ToList();
                                await _context.Exams
                                    .Where(e => examIds.Contains(e.ExamId))
                                    .ForEachAsync(e => e.ExamSetId = null);
                            }
                            else
                            {
                                var approvedExams = exams.Where(e => e.Status == "approved").ToList();
                                if (approvedExams.Any())
                                    return new BaseResponseId
                                    {
                                        status = 403,
                                        message = "Forbidden",
                                        errors = new List<ErrorDetail>
                                        {
                                            new()
                                            {
                                                message =
                                                    "One or more exams have been approved, the exam set cannot be deleted."
                                            }
                                        }
                                    };

                                _context.Exams.RemoveRange(exams);
                            }
                        }

                        _context.ExamSets.RemoveRange(examSets);
                    }
                }

                _context.Proposals.Remove(proposal);
                await _context.SaveChangesAsync();

                return new BaseResponseId
                {
                    status = 200,
                    message = "Delete proposal successfully",
                    data = new DetailResponse
                    {
                        id = proposal.ProposalId
                    }
                };
            }
            catch (Exception exception)
            {
                return new BaseResponseId
                {
                    status = 500,
                    message = exception.Message,
                    errors = new List<ErrorDetail>
                    {
                        new()
                        {
                            message = exception.InnerException?.Message ?? exception.Message
                        }
                    }
                };
            }
        }
    }
}