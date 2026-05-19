using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.LabResult.Common;
using Application.UseCases.LabResult.Queries;

namespace Application.UseCases.LabResult.Handlers;

internal sealed class GetLabResultByIdQueryHandler
    : IRequestHandler<GetLabResultByIdQuery, ErrorOr<LabResultResult>>
{
    private readonly ILabResultRepository _labResultRepository;

    public GetLabResultByIdQueryHandler(ILabResultRepository labResultRepository)
    {
        _labResultRepository = labResultRepository;
    }

    public async Task<ErrorOr<LabResultResult>> Handle(
        GetLabResultByIdQuery request,
        CancellationToken cancellationToken)
    {
        var record = await _labResultRepository.GetByIdAsync(request.RecordId);
        
        if (record is null)
        {
            return RecordErrors.RecordNotFound;
        }

        return new LabResultResult(
            record.Id,
            record.PatientId,
            record.SampleDate,
            record.Hba1c,
            record.TotalCholesterol,
            record.Ldl,
            record.Hdl,
            record.Triglycerides,
            record.Creatinine,
            record.Bun,
            record.EgoProteins,
            record.EgoGlucose,
            record.Notes,
            record.CreatedAt);
    }
}
