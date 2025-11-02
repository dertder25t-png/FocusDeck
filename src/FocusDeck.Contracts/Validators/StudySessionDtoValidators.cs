using FluentValidation;
using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Contracts.Validators;

public class CreateStudySessionDtoValidator : AbstractValidator<CreateStudySessionDto>
{
    public CreateStudySessionDtoValidator()
    {
        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0 minutes");

        RuleFor(x => x.Category)
            .MaximumLength(120).WithMessage("Category must not exceed 120 characters");
    }
}

public class UpdateStudySessionDtoValidator : AbstractValidator<UpdateStudySessionDto>
{
    public UpdateStudySessionDtoValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required");

        RuleFor(x => x.DurationMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Duration must be greater than or equal to 0 minutes");

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 3).WithMessage("Status must be between 0 and 3");

        RuleFor(x => x.FocusRate)
            .InclusiveBetween(0, 100).When(x => x.FocusRate.HasValue)
            .WithMessage("Focus rate must be between 0 and 100");

        RuleFor(x => x.BreaksCount)
            .GreaterThanOrEqualTo(0).WithMessage("Breaks count must be greater than or equal to 0");

        RuleFor(x => x.BreakDurationMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Break duration must be greater than or equal to 0");

        RuleFor(x => x.Category)
            .MaximumLength(120).WithMessage("Category must not exceed 120 characters");
    }
}
