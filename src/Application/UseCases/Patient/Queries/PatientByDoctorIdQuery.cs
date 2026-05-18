using Application.UseCases.Patient.Common;
using MediatR;
using ErrorOr;
namespace Application.UseCases.Patient.Queries;

public record PatientByDoctorIdQuery(
    Guid doctorId) : IRequest<ErrorOr<List<PatientResult>>>;