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

        services.AddScoped<Patients.Handlers.GetPatientQueryHandler>();
        services.AddScoped<Patients.Handlers.CreatePatientCommandHandler>();
        services.AddScoped<Patients.Handlers.UpdatePatientCommandHandler>();
        services.AddScoped<Patients.Handlers.DeletePatientCommandHandler>();
        services.AddScoped<IValidator<Patients.CreatePatientCommand>, Validators.CreatePatientCommandValidator>();
        services.AddScoped<IValidator<Patients.UpdatePatientCommand>, Validators.UpdatePatientCommandValidator>();
        services.AddScoped<Patients.Handlers.FindDuplicatePatientsQueryHandler>();
        services.AddScoped<IValidator<Patients.FindDuplicatePatientsQuery>, Validators.FindDuplicatePatientsQueryValidator>();

        services.AddScoped<Patients.Handlers.SearchPatientsQueryHandler>();
        services.AddScoped<IValidator<Patients.SearchPatientsQuery>, Validators.SearchPatientsQueryValidator>();

        services.AddScoped<Patients.Handlers.RecordPatientViewedCommandHandler>();
        services.AddScoped<Patients.Handlers.ListRecentPatientsQueryHandler>();

        services.AddScoped<Appointments.Handlers.ScheduleAppointmentCommandHandler>();
        services.AddScoped<Appointments.Handlers.GetAppointmentQueryHandler>();
        services.AddScoped<IValidator<Appointments.ScheduleAppointmentCommand>, Validators.ScheduleAppointmentCommandValidator>();
        services.AddScoped<Appointments.Handlers.ListAppointmentsQueryHandler>();
        services.AddScoped<IValidator<Appointments.ListAppointmentsQuery>, Validators.ListAppointmentsQueryValidator>();

        services.AddScoped<Appointments.RescheduleAppointmentCommandHandler>();
        services.AddScoped<Appointments.ChangeAppointmentStatusCommandHandler>();
        services.AddScoped<IValidator<Appointments.RescheduleAppointmentCommand>, Validators.RescheduleAppointmentCommandValidator>();

        services.AddScoped<Visits.Handlers.StartVisitCommandHandler>();
        services.AddScoped<Visits.Handlers.GetVisitQueryHandler>();
        services.AddScoped<Visits.Handlers.CompleteVisitCommandHandler>();

        services.AddScoped<Consultation.Handlers.RecordVitalsCommandHandler>();
        services.AddScoped<Consultation.Handlers.GetVitalsQueryHandler>();
        services.AddScoped<Consultation.Handlers.UpdateVitalsCommandHandler>();

        services.AddScoped<Prescriptions.Handlers.GeneratePrescriptionCommandHandler>();
        services.AddScoped<Prescriptions.Handlers.GetPrescriptionQueryHandler>();
        services.AddScoped<Prescriptions.Handlers.RecordPrescriptionPrintCommandHandler>();
        services.AddScoped<Prescriptions.Handlers.GetPrescriptionPrintsQueryHandler>();
        services.AddScoped<Prescriptions.Handlers.GetPrescriptionPrintQueryHandler>();

        services.AddScoped<Consultation.Handlers.SaveComplaintsCommandHandler>();
        services.AddScoped<IValidator<Consultation.SaveComplaintsCommand>, Validators.SaveComplaintsCommandValidator>();
        services.AddScoped<Consultation.Handlers.SaveDiagnosesCommandHandler>();
        services.AddScoped<IValidator<Consultation.SaveDiagnosesCommand>, Validators.SaveDiagnosesCommandValidator>();
        services.AddScoped<Consultation.Handlers.GetVisitNotesQueryHandler>();

        services.AddScoped<Consultation.Handlers.SaveMedicationsCommandHandler>();
        services.AddScoped<IValidator<Consultation.SaveMedicationsCommand>, Validators.SaveMedicationsCommandValidator>();
        services.AddScoped<Consultation.Handlers.MedicineSuggestionsQueryHandler>();
        services.AddScoped<IValidator<Consultation.MedicineSuggestionsQuery>, Validators.MedicineSuggestionsQueryValidator>();

        services.AddScoped<Appointments.Handlers.PatientAppointmentHistoryHandler>();
        services.AddScoped<IValidator<Appointments.PatientAppointmentHistoryQuery>, Validators.PatientAppointmentHistoryValidator>();
        return services;
    }
}
