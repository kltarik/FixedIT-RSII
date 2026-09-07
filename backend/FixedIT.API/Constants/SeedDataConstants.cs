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
        new("client1@fixedit.local", "Amina", "Softić", "Sarajevo", "+38761111001", "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=512"),
        new("client2@fixedit.local", "Ivan", "Marić", "Mostar", "+38761111002", "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=512"),
        new("client3@fixedit.local", "Lejla", "Hodžić", "Tuzla", "+38761111003", "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=512")
    ];

    public static readonly SeedProfessional[] Professionals =
    [
        new(
            new SeedUser("professional1@fixedit.local", "Emir", "Hadžić", "Sarajevo", "+38762222001", "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=512"),
            "Pouzdan vodoinstalater za kućne i poslovne objekte.",
            45m,
            8,
            ["Vodoinstalater", "Majstor"]),
        new(
            new SeedUser("professional2@fixedit.local", "Ana", "Kovač", "Mostar", "+38762222002", "https://images.unsplash.com/photo-1531123897727-8f129e1688ce?w=512"),
            "Licencirani električar i serviser klima uređaja.",
            55m,
            6,
            ["Električar", "Klima tehničar"]),
        new(
            new SeedUser("professional3@fixedit.local", "Marko", "Petrović", "Banja Luka", "+38762222003", "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=512"),
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

    public static readonly SeedPortfolio[] PortfolioItems =
    [
        new(0, "Obnova kupatila", "Zamjena instalacija i završna montaža sanitarija.", "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?w=900"),
        new(1, "Nova elektroinstalacija", "Ugradnja razvodne kutije i zaštitnih elemenata.", "https://images.unsplash.com/photo-1621905252507-b35492cc74b4?w=900"),
        new(2, "Uređenje dnevnog boravka", "Priprema zidova i završno krečenje prostora.", "https://images.unsplash.com/photo-1562259949-e8e7689d7828?w=900")
    ];

    public static readonly SeedJobPosting[] JobPostings =
    [
        new(0, "Popravka odvoda u kuhinji", "Potrebna dijagnostika i popravka odvoda ispod sudopera.", "Vodoinstalater", "Sarajevo", 120m, "https://images.unsplash.com/photo-1585704032915-c3400ca199e7?w=900"),
        new(1, "Ugradnja nove utičnice", "Potrebna ugradnja dvostruke utičnice u radnoj sobi.", "Električar", "Mostar", 90m, "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=900"),
        new(2, "Krečenje spavaće sobe", "Priprema i krečenje sobe površine približno 18 m2.", "Moler", "Tuzla", 250m, "https://images.unsplash.com/photo-1589939705384-5185137a7f0f?w=900")
    ];
}

public sealed record SeedUser(
    string Email,
    string FirstName,
    string LastName,
    string CityName,
    string PhoneNumber,
    string ProfilePictureUrl);

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

public sealed record SeedPortfolio(
    int ProfessionalIndex,
    string Title,
    string Description,
    string ImageUrl);

public sealed record SeedJobPosting(
    int ClientIndex,
    string Title,
    string Description,
    string CategoryName,
    string CityName,
    decimal Budget,
    string ImageUrl);
