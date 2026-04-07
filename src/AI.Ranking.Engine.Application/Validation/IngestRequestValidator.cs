using AI.Ranking.Engine.Application.Contracts;
using AI.Ranking.Engine.Application.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Application.Validation;

public sealed class IngestRequestValidator : AbstractValidator<IngestRequest>
{
    public IngestRequestValidator(IOptions<IngestOptions> ingestOptions)
    {
        ArgumentNullException.ThrowIfNull(ingestOptions);

        var maxBytes = ingestOptions.Value.MaxUploadBytes;

        RuleFor(x => x.EntityId)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.ContentType)
            .IsInEnum();

        RuleFor(x => x.ContentLength)
            .GreaterThan(0)
            .LessThanOrEqualTo(maxBytes)
            .WithMessage($"Content length must be greater than 0 and at most {maxBytes} bytes (configured MaxUploadBytes).");

        RuleFor(x => x.FileName)
            .Must(HasAllowedExtension)
            .WithMessage("File name must have a .pdf or .docx extension (case-insensitive).");
    }

    private static bool HasAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
               || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
    }
}
