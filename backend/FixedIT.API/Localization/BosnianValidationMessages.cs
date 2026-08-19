using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FixedIT.API.Localization;

public static class BosnianValidationMessages
{
    public static string Build(ModelStateDictionary modelState)
    {
        var messages = modelState.Values
            .SelectMany(value => value.Errors)
            .Select(Localize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return messages.Length == 0
            ? "Zahtjev sadrži neispravne podatke."
            : string.Join(" ", messages);
    }

    private static string Localize(ModelError error)
    {
        var message = error.ErrorMessage;
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Zahtjev sadrži vrijednost u neispravnom formatu.";
        }

        if (message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return "Vrijednost obaveznog polja nije unesena.";
        }

        if (message.Contains("valid e-mail", StringComparison.OrdinalIgnoreCase)
            || message.Contains("valid email", StringComparison.OrdinalIgnoreCase))
        {
            return "Unesena email adresa nije ispravna.";
        }

        if (message.Contains("maximum length", StringComparison.OrdinalIgnoreCase)
            || message.Contains("minimum length", StringComparison.OrdinalIgnoreCase)
            || message.Contains("between", StringComparison.OrdinalIgnoreCase)
            || message.Contains("range", StringComparison.OrdinalIgnoreCase))
        {
            return "Dužina ili vrijednost polja nije u dozvoljenom rasponu.";
        }

        if (message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
            || message.Contains("is invalid", StringComparison.OrdinalIgnoreCase)
            || message.Contains("JSON", StringComparison.OrdinalIgnoreCase))
        {
            return "Zahtjev sadrži vrijednost u neispravnom formatu.";
        }

        if (message.Contains(" ne ", StringComparison.OrdinalIgnoreCase)
            || message.Contains("nije", StringComparison.OrdinalIgnoreCase)
            || message.Contains("mora", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Navedite", StringComparison.OrdinalIgnoreCase))
        {
            return message;
        }

        return "Zahtjev sadrži neispravnu vrijednost polja.";
    }
}
