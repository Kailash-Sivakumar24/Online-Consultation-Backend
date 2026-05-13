using System.Net;
using System.Net.Http.Json;
using ConsultationApi.Core.DTOs.Auth;
using ConsultationApi.Core.Enums;

namespace ConsultationApi.Tests.Integration;

public class AuthFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_Patient_Returns201()
    {
        var request = new RegisterRequestDto
        {
            Email = $"patient_{Guid.NewGuid()}@test.com",
            Password = "TestP@ss1",
            FullName = "Integration Patient",
            Role = UserRole.Patient,
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Female
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body?.AccessToken);
        Assert.NotNull(body?.RefreshToken);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var email = $"logintest_{Guid.NewGuid()}@test.com";
        var password = "TestP@ss1";

        await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = password,
            FullName = "Login Test",
            Role = UserRole.Patient,
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body?.AccessToken);
    }

    [Fact]
    public async Task UnauthenticatedRequest_Returns401()
    {
        var response = await Client.GetAsync("/api/appointments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctors_Returns200WithPaginationEnvelope()
    {
        var response = await Client.GetAsync("/api/doctors?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginationEnvelopeCheck>();
        Assert.NotNull(body);
        Assert.True(body!.Page >= 1);
        Assert.True(body.PageSize >= 1);
    }

    private class PaginationEnvelopeCheck
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
