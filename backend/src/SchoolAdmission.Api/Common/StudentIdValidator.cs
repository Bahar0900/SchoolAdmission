using System.Text.RegularExpressions;
using SchoolAdmission.Api.Exceptions;

namespace SchoolAdmission.Api.Common;

public static class StudentIdValidator
{
    private static readonly Regex StudentIdRegex = new(@"^\d{17}$", RegexOptions.Compiled);

    public static string ValidateAndGet(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue("X-Student-Id", out var values) || string.IsNullOrWhiteSpace(values))
        {
            throw new BadRequestException("Missing X-Student-Id header. Student 17-digit birth registration number is required.");
        }

        var studentId = values.ToString().Trim();
        if (!StudentIdRegex.IsMatch(studentId))
        {
            throw new BadRequestException("Invalid X-Student-Id header. It must be exactly a 17-digit numeric birth registration number.");
        }

        return studentId;
    }
}
