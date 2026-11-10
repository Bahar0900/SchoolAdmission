using SchoolAdmission.Api.Models.Enums;

namespace SchoolAdmission.Api.Models.Entities;

public class SeatHold
{
    public string Id { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public SchoolClass? SchoolClass { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public HoldStatus Status { get; set; } = HoldStatus.HELD;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
