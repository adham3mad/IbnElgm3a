using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class StrongPasswordAttribute : ValidationAttribute
{
    private static readonly Regex HasUpper = new Regex(@"[A-Z]+", RegexOptions.Compiled);
    private static readonly Regex HasLower = new Regex(@"[a-z]+", RegexOptions.Compiled);
    private static readonly Regex HasDigit = new Regex(@"\d+", RegexOptions.Compiled);
    private static readonly Regex HasSpecial = new Regex(@"[\W_]+", RegexOptions.Compiled);

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var password = value as string;

        if (string.IsNullOrWhiteSpace(password))
            return ValidationResult.Success;

        var localizer = (IbnElgm3a.Services.Localization.ILocalizationService)validationContext.GetService(typeof(IbnElgm3a.Services.Localization.ILocalizationService))!;

        if (password.Length < 10)
            return new ValidationResult(localizer.GetMessage("PASSWORD_TOO_SHORT"));

        if (!HasUpper.IsMatch(password))
            return new ValidationResult(localizer.GetMessage("PASSWORD_MISSING_UPPER"));

        if (!HasLower.IsMatch(password))
            return new ValidationResult(localizer.GetMessage("PASSWORD_MISSING_LOWER"));

        if (!HasDigit.IsMatch(password))
            return new ValidationResult(localizer.GetMessage("PASSWORD_MISSING_DIGIT"));

        if (!HasSpecial.IsMatch(password))
            return new ValidationResult(localizer.GetMessage("PASSWORD_MISSING_SPECIAL"));

        return ValidationResult.Success;
    }
}
