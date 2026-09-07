# FixedIT

FixedIT je seminarski projekat iz predmeta Razvoj softvera II. Platforma povezuje klijente kojima je potrebna usluga s verifikovanim profesionalcima, uz oglase za posao, rezervacije, komunikaciju, plaćanje, recenzije i preporuke.

## Arhitektura

- `backend/FixedIT.API` - glavni ASP.NET Core 10 REST API
- `backend/FixedIT.NotificationService` - pozadinski servis za asinhrone notifikacije putem RabbitMQ-a
- `backend/FixedIT.Shared` - dijeljeni modeli između servisa
- `mobilne/fixedit_mobile` - Flutter Android aplikacija za klijente i profesionalce
- `desktop/fixedit_desktop` - Flutter Windows aplikacija za administratore
- SQL Server 2022 - relaciona baza podataka
- RabbitMQ 3.13 - razmjena poruka između servisa
- MailHog - lokalni SMTP za razvoj

## Preduslovi

- .NET SDK 10
- Flutter stable 3.22 ili noviji
- Docker Desktop s Docker Compose podrškom
- ADB i Android Studio AVD za testiranje mobilne aplikacije
- Windows razvojni alati za Flutter desktop za pokretanje desktop aplikacije

## Pokretanje

### 1. Konfiguracija

Raspakirati `.env-tajne.zip` šifrom dostavljenom uz predaju rada i smjestiti dobijeni `.env` fajl u root direktorij projekta:

```powershell
# primjer sa 7-Zip
& "C:\Program Files\7-Zip\7z.exe" e .env-tajne.zip -o. -p<šifra>
```

Referentne vrijednosti i opis svih varijabli nalaze se u `.env.example`.

### 2. Pokretanje servisa

```powershell
docker compose up --build -d
```

Provjera da je API spreman:

```powershell
Invoke-RestMethod http://localhost:5000/health
# Očekivani odgovor: Healthy
```

MailHog razvojni inbox dostupan je na `http://localhost:8025`. Kod koji se
pošalje nakon izbora **Zaboravili ste lozinku?** nalazi se u posljednjoj poruci
za unesenu email adresu.

### 3. Windows Desktop aplikacija

Pokrenuti:

```powershell
desktop\fixedit_desktop\build\windows\x64\runner\Release\fixedit_desktop.exe
```

API adresa je `http://localhost:5000` i ugrađena je u release build.

### 4. Android mobilna aplikacija

Pokrenuti Android Virtual Device u Android Studiju, a zatim instalirati APK:

```powershell
adb install -r mobilne\fixedit_mobile\build\app\outputs\flutter-apk\app-release.apk
```

API adresa je `http://10.0.2.2:5000`, standardna AVD adresa za host računar, i ugrađena je u release build.

### 5. PayPal sandbox plaćanje

Za kompletan test plaćanja mora se koristiti **Personal** PayPal sandbox nalog
koji nije isti nalog kao **Business** sandbox merchant povezan s vrijednostima
`PAYPAL_CLIENT_ID` i `PAYPAL_CLIENT_SECRET`. Korištenje merchant naloga kao
kupca PayPal odbija porukom `COMPLIANCE_VIOLATION`.

Podaci za FixedIT testne korisnike iz naredne tabele nisu PayPal podaci. Za
plaćanje se koristi Personal sandbox kupac `fixedit_klijenti@personal.example.com`.
Njegova lozinka nalazi se u šifrovanoj `.env-tajne.zip` arhivi kao
`PAYPAL_TEST_BUYER_PASSWORD`, dok je email zapisan kao
`PAYPAL_TEST_BUYER_EMAIL`. Business sandbox nalog predstavlja prodavca, a
Personal sandbox nalog kupca.
Detaljne PayPal upute dostupne su u [Sandbox accounts dokumentaciji](https://developer.paypal.com/sandbox-testing/accounts/).

Nakon što klijent odobri plaćanje u ugrađenom PayPal prozoru, aplikacija
automatski presreće povratni URL i poziva serversku potvrdu naplate. Uspješan
capture ažurira status plaćanja, zaradu profesionalca i završne izvještaje.

## Testni nalozi

Sljedeći nalozi automatski se kreiraju pri prvom pokretanju sistema:

| Uloga | Email | Lozinka | Aplikacija |
|---|---|---|---|
| Administrator | `admin@fixedit.local` | `Admin@FixedIT2026!` | Desktop |
| Klijent | `client1@fixedit.local` | `User@FixedIT2026!` | Mobilna |
| Klijent | `client2@fixedit.local` | `User@FixedIT2026!` | Mobilna |
| Klijent | `client3@fixedit.local` | `User@FixedIT2026!` | Mobilna |
| Profesionalac | `professional1@fixedit.local` | `User@FixedIT2026!` | Mobilna |
| Profesionalac | `professional2@fixedit.local` | `User@FixedIT2026!` | Mobilna |
| Profesionalac | `professional3@fixedit.local` | `User@FixedIT2026!` | Mobilna |

Lozinke odgovaraju vrijednostima `SEED_ADMIN_PASSWORD` i `SEED_USER_PASSWORD` iz `.env` fajla.

## Dokumentacija

Kompletna tehnička dokumentacija, uključujući arhitekturu, API katalog, konfiguraciju i isporuku, nalazi se u [documentation.md](documentation.md).

Detaljan opis sistema preporuke nalazi se u [recommender-dokumentacija.md](recommender-dokumentacija.md).
