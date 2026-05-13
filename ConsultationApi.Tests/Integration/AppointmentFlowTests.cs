using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ConsultationApi.Core.DTOs.Appointments;
using ConsultationApi.Core.DTOs.Auth;
using ConsultationApi.Core.DTOs.Doctors;
using ConsultationApi.Core.DTOs.Prescriptions;
using ConsultationApi.Core.DTOs.Reviews;
using ConsultationApi.Core.DTOs.Sessions;
using ConsultationApi.Core.Enums;

namespace ConsultationApi.Tests.Integration;

public class AppointmentFlowTests : IntegrationTestBase
{
    // -----------------------------------------------------------------------
    // Existing tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FullFlow_RegisterLoginBookConfirm_Succeeds()
    {
        var patientEmail = $"patient_{Guid.NewGuid()}@test.com";
        var patientRegister = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = patientEmail,
            Password = "TestP@ss1",
            FullName = "Flow Patient",
            Role = UserRole.Patient,
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male
        });
        Assert.Equal(HttpStatusCode.Created, patientRegister.StatusCode);
        var patientTokens = await patientRegister.Content.ReadFromJsonAsync<AuthResponseDto>();

        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var doctorFullName = $"FlowDoctor_{uniqueSuffix}";
        var doctorRegister = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = $"doctor_{uniqueSuffix}@test.com",
            Password = "TestP@ss1",
            FullName = doctorFullName,
            Role = UserRole.Doctor,
            Specialization = "General Medicine",
            LicenseNumber = $"LIC-{uniqueSuffix}",
            ConsultationFee = 150m
        });
        Assert.Equal(HttpStatusCode.Created, doctorRegister.StatusCode);
        var doctorTokens = await doctorRegister.Content.ReadFromJsonAsync<AuthResponseDto>();

        var doctorsResponse = await Client.GetAsync("/api/doctors?page=1&pageSize=100");
        var doctorsBody = await doctorsResponse.Content.ReadFromJsonAsync<PagedResultHelper<DoctorSummaryDto>>();
        var doctor = doctorsBody!.Data.First(d => d.FullName == doctorFullName);

        var patientClient = Factory.CreateClient();
        patientClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", patientTokens!.AccessToken);

        var scheduledAt = DateTime.UtcNow.Date.AddDays(7)
            .AddHours(9 + (Math.Abs(uniqueSuffix.GetHashCode()) % 8));

        var bookResponse = await patientClient.PostAsJsonAsync("/api/appointments", new BookAppointmentDto
        {
            DoctorId = doctor.Id,
            ScheduledAt = scheduledAt,
            DurationMinutes = 30,
            SessionType = SessionType.Video
        });
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);
        var appointment = await bookResponse.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(appointment);
        Assert.Equal(AppointmentStatus.Pending, appointment!.Status);

        var doctorClient = Factory.CreateClient();
        doctorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", doctorTokens!.AccessToken);

        var confirmResponse = await doctorClient.PutAsync($"/api/appointments/{appointment.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.Equal(AppointmentStatus.Confirmed, confirmed!.Status);
        Assert.NotNull(confirmed.SessionId);
    }

    [Fact]
    public async Task PatientCallingDoctorOnlyEndpoint_Returns403()
    {
        var email = $"403test_{Guid.NewGuid()}@test.com";
        var register = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "TestP@ss1",
            FullName = "Forbidden Patient",
            Role = UserRole.Patient,
            DateOfBirth = new DateTime(1995, 6, 15),
            Gender = Gender.Female
        });
        var tokens = await register.Content.ReadFromJsonAsync<AuthResponseDto>();

        var patientClient = Factory.CreateClient();
        patientClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var response = await patientClient.PutAsync($"/api/appointments/{Guid.NewGuid()}/confirm", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Full end-to-end consultation lifecycle
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FullFlow_SessionStartMessagesEndPrescriptionReview_Succeeds()
    {
        // ── 1. Register patient ──────────────────────────────────────────────
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var patientRegister = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = $"patient_{uniqueSuffix}@test.com",
            Password = "TestP@ss1",
            FullName = $"LifecyclePatient_{uniqueSuffix}",
            Role = UserRole.Patient,
            DateOfBirth = new DateTime(1990, 3, 15),
            Gender = Gender.Female
        });
        Assert.Equal(HttpStatusCode.Created, patientRegister.StatusCode);
        var patientTokens = await patientRegister.Content.ReadFromJsonAsync<AuthResponseDto>();

        // ── 2. Register doctor ───────────────────────────────────────────────
        var doctorFullName = $"LifecycleDoctor_{uniqueSuffix}";
        var doctorRegister = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = $"doctor_{uniqueSuffix}@test.com",
            Password = "TestP@ss1",
            FullName = doctorFullName,
            Role = UserRole.Doctor,
            Specialization = "Neurology",
            LicenseNumber = $"LIC-{uniqueSuffix}",
            ConsultationFee = 200m
        });
        Assert.Equal(HttpStatusCode.Created, doctorRegister.StatusCode);
        var doctorTokens = await doctorRegister.Content.ReadFromJsonAsync<AuthResponseDto>();

        // ── 3. Resolve the doctor's profile ID ──────────────────────────────
        var doctorsResponse = await Client.GetAsync("/api/doctors?page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, doctorsResponse.StatusCode);
        var doctorsBody = await doctorsResponse.Content.ReadFromJsonAsync<PagedResultHelper<DoctorSummaryDto>>();
        var doctorSummary = doctorsBody!.Data.First(d => d.FullName == doctorFullName);

        // ── 4. Patient books appointment ─────────────────────────────────────
        var patientClient = Factory.CreateClient();
        patientClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", patientTokens!.AccessToken);

        var scheduledAt = DateTime.UtcNow.Date.AddDays(7)
            .AddHours(9 + (Math.Abs(uniqueSuffix.GetHashCode()) % 8));

        var bookResponse = await patientClient.PostAsJsonAsync("/api/appointments", new BookAppointmentDto
        {
            DoctorId = doctorSummary.Id,
            ScheduledAt = scheduledAt,
            DurationMinutes = 30,
            SessionType = SessionType.Video
        });
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);
        var appointment = await bookResponse.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(appointment);
        Assert.Equal(AppointmentStatus.Pending, appointment!.Status);

        // ── 5. Doctor confirms → ConsultationSession auto-created ────────────
        var doctorClient = Factory.CreateClient();
        doctorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", doctorTokens!.AccessToken);

        var confirmResponse = await doctorClient.PutAsync($"/api/appointments/{appointment.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.Equal(AppointmentStatus.Confirmed, confirmed!.Status);
        Assert.NotNull(confirmed.SessionId);
        var sessionId = confirmed.SessionId!.Value;

        // ── 6. Doctor starts the session ─────────────────────────────────────
        var startResponse = await doctorClient.PostAsync($"/api/sessions/{sessionId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var startedSession = await startResponse.Content.ReadFromJsonAsync<SessionDto>();
        Assert.NotNull(startedSession!.StartedAt);
        Assert.Null(startedSession.EndedAt);

        // ── 7. Doctor sends a message ────────────────────────────────────────
        var doctorMsgResponse = await doctorClient.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/messages",
            new SendMessageDto { Content = "Hello, what symptoms are you experiencing?" });
        Assert.Equal(HttpStatusCode.Created, doctorMsgResponse.StatusCode);
        var doctorMsg = await doctorMsgResponse.Content.ReadFromJsonAsync<ChatMessageDto>();
        Assert.NotNull(doctorMsg);
        Assert.Equal("Hello, what symptoms are you experiencing?", doctorMsg!.Content);
        Assert.Equal(sessionId, doctorMsg.SessionId);

        // ── 8. Patient sends a message ───────────────────────────────────────
        var patientMsgResponse = await patientClient.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/messages",
            new SendMessageDto { Content = "I have a persistent headache and dizziness." });
        Assert.Equal(HttpStatusCode.Created, patientMsgResponse.StatusCode);
        var patientMsg = await patientMsgResponse.Content.ReadFromJsonAsync<ChatMessageDto>();
        Assert.NotNull(patientMsg);
        Assert.Equal("I have a persistent headache and dizziness.", patientMsg!.Content);

        // ── 9. GET messages → both messages are present ──────────────────────
        var messagesResponse = await patientClient.GetAsync($"/api/sessions/{sessionId}/messages");
        Assert.Equal(HttpStatusCode.OK, messagesResponse.StatusCode);
        var messages = await messagesResponse.Content.ReadFromJsonAsync<List<ChatMessageDto>>();
        Assert.NotNull(messages);
        Assert.Equal(2, messages!.Count);
        Assert.Contains(messages, m => m.Content == "Hello, what symptoms are you experiencing?");
        Assert.Contains(messages, m => m.Content == "I have a persistent headache and dizziness.");

        // ── 10. Doctor ends the session ──────────────────────────────────────
        var endResponse = await doctorClient.PostAsync($"/api/sessions/{sessionId}/end", null);
        Assert.Equal(HttpStatusCode.OK, endResponse.StatusCode);
        var endedSession = await endResponse.Content.ReadFromJsonAsync<SessionDto>();
        Assert.NotNull(endedSession!.EndedAt);
        Assert.NotNull(endedSession.StartedAt);

        // ── 11. Appointment transitions to Completed ─────────────────────────
        var appointmentDetailResponse = await patientClient.GetAsync($"/api/appointments/{appointment.Id}");
        Assert.Equal(HttpStatusCode.OK, appointmentDetailResponse.StatusCode);
        var completedAppointment = await appointmentDetailResponse.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.Equal(AppointmentStatus.Completed, completedAppointment!.Status);

        // ── 12. Doctor issues a prescription with two medication items ────────
        var prescriptionResponse = await doctorClient.PostAsJsonAsync("/api/prescriptions", new IssuePrescriptionDto
        {
            SessionId = sessionId,
            Instructions = "Take all medication with food. Rest and stay hydrated.",
            MedicationItems =
            [
                new() { DrugName = "Ibuprofen",   Dosage = "400mg", FrequencyPerDay = 3, DurationDays = 5 },
                new() { DrugName = "Paracetamol", Dosage = "500mg", FrequencyPerDay = 2, DurationDays = 3 }
            ]
        });
        Assert.Equal(HttpStatusCode.Created, prescriptionResponse.StatusCode);
        var prescription = await prescriptionResponse.Content.ReadFromJsonAsync<PrescriptionDto>();
        Assert.NotNull(prescription);
        Assert.Equal(sessionId, prescription!.SessionId);
        Assert.Equal(2, prescription.MedicationItems.Count);
        Assert.Contains(prescription.MedicationItems, m => m.DrugName == "Ibuprofen");
        Assert.Contains(prescription.MedicationItems, m => m.DrugName == "Paracetamol");

        // ── 13. Patient retrieves their prescriptions ─────────────────────────
        var myPrescriptionsResponse = await patientClient.GetAsync("/api/patients/me/prescriptions");
        Assert.Equal(HttpStatusCode.OK, myPrescriptionsResponse.StatusCode);
        var myPrescriptions = await myPrescriptionsResponse.Content.ReadFromJsonAsync<List<PrescriptionDto>>();
        Assert.NotNull(myPrescriptions);
        Assert.True(myPrescriptions!.Count >= 1);
        Assert.Contains(myPrescriptions, p => p.Id == prescription.Id);

        // ── 14. Patient submits a review for the completed appointment ────────
        var reviewResponse = await patientClient.PostAsJsonAsync("/api/reviews", new SubmitReviewDto
        {
            AppointmentId = appointment.Id,
            Rating = 5,
            Comment = "Excellent consultation, very thorough!"
        });
        Assert.Equal(HttpStatusCode.Created, reviewResponse.StatusCode);
        var review = await reviewResponse.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotNull(review);
        Assert.Equal(appointment.Id, review!.AppointmentId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Excellent consultation, very thorough!", review.Comment);
    }

    // -----------------------------------------------------------------------
    // Error / guard rule tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Patient_SendMessage_AfterSessionEnded_Returns422()
    {
        var (patientClient, _, _, sessionId) = await SetupUpToSessionEndAsync();

        var response = await patientClient.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/messages",
            new SendMessageDto { Content = "Can I still ask a question?" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task DuplicatePrescription_ForSameSession_Returns409()
    {
        var (_, doctorClient, _, sessionId) = await SetupUpToSessionEndAsync();

        var request = new IssuePrescriptionDto
        {
            SessionId = sessionId,
            Instructions = "First prescription.",
            MedicationItems =
            [
                new() { DrugName = "Aspirin", Dosage = "100mg", FrequencyPerDay = 1, DurationDays = 3 }
            ]
        };

        var firstResponse = await doctorClient.PostAsJsonAsync("/api/prescriptions", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await doctorClient.PostAsJsonAsync("/api/prescriptions", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task DuplicateReview_ForSameAppointment_Returns409()
    {
        var (patientClient, _, appointmentId, _) = await SetupUpToSessionEndAsync();

        var request = new SubmitReviewDto
        {
            AppointmentId = appointmentId,
            Rating = 4,
            Comment = "Good experience."
        };

        var firstResponse = await patientClient.PostAsJsonAsync("/api/reviews", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await patientClient.PostAsJsonAsync("/api/reviews", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    // -----------------------------------------------------------------------
    // Private helper — bootstraps the full flow up to session ended
    // -----------------------------------------------------------------------

    /// <summary>
    /// Registers a fresh patient + doctor, books an appointment, confirms it,
    /// starts the session, and ends the session. Returns authenticated clients
    /// and the IDs needed by subsequent test steps.
    /// </summary>
    private async Task<(HttpClient patientClient, HttpClient doctorClient, Guid appointmentId, Guid sessionId)>
        SetupUpToSessionEndAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");

        var patientRegister = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = $"patient_{suffix}@test.com",
            Password = "TestP@ss1",
            FullName = $"SetupPatient_{suffix}",
            Role = UserRole.Patient,
            DateOfBirth = new DateTime(1992, 6, 10),
            Gender = Gender.Male
        });
        var patientTokens = await patientRegister.Content.ReadFromJsonAsync<AuthResponseDto>();

        var doctorFullName = $"SetupDoctor_{suffix}";
        var doctorRegister = await Client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = $"doctor_{suffix}@test.com",
            Password = "TestP@ss1",
            FullName = doctorFullName,
            Role = UserRole.Doctor,
            Specialization = "General Medicine",
            LicenseNumber = $"LIC-{suffix}",
            ConsultationFee = 100m
        });
        var doctorTokens = await doctorRegister.Content.ReadFromJsonAsync<AuthResponseDto>();

        var doctorsBody = await (await Client.GetAsync("/api/doctors?page=1&pageSize=100"))
            .Content.ReadFromJsonAsync<PagedResultHelper<DoctorSummaryDto>>();
        var doctorSummary = doctorsBody!.Data.First(d => d.FullName == doctorFullName);

        var patientClient = Factory.CreateClient();
        patientClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", patientTokens!.AccessToken);

        var doctorClient = Factory.CreateClient();
        doctorClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", doctorTokens!.AccessToken);

        var scheduledAt = DateTime.UtcNow.Date.AddDays(7)
            .AddHours(9 + (Math.Abs(suffix.GetHashCode()) % 8));

        var bookResponse = await patientClient.PostAsJsonAsync("/api/appointments", new BookAppointmentDto
        {
            DoctorId = doctorSummary.Id,
            ScheduledAt = scheduledAt,
            DurationMinutes = 30,
            SessionType = SessionType.Video
        });
        var appointment = await bookResponse.Content.ReadFromJsonAsync<AppointmentDto>();

        var confirmResponse = await doctorClient.PutAsync($"/api/appointments/{appointment!.Id}/confirm", null);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<AppointmentDto>();
        var sessionId = confirmed!.SessionId!.Value;

        await doctorClient.PostAsync($"/api/sessions/{sessionId}/start", null);
        await doctorClient.PostAsync($"/api/sessions/{sessionId}/end", null);

        return (patientClient, doctorClient, appointment.Id, sessionId);
    }

    // -----------------------------------------------------------------------
    // Shared helper types
    // -----------------------------------------------------------------------

    private class PagedResultHelper<T>
    {
        public List<T> Data { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
