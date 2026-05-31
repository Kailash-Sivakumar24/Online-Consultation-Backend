using ConsultationApi.Core.Interfaces;
using ConsultationApi.Core.Settings;
using ConsultationApi.Infrastructure.BackgroundServices;
using ConsultationApi.Infrastructure.Data;
using ConsultationApi.Infrastructure.Repositories;
using ConsultationApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ConsultationApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddMemoryCache();

        var redisConn = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConn));
            services.AddScoped<ICacheService, LayeredCacheService>();
            services.AddHostedService<RedisCacheInvalidationListener>();
        }
        else
        {
            services.AddScoped<ICacheService, MemoryCacheService>();
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddHostedService<AppointmentReminderService>();
        services.AddHostedService<SessionStatusUpdaterService>();

        return services;
    }
}
