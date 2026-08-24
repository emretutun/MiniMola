using FluentValidation;

namespace MiniMola.Application.Aquariums.Validators;

public sealed class UpdateFishNicknameRequestValidator
    : AbstractValidator<UpdateFishNicknameRequest>
{
    public UpdateFishNicknameRequestValidator()
    {
        RuleFor(x => x.Nickname)
            .Cascade(CascadeMode.Stop)

            .NotEmpty()
            .WithMessage(
                "Balığın adı boş bırakılamaz.")

            .MaximumLength(20)
            .WithMessage(
                "Balığın adı en fazla 20 karakter olabilir.")

            .Must(ContainsOnlyAllowedCharacters)
            .WithMessage(
                "Balık adı yalnızca harf, rakam, "
                + "boşluk, tire ve kesme işareti içerebilir.");
    }

    private static bool ContainsOnlyAllowedCharacters(
        string nickname)
    {
        return nickname.All(
            character =>
                char.IsLetterOrDigit(character)
                || character == ' '
                || character == '-'
                || character == '\'');
    }
}