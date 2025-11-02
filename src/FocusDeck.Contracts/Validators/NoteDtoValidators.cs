using FluentValidation;
using FocusDeck.Contracts.DTOs;

namespace FocusDeck.Contracts.Validators;

public class CreateNoteDtoValidator : AbstractValidator<CreateNoteDto>
{
    public CreateNoteDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required")
            .MaximumLength(32).WithMessage("Color must not exceed 32 characters")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color code");
    }
}

public class UpdateNoteDtoValidator : AbstractValidator<UpdateNoteDto>
{
    public UpdateNoteDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required")
            .MaximumLength(32).WithMessage("Color must not exceed 32 characters")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color code");
    }
}
