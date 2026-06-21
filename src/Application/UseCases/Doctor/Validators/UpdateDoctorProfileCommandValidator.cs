using Application.UseCases.Doctor.Commands;
using FluentValidation;

namespace Application.UseCases.Doctor.Validators;

internal sealed class UpdateDoctorProfileCommandValidator : AbstractValidator<UpdateDoctorProfileCommand>
{
    public UpdateDoctorProfileCommandValidator()
    {
        RuleFor(x => x.LicenseNumber)
            .NotEmpty()
            .WithMessage("El número de cédula profesional es requerido");

        RuleFor(x => x.Speciality)
            .NotEmpty()
            .WithMessage("La especialidad es requerida");
    }
}
