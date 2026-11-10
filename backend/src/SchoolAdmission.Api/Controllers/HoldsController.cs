using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Common;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public HoldsController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpGet("{holdId}")]
    public async Task<IActionResult> GetHoldById(string holdId)
    {
        var result = await _admissionService.GetHoldByIdAsync(holdId);
        return Ok(result);
    }

    [HttpPost("{holdId}/confirm")]
    public async Task<IActionResult> ConfirmHold(string holdId)
    {
        var studentId = StudentIdValidator.ValidateAndGet(Request.Headers);
        var result = await _admissionService.ConfirmHoldAsync(holdId, studentId);
        return Ok(result);
    }

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> CancelHold(string holdId)
    {
        var studentId = StudentIdValidator.ValidateAndGet(Request.Headers);
        await _admissionService.CancelHoldAsync(holdId, studentId);
        return NoContent();
    }
}
