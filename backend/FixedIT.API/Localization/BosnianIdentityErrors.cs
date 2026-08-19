using Microsoft.AspNetCore.Identity;

namespace FixedIT.API.Localization;

public static class BosnianIdentityErrors
{
    public static string Build(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Code switch
        {
            "ConcurrencyFailure" => "Korisnički nalog je u međuvremenu izmijenjen. Pokušajte ponovo.",
            "PasswordMismatch" => "Unesena lozinka nije ispravna.",
            "InvalidToken" => "Sigurnosni token nije ispravan.",
            "LoginAlreadyAssociated" => "Ova prijava je već povezana s drugim nalogom.",
            "InvalidUserName" => "Korisničko ime nije ispravno.",
            "InvalidEmail" => "Email adresa nije ispravna.",
            "DuplicateUserName" => "Nalog s ovim korisničkim imenom već postoji.",
            "DuplicateEmail" => "Nalog s ovom email adresom već postoji.",
            "InvalidRoleName" => "Naziv uloge nije ispravan.",
            "DuplicateRoleName" => "Uloga s ovim nazivom već postoji.",
            "UserAlreadyHasPassword" => "Korisnički nalog već ima postavljenu lozinku.",
            "UserLockoutNotEnabled" => "Zaključavanje ovog korisničkog naloga nije omogućeno.",
            "UserAlreadyInRole" => "Korisnik već ima odabranu ulogu.",
            "UserNotInRole" => "Korisnik nema odabranu ulogu.",
            "PasswordTooShort" => "Lozinka je prekratka.",
            "PasswordRequiresUniqueChars" => "Lozinka nema dovoljan broj različitih znakova.",
            "PasswordRequiresNonAlphanumeric" => "Lozinka mora sadržavati poseban znak.",
            "PasswordRequiresDigit" => "Lozinka mora sadržavati broj.",
            "PasswordRequiresLower" => "Lozinka mora sadržavati malo slovo.",
            "PasswordRequiresUpper" => "Lozinka mora sadržavati veliko slovo.",
            "RecoveryCodeRedemptionFailed" => "Kod za oporavak nije ispravan.",
            _ => "Operacija nad korisničkim nalogom nije uspjela."
        }));
    }
}
