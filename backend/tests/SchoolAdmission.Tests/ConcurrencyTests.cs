using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using SchoolAdmission.Api.Dtos;
using Xunit;

namespace SchoolAdmission.Tests;

public class ConcurrencyTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConcurrencyTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HighConcurrency_HoldsNeverExceedTotalSeats_UnderSimultaneousRequests()
    {
        // 1. Setup: Create a class with only 5 seats
        const int totalSeats = 5;
        const int concurrentStudentsCount = 50;

        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest("Class 6 - Concurrency Test", totalSeats));
        Assert.Equal(HttpStatusCode.Created, classRes.StatusCode);
        var classData = await classRes.Content.ReadFromJsonAsync<CreateClassResponse>();
        Assert.NotNull(classData);
        var classId = classData.Id;

        // 2. Prepare 50 concurrent requests with 50 distinct 17-digit student IDs
        var results = new ConcurrentBag<(HttpStatusCode StatusCode, CreateHoldResponse? Data)>();

        var tasks = Enumerable.Range(1, concurrentStudentsCount).Select(async i =>
        {
            var studentId = $"201526900000000{i:D2}"; // 17-digit unique student ID
            var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
            request.Headers.Add("X-Student-Id", studentId);

            var response = await _client.SendAsync(request);
            CreateHoldResponse? data = null;
            if (response.IsSuccessStatusCode)
            {
                data = await response.Content.ReadFromJsonAsync<CreateHoldResponse>();
            }

            results.Add((response.StatusCode, data));
        });

        // 3. Fire all 50 requests simultaneously
        await Task.WhenAll(tasks);

        // 4. Assert: Exactly 5 seats held, exactly 45 rejected with 409 Conflict
        var successList = results.Where(r => r.StatusCode == HttpStatusCode.Created).ToList();
        var conflictList = results.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();

        Assert.Equal(totalSeats, successList.Count);
        Assert.Equal(concurrentStudentsCount - totalSeats, conflictList.Count);

        // 5. Verify the class status from API
        var classDetail = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classId}");
        Assert.NotNull(classDetail);
        Assert.Equal(totalSeats, classDetail.TotalSeats);
        Assert.Equal(totalSeats, classDetail.HeldSeats);
        Assert.Equal(0, classDetail.AvailableSeats);
        Assert.True(classDetail.HeldSeats + classDetail.ConfirmedSeats <= classDetail.TotalSeats,
            "RULE 1 VIOLATION: Held + Confirmed exceeded TotalSeats!");

        // 6. Concurrently confirm all 5 held seats
        var confirmTasks = successList.Select(async s =>
        {
            var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{s.Data!.HoldId}/confirm");
            confirmReq.Headers.Add("X-Student-Id", s.Data.StudentId);
            var confirmRes = await _client.SendAsync(confirmReq);
            Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);
        });

        await Task.WhenAll(confirmTasks);

        // 7. Verify final confirmed seats count
        var finalClassDetail = await _client.GetFromJsonAsync<ClassDetailResponse>($"/classes/{classId}");
        Assert.NotNull(finalClassDetail);
        Assert.Equal(totalSeats, finalClassDetail.ConfirmedSeats);
        Assert.Equal(0, finalClassDetail.HeldSeats);
        Assert.Equal(0, finalClassDetail.AvailableSeats);
    }
}
