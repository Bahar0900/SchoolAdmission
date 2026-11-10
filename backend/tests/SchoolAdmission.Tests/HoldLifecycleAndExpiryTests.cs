using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Dtos;
using Xunit;

namespace SchoolAdmission.Tests;

public class HoldLifecycleAndExpiryTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public HoldLifecycleAndExpiryTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CancelHold_ReleasesSeatImmediately()
    {
        var student = "20152691234567894";

        // 1. Create class with 2 seats
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class 4", 2));
        var classData = await classRes.Content.ReadFromJsonAsync<CreateClassResponse>();

        // 2. Create hold
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classData!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", student);
        var holdRes = await _client.SendAsync(holdReq);
        var holdData = await holdRes.Content.ReadFromJsonAsync<CreateHoldResponse>();

        // 3. Check seats: held=1, available=1
        var classDetail1 = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classData.Id}");
        Assert.Equal(1, classDetail1!.HeldSeats);
        Assert.Equal(1, classDetail1.AvailableSeats);

        // 4. Cancel hold
        var cancelReq = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{holdData!.HoldId}");
        cancelReq.Headers.Add("X-Student-Id", student);
        var cancelRes = await _client.SendAsync(cancelReq);
        Assert.Equal(HttpStatusCode.NoContent, cancelRes.StatusCode);

        // 5. Check seats again: held=0, available=2
        var classDetail2 = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classData.Id}");
        Assert.Equal(0, classDetail2!.HeldSeats);
        Assert.Equal(2, classDetail2.AvailableSeats);
    }

    [Fact]
    public async Task ExpiredHold_AutomaticallyFreesSeat_AndCannotBeConfirmed()
    {
        var student = "20152691234567895";

        // 1. Create class with 1 seat
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class Expiry Test", 1));
        var classData = await classRes.Content.ReadFromJsonAsync<CreateClassResponse>();

        // 2. Create hold
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classData!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", student);
        var holdRes = await _client.SendAsync(holdReq);
        var holdData = await holdRes.Content.ReadFromJsonAsync<CreateHoldResponse>();

        // 3. Manually simulate expiration by setting ExpiresAt in the past
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var holdInDb = await db.Holds.FindAsync(holdData!.HoldId);
            Assert.NotNull(holdInDb);
            holdInDb.ExpiresAt = DateTime.UtcNow.AddMinutes(-5); // Expired 5 minutes ago
            await db.SaveChangesAsync();
        }

        // 4. Verify class seat count: Available seats should automatically be 1 without any manual API call!
        var classDetail = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classData.Id}");
        Assert.Equal(0, classDetail!.HeldSeats);
        Assert.Equal(1, classDetail.AvailableSeats);

        // 5. Verify GET /holds/{id} reports status as EXPIRED
        var holdStatus = await _client.GetFromJsonAsync<HoldStatusResponse>($"/holds/{holdData!.HoldId}");
        Assert.Equal("EXPIRED", holdStatus!.Status);

        // 6. Attempting to confirm this expired hold must be rejected with 400 Bad Request
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData.HoldId}/confirm");
        confirmReq.Headers.Add("X-Student-Id", student);
        var confirmRes = await _client.SendAsync(confirmReq);

        Assert.Equal(HttpStatusCode.BadRequest, confirmRes.StatusCode);
    }

    [Fact]
    public async Task GetMyHolds_ReturnsAllStudentHolds()
    {
        var student = "20152691234567896";

        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class 5", 5));
        var classData = await classRes.Content.ReadFromJsonAsync<CreateClassResponse>();

        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classData!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", student);
        await _client.SendAsync(holdReq);

        var myHoldsReq = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        myHoldsReq.Headers.Add("X-Student-Id", student);
        var myHoldsRes = await _client.SendAsync(myHoldsReq);

        Assert.Equal(HttpStatusCode.OK, myHoldsRes.StatusCode);
        var list = await myHoldsRes.Content.ReadFromJsonAsync<List<StudentHoldItemResponse>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.Equal("Class 5", list[0].ClassName);
    }
}
