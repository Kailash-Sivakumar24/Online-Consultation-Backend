using AutoMapper;
using ConsultationApi.Core.DTOs.Appointments;
using ConsultationApi.Core.DTOs.Doctors;
using ConsultationApi.Core.DTOs.Notifications;
using ConsultationApi.Core.DTOs.Prescriptions;
using ConsultationApi.Core.DTOs.Reviews;
using ConsultationApi.Core.DTOs.Sessions;
using ConsultationApi.Core.Entities;

namespace ConsultationApi.Infrastructure.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Doctor, DoctorSummaryDto>();
        CreateMap<Doctor, DoctorDetailDto>()
            .ForMember(d => d.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty));

        CreateMap<ConsultationSession, SessionDto>();
        CreateMap<ChatMessage, ChatMessageDto>()
            .ForMember(d => d.SenderName, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.Email : string.Empty));

        CreateMap<Prescription, PrescriptionDto>();
        CreateMap<MedicationItem, MedicationItemResponseDto>();

        CreateMap<Appointment, AppointmentDto>()
            .ForMember(d => d.PatientName, opt => opt.MapFrom(src => src.Patient != null ? src.Patient.FullName : string.Empty))
            .ForMember(d => d.DoctorName, opt => opt.MapFrom(src => src.Doctor != null ? src.Doctor.FullName : string.Empty))
            .ForMember(d => d.SessionId, opt => opt.MapFrom(src => src.ConsultationSession != null ? src.ConsultationSession.Id : (Guid?)null));

        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.PatientName, opt => opt.MapFrom(src => src.Patient != null ? src.Patient.FullName : string.Empty));

        CreateMap<Notification, NotificationDto>();
    }
}
