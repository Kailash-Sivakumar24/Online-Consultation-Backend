using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Interfaces;
using ConsultationApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsultationApi.Infrastructure.BackgroundServices;

public class SessionStatusUpdaterService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SessionStatusUpdaterService> _logger;

    public SessionStatusUpdaterService(IServiceProvider services, ILogger<SessionStatusUpdaterService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateExpiredSessionsAsync();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task UpdateExpiredSessionsAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var signalRNotifier = scope.ServiceProvider.GetRequiredService<ISignalRNotifier>();

        var now = DateTime.UtcNow;

        var expired = await db.Appointments
            .Include(a => a.ConsultationSession)
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Where(a => a.Status == AppointmentStatus.Confirmed
                        && a.ScheduledAt.AddMinutes(a.DurationMinutes) < now
                        && (a.ConsultationSession == null || a.ConsultationSession.StartedAt == null)
                        && a.DeletedAt == null)
            .ToListAsync();

        foreach (var appt in expired)
        {
            appt.Status = AppointmentStatus.Completed;
            _logger.LogInformation("Auto-completed appointment {Id} that exceeded its scheduled time.", appt.Id);

            if (appt.ConsultationSession != null)
                await signalRNotifier.NotifySessionStatusChangedAsync(appt.ConsultationSession.Id, "Completed");

            await notificationService.CreateNotificationAsync(
                appt.Patient.UserId,
                NotificationType.SessionEnded,
                "Appointment Completed",
                $"Your appointment with Dr. {appt.Doctor.FullName} has been automatically completed.");

            await notificationService.CreateNotificationAsync(
                appt.Doctor.UserId,
                NotificationType.SessionEnded,
                "Appointment Completed",
                $"Your appointment with {appt.Patient.FullName} has been automatically completed.");
        }

        if (expired.Any())
            await db.SaveChangesAsync();
    }
}
