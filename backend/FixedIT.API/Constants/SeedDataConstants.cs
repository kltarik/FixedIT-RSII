namespace FixedIT.API.Constants;

public static class SeedDataConstants
{
    public const int SeededReservationCount = 5;
    public const int DefaultDurationMinutes = 120;

    public static readonly string[] CityNames =
    [
        "Sarajevo",
        "Mostar",
        "Banja Luka",
        "Tuzla",
        "Zenica"
    ];

    public static readonly (string Name, string Description)[] Categories =
    [
        ("Vodoinstalater", "Vodoinstalaterske instalacije i popravke"),
        ("Električar", "Električne instalacije i servis"),
        ("Klima tehničar", "Ugradnja i održavanje klima uređaja"),
        ("Moler", "Krečenje i završni zidarski radovi"),
        ("Čišćenje", "Profesionalno čišćenje prostora"),
        ("Majstor", "Opći kućni popravci i održavanje"),
        ("Prevoz", "Prevoz stvari i pomoć pri selidbi")
    ];

    public static readonly SeedUser[] Clients =
    [
        new("client1@fixedit.local", "Amina", "Softić", "Sarajevo", "+38761111001"),
        new("client2@fixedit.local", "Ivan", "Marić", "Mostar", "+38761111002"),
        new("client3@fixedit.local", "Lejla", "Hodžić", "Tuzla", "+38761111003")
    ];

    public static readonly SeedProfessional[] Professionals =
    [
        new(
            new SeedUser("professional1@fixedit.local", "Emir", "Hadžić", "Sarajevo", "+38762222001"),
            "Pouzdan vodoinstalater za kućne i poslovne objekte.",
            45m,
            8,
            ["Vodoinstalater", "Majstor"]),
        new(
            new SeedUser("professional2@fixedit.local", "Ana", "Kovač", "Mostar", "+38762222002"),
            "Licencirani električar i serviser klima uređaja.",
            55m,
            6,
            ["Električar", "Klima tehničar"]),
        new(
            new SeedUser("professional3@fixedit.local", "Marko", "Petrović", "Banja Luka", "+38762222003"),
            "Specijalista za čišćenje, krečenje i završne radove.",
            35m,
            5,
            ["Čišćenje", "Moler"])
    ];

    public static readonly SeedReservation[] Reservations =
    [
        new(0, 0, "Popravka curenja u kupatilu", 10, 110m, 5, "Brza i kvalitetna popravka."),
        new(1, 1, "Zamjena električne razvodne kutije", 9, 220m, 4, "Profesionalno obavljen posao."),
        new(2, 2, "Generalno čišćenje stana", 8, 140m, 5, "Stan je detaljno očišćen."),
        new(0, 1, "Servis klima uređaja", 7, 95m, 4, "Uredan servis i dobar savjet."),
        new(1, 0, "Montaža kuhinjske slavine", 6, 75m, 3, "Posao je završen prema dogovoru.")
    ];
}

public sealed record SeedUser(
    string Email,
    string FirstName,
    string LastName,
    string CityName,
    string PhoneNumber);

public sealed record SeedProfessional(
    SeedUser User,
    string Bio,
    decimal HourlyRate,
    int YearsOfExperience,
    string[] CategoryNames);

public sealed record SeedReservation(
    int ClientIndex,
    int ProfessionalIndex,
    string ServiceDescription,
    int DaysAgo,
    decimal TotalPrice,
    int Rating,
    string ReviewComment);
