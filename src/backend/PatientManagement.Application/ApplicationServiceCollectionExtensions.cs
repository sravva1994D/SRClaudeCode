using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Handlers;

namespace PatientManagement.Application;

/// <summary>Registers use-case handlers and their validators (CLAUDE.md section 6.9).</summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshSessionCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<ChangePasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();

        services.AddScoped<IValidator<LoginCommand>, Validators.LoginCommandValidator>();
        services.AddScoped<IValidator<ChangePasswordCommand>, Validators.ChangePasswordCommandValidator>();

        services.AddScoped<Clinic.Handlers.GetClinicProfileQueryHandler>();
        services.AddScoped<Clinic.Handlers.UpdateClinicProfileCommandHandler>();
        services.AddScoped<IValidator<Clinic.UpdateClinicProfileCommand>, Validators.UpdateClinicProfileCommandValidator>();
        return services;
    }
}
