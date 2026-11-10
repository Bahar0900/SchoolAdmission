using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Common;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public class MeController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public MeController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpGet("holds")]
    public async Task<IActionResult> GetMyHolds()
    {
        var studentId = StudentIdValidator.ValidateAndGet(Request.Headers);
        var result = await _admissionService.GetStudentHoldsAsync(studentId);
        return Ok(result);
    }
}
