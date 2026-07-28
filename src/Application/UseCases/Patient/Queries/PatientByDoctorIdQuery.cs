using Application.UseCases.Patient.Common;
using MediatR;
using ErrorOr;
using Application.Common.Messaging;

namespace Application.UseCases.Patient.Queries;

public record PatientByDoctorIdQuery(
    Guid doctorId) : IQuery<ErrorOr<List<PatientResult>>>;