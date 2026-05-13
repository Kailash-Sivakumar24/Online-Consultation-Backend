using ConsultationApi.Core.DTOs.Prescriptions;
using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;

namespace ConsultationApi.Infrastructure.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public PrescriptionService(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<PrescriptionDto> IssuePrescriptionAsync(IssuePrescriptionDto request)
    {
        var session = await _uow.ConsultationSessions.GetByIdAsync(request.SessionId)
            ?? throw new NotFoundException("ConsultationSession", request.SessionId);

        var appointment = await _uow.Appointments.GetByIdAsync(session.AppointmentId)
            ?? throw new NotFoundException("Appointment", session.AppointmentId);

        if (appointment.Status != AppointmentStatus.Completed)
            throw new BusinessRuleViolationException("A prescription can only be issued after a completed session.");

        var doctors = await _uow.Doctors.FindAsync(d => d.UserId == _currentUser.UserId);
        var doctor = doctors.FirstOrDefault();
        if (doctor == null || doctor.Id != appointment.DoctorId)
            throw new ForbiddenException("Only the doctor who ran the session can issue a prescription.");

        var existing = await _uow.Prescriptions.FindAsync(p => p.SessionId == request.SessionId);
        if (existing.Any())
            throw new ConflictException("A prescription has already been issued for this session.");

        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            IssuedAt = DateTime.UtcNow,
            Instructions = request.Instructions
        };
        await _uow.Prescriptions.AddAsync(prescription);

        foreach (var item in request.MedicationItems)
        {
            var med = new MedicationItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                DrugName = item.DrugName,
                Dosage = item.Dosage,
                FrequencyPerDay = item.FrequencyPerDay,
                DurationDays = item.DurationDays,
                Instructions = item.Instructions
            };
            await _uow.MedicationItems.AddAsync(med);
        }

        await _uow.CommitAsync();
        return await GetByIdAsync(prescription.Id);
    }

    public async Task<PrescriptionDto> GetByIdAsync(Guid id)
    {
        var prescription = await _uow.Prescriptions.GetByIdAsync(id)
            ?? throw new NotFoundException("Prescription", id);

        var items = await _uow.MedicationItems.FindAsync(m => m.PrescriptionId == id);

        return new PrescriptionDto
        {
            Id = prescription.Id,
            SessionId = prescription.SessionId,
            IssuedAt = prescription.IssuedAt,
            Instructions = prescription.Instructions,
            MedicationItems = items.Select(m => new MedicationItemResponseDto
            {
                Id = m.Id,
                DrugName = m.DrugName,
                Dosage = m.Dosage,
                FrequencyPerDay = m.FrequencyPerDay,
                DurationDays = m.DurationDays,
                Instructions = m.Instructions
            }).ToList()
        };
    }

    public async Task<IEnumerable<PrescriptionDto>> GetMyPrescriptionsAsync()
    {
        var patients = await _uow.Patients.FindAsync(p => p.UserId == _currentUser.UserId);
        var patient = patients.FirstOrDefault() ?? throw new NotFoundException("Patient profile not found.");

        var appointments = await _uow.Appointments.FindAsync(a =>
            a.PatientId == patient.Id && a.Status == AppointmentStatus.Completed);

        var result = new List<PrescriptionDto>();
        foreach (var appt in appointments)
        {
            var sessions = await _uow.ConsultationSessions.FindAsync(s => s.AppointmentId == appt.Id);
            foreach (var session in sessions)
            {
                var prescriptions = await _uow.Prescriptions.FindAsync(p => p.SessionId == session.Id);
                foreach (var p in prescriptions)
                    result.Add(await GetByIdAsync(p.Id));
            }
        }
        return result;
    }
}
