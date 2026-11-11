using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

try
{
    var assignmentsResponse = await http.GetAsync("/api/assignments");
    if (!assignmentsResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(assignmentsResponse);
        return;
    }

    var assignments = await assignmentsResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>() ?? [];
    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var assignment in assignments.Where(item => item.IsOpen))
    {
        Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
    }

    var selectedAssignmentId = assignments.FirstOrDefault(item => item.IsOpen)?.Id;
    if (selectedAssignmentId is null)
    {
        Console.WriteLine("Khong co bai tap nao dang mo.");
        return;
    }

    var detailResponse = await http.GetAsync($"/api/assignments/{selectedAssignmentId}");
    if (!detailResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(detailResponse);
        return;
    }
    var detail = await detailResponse.Content.ReadFromJsonAsync<AssignmentDto>();
    Console.WriteLine($"Bai tap duoc chon: {detail?.Title}");

    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");
    Console.WriteLine($"Location: {createResponse.Headers.Location}");

    // Đọc lại bằng URI được server trả trong Location.
    var location = createResponse.Headers.Location;
    if (location is not null)
    {
        var readBackResponse = await http.GetAsync(location);
        if (!readBackResponse.IsSuccessStatusCode)
        {
            await PrintApiErrorAsync(readBackResponse);
            return;
        }

        var readBack = await readBackResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai luot nop: {readBack?.StudentCode} - {readBack?.RepositoryUrl}");
    }

    // Thử lỗi HTTP; vẫn đọc ApiError thay vì làm chương trình sập.
    var invalidResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selectedAssignmentId}/submissions",
        new CreateSubmissionRequest("", ""));
    Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi toi API: {ex.Message}");
}

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    if (error is not null)
    {
        Console.WriteLine($"{error.Code}: {error.Message}");
    }
    else
    {
        Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}");
    }
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
