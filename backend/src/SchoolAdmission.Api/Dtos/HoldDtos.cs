namespace SchoolAdmission.Api.Dtos;

public record CreateHoldResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    string ExpiresAt
);

public record HoldStatusResponse(
    string HoldId,
    string ClassId,
    string Status,
    string ExpiresAt
);

public record ConfirmHoldResponse(
    string HoldId,
    string Status
);

public record StudentHoldItemResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    string ExpiresAt
);

public record ErrorResponse(
    string Message,
    string? Detail = null
);
