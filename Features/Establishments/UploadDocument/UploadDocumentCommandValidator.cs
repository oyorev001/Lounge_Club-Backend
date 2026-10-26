namespace LoungeClubsApi.Features.Establishments.UploadDocument;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.EstablishmentId).NotEmpty();
        RuleFor(x => x.File).NotNull().WithMessage("A file is required.");
        RuleFor(x => x.File.Length).GreaterThan(0).When(x => x.File is not null)
            .WithMessage("File is empty.");
        RuleFor(x => x.DocumentType).IsInEnum();
    }
}
