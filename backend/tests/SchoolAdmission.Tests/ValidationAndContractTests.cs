using System.Net;
using System.Net.Http.Json;
using SchoolAdmission.Api.Dtos;
using Xunit;

namespace SchoolAdmission.Tests;

public class ValidationAndContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ValidationAndContractTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateClass_WithValidPayload_Returns201AndCorrectContract()
    {
        var request = new CreateClassRequest("Class 6 - Morning Shift", 120);
        var response = await _client.PostAsJsonAsync("/classes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<CreateClassResponse>();
        Assert.NotNull(content);
        Assert.StartsWith("c_", content.Id);
        Assert.Equal("Class 6 - Morning Shift", content.Name);
        Assert.Equal(120, content.TotalSeats);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890")] // 10 digits
    [InlineData("201526912345678901")] // 18 digits
    [InlineData("2015269123456789A")] // contains letter
    public async Task CreateHold_WithInvalidStudentId_Returns400BadRequest(string invalidId)
    {
        // 1. Create a class
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class 1", 10));
        var classData = await classRes.Content.ReadFromJsonAsync<CreateClassResponse>();
        Assert.NotNull(classData);

        // 2. Request hold with invalid or missing header
        var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classData.Id}/holds");
        if (!string.IsNullOrEmpty(invalidId))
        {
            request.Headers.Add("X-Student-Id", invalidId);
        }

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmHold_ByDifferentStudent_Returns403Forbidden()
    {
        var studentA = "20152691234567891";
        var studentB = "20152691234567892";

        // 1. Create class
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class 2", 10));
        var classData = await classRes.Content.ReadFromJsonAsync<CreateClassResponse>();

        // 2. Student A creates hold
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classData!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", studentA);
        var holdRes = await _client.SendAsync(holdReq);
        Assert.Equal(HttpStatusCode.Created, holdRes.StatusCode);
        var holdData = await holdRes.Content.ReadFromJsonAsync<CreateHoldResponse>();

        // 3. Student B attempts to confirm Student A's hold
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData!.HoldId}/confirm");
        confirmReq.Headers.Add("X-Student-Id", studentB);
        var confirmRes = await _client.SendAsync(confirmReq);

        Assert.Equal(HttpStatusCode.Forbidden, confirmRes.StatusCode);
    }

    [Fact]
    public async Task ConfirmHold_MultipleTimes_IsIdempotentAndIncrementsOnlyOnce()
    {
        var student = "20152691234567893";

        // 1. Create class
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class 3", 10));
        var classData = await classRes.Content.ReadFromJsonAsync<CreateClassResponse>();

        // 2. Student creates hold
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classData!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", student);
        var holdRes = await _client.SendAsync(holdReq);
        var holdData = await holdRes.Content.ReadFromJsonAsync<CreateHoldResponse>();

        // 3. Confirm 1st time
        var confirmReq1 = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData!.HoldId}/confirm");
        confirmReq1.Headers.Add("X-Student-Id", student);
        var confirmRes1 = await _client.SendAsync(confirmReq1);
        Assert.Equal(HttpStatusCode.OK, confirmRes1.StatusCode);

        // 4. Confirm 2nd time (should still return 200 without error)
        var confirmReq2 = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData.HoldId}/confirm");
        confirmReq2.Headers.Add("X-Student-Id", student);
        var confirmRes2 = await _client.SendAsync(confirmReq2);
        Assert.Equal(HttpStatusCode.OK, confirmRes2.StatusCode);

        // 5. Verify confirmedSeats in class is exactly 1
        var classDetail = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classData.Id}");
        Assert.NotNull(classDetail);
        Assert.Equal(1, classDetail.ConfirmedSeats);
        Assert.Equal(0, classDetail.HeldSeats);
        Assert.Equal(9, classDetail.AvailableSeats);
    }

    [Fact]
    public async Task ConfirmAdmission_InOneClass_CancelsOtherHolds_AndPreventsFurtherAdmissions()
    {
        var student = "20152691234567899";

        // 1. Create Class A and Class B
        var classARes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class A - Morning", 5));
        var classAData = await classARes.Content.ReadFromJsonAsync<CreateClassResponse>();

        var classBRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class B - Day", 5));
        var classBData = await classBRes.Content.ReadFromJsonAsync<CreateClassResponse>();

        // 2. Student holds a seat in Class A and a seat in Class B
        var holdAReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classAData!.Id}/holds");
        holdAReq.Headers.Add("X-Student-Id", student);
        var holdARes = await _client.SendAsync(holdAReq);
        var holdAData = await holdARes.Content.ReadFromJsonAsync<CreateHoldResponse>();

        var holdBReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classBData!.Id}/holds");
        holdBReq.Headers.Add("X-Student-Id", student);
        var holdBRes = await _client.SendAsync(holdBReq);
        var holdBData = await holdBRes.Content.ReadFromJsonAsync<CreateHoldResponse>();

        // 3. Confirm hold in Class A
        var confirmAReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdAData!.HoldId}/confirm");
        confirmAReq.Headers.Add("X-Student-Id", student);
        var confirmARes = await _client.SendAsync(confirmAReq);
        Assert.Equal(HttpStatusCode.OK, confirmARes.StatusCode);

        // 4. Verify Class B's hold was automatically cancelled and its seat released
        var holdBStatus = await _client.GetFromJsonAsync<HoldStatusResponse>($"/holds/{holdBData!.HoldId}");
        Assert.Equal("CANCELLED", holdBStatus!.Status);

        var classBDetail = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classBData.Id}");
        Assert.Equal(0, classBDetail!.HeldSeats);
        Assert.Equal(0, classBDetail.ConfirmedSeats);
        Assert.Equal(5, classBDetail.AvailableSeats); // seat was released back!

        // 5. Attempting to confirm hold B now must fail
        var confirmBReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdBData.HoldId}/confirm");
        confirmBReq.Headers.Add("X-Student-Id", student);
        var confirmBRes = await _client.SendAsync(confirmBReq);
        Assert.True(confirmBRes.StatusCode == HttpStatusCode.BadRequest || confirmBRes.StatusCode == HttpStatusCode.Conflict);

        // 6. Attempting to create a new hold in any other class must fail with 409 Conflict
        var newHoldReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classBData.Id}/holds");
        newHoldReq.Headers.Add("X-Student-Id", student);
        var newHoldRes = await _client.SendAsync(newHoldReq);
        Assert.Equal(HttpStatusCode.Conflict, newHoldRes.StatusCode);
    }
}
