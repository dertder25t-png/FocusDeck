using FluentValidation;
using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Contracts.Validators;

public class CreateAutomationDtoValidator : AbstractValidator<CreateAutomationDto>
{
    public CreateAutomationDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
    }
}

public class UpdateAutomationDtoValidator : AbstractValidator<UpdateAutomationDto>
{
    public UpdateAutomationDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
    }
}
