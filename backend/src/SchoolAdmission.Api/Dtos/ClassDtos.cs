using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.Dtos;

public record CreateClassRequest(
    [Required(ErrorMessage = "Class name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Class name must be between 1 and 200 characters.")]
    string Name,

    [Range(1, 100000, ErrorMessage = "totalSeats must be at least 1.")]
    int TotalSeats
);

public record CreateClassResponse(
    string Id,
    string Name,
    int TotalSeats
);

public record ClassDetailResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats
);
