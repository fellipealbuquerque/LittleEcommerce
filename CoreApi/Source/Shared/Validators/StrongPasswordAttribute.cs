using System.ComponentModel.DataAnnotations;

namespace LittleEcommerce.Source.Shared.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 72;

    public StrongPasswordAttribute()
        : base($"A senha precisa ter de {MinimumLength} a {MaximumLength} caracteres, com pelo menos uma letra e um número")
    {
    }

    public override bool IsValid(object? value) =>
        value is string password
        && password.Length is >= MinimumLength and <= MaximumLength
        && password.Any(char.IsLetter)
        && password.Any(char.IsDigit);
}
