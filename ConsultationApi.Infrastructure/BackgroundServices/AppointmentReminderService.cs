using ConsultationApi.Core.Enums;
using ConsultationApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsultationApi.Infrastructure.BackgroundServices;

public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(IServiceProvider services, ILogger<AppointmentReminderService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckRemindersAsync();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckRemindersAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var within24Hours = now.AddHours(24);

        var upcoming = await db.Appointments
            .Where(a => a.Status == AppointmentStatus.Confirmed
                        && a.ScheduledAt >= now
                        && a.ScheduledAt <= within24Hours
                        && a.DeletedAt == null)
            .ToListAsync();

        foreach (var appt in upcoming)
            _logger.LogInformation("Reminder: Appointment {Id} scheduled at {ScheduledAt}", appt.Id, appt.ScheduledAt);
    }
}
