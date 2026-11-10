using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Common;
using SchoolAdmission.Api.Dtos;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public ClassesController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _admissionService.CreateClassAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllClasses()
    {
        var result = await _admissionService.GetAllClassesAsync();
        return Ok(result);
    }

    [HttpGet("{classId}")]
    public async Task<IActionResult> GetClassById(string classId)
    {
        var result = await _admissionService.GetClassByIdAsync(classId);
        return Ok(result);
    }

    [HttpPost("{classId}/holds")]
    public async Task<IActionResult> CreateHold(string classId)
    {
        var studentId = StudentIdValidator.ValidateAndGet(Request.Headers);
        var result = await _admissionService.CreateHoldAsync(classId, studentId);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
