using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PottyTrain.Api.Tests;

public class ApiTests : IClassFixture<TestFactory>
{
    private readonly TestFactory _f;
    public ApiTests(TestFactory f) => _f = f;

    private async Task<(HttpClient Client, string ChildId)> Setup()
    {
        var c = _f.CreateClient();
        var reg = await c.PostAsJsonAsync("/api/auth/register", new { email = $"{Guid.NewGuid():N}@t.com", password = "password1", displayName = "T" });
        var token = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        c.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var child = await c.PostAsJsonAsync("/api/children", new { name = "Leo", birthDate = "2024-03-01" });
        return (c, (await child.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!);
    }

    [Theory]
    [InlineData("""{"type":"Pee","stoolType":3}""")]            // stool type on a wee
    [InlineData("""{"type":"Poop","stoolType":8}""")]           // out of range
    [InlineData("""{"type":"Poop","amount":50}""")]             // ml on a poop
    [InlineData("""{"type":"Fluid","tags":["bad tag"]}""")]     // invalid tag
    [InlineData("""{"type":"Food","colour":"Red"}""")]          // colour on food
    public async Task Rejects_inconsistent_logs(string json)
    {
        var (c, id) = await Setup();
        var r = await c.PostAsync($"/api/children/{id}/logs", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Stores_and_returns_intake_and_output_details_and_insights()
    {
        var (c, id) = await Setup();
        async Task Post(object body) => Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync($"/api/children/{id}/logs", body)).StatusCode);
        await Post(new { type = "Fluid", amount = 200, category = "water" });
        await Post(new { type = "Food", size = "Medium", category = "snack", tags = new[] { "Fibre", "fibre", "dairy" } });
        await Post(new { type = "Poop", inPotty = true, stoolType = 4, size = "Small", colour = "Normal" });

        var logs = await c.GetFromJsonAsync<JsonElement>($"/api/children/{id}/logs?type=Food");
        Assert.Equal("fibre,dairy", string.Join(',', logs[0].GetProperty("tags").EnumerateArray().Select(t => t.GetString())));

        var insights = await c.GetFromJsonAsync<JsonElement>($"/api/children/{id}/insights");
        Assert.Equal(200, insights.GetProperty("balance")[0].GetProperty("intakeMl").GetInt32());
        var pred = await c.GetAsync($"/api/children/{id}/prediction");
        Assert.Equal(HttpStatusCode.OK, pred.StatusCode);
    }
}
