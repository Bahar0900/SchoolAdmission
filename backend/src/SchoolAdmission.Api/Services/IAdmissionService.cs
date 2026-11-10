using SchoolAdmission.Api.Dtos;

namespace SchoolAdmission.Api.Services;

public interface IAdmissionService
{
    Task<CreateClassResponse> CreateClassAsync(CreateClassRequest request);
    Task<IEnumerable<ClassDetailResponse>> GetAllClassesAsync();
    Task<ClassDetailResponse> GetClassByIdAsync(string classId);

    Task<CreateHoldResponse> CreateHoldAsync(string classId, string studentId);
    Task<HoldStatusResponse> GetHoldByIdAsync(string holdId);
    Task<ConfirmHoldResponse> ConfirmHoldAsync(string holdId, string studentId);
    Task CancelHoldAsync(string holdId, string studentId);

    Task<IEnumerable<StudentHoldItemResponse>> GetStudentHoldsAsync(string studentId);
    Task CleanExpiredHoldsAsync();
}
