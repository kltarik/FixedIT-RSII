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

Za fizički Android uređaj računar i telefon moraju biti na istoj mreži. APK se tada ponovo gradi sa LAN IPv4 adresom računara:

```powershell
$pcIp = "192.168.1.10" # zamijeniti stvarnom LAN IPv4 adresom računara
Set-Location mobilne\fixedit_mobile
flutter build apk --release "--dart-define=API_BASE_URL=http://${pcIp}:5000"
adb install -r build\app\outputs\flutter-apk\app-release.apk
adb shell monkey -p com.example.fixedit_mobile 1
```

Windows Firewall mora dozvoliti dolazni TCP promet na portu `5000`.

## Testni nalozi

Sljedeći nalozi automatski se kreiraju pri prvom pokretanju sistema:

| Uloga | Email | Lozinka | Aplikacija |
|---|---|---|---|
| Administrator | `admin@fixedit.local` | `Admin_FixedIT_2026!` | Desktop |
| Klijent | `client1@fixedit.local` | `User_FixedIT_2026!` | Mobilna |
| Klijent | `client2@fixedit.local` | `User_FixedIT_2026!` | Mobilna |
| Klijent | `client3@fixedit.local` | `User_FixedIT_2026!` | Mobilna |
| Profesionalac | `professional1@fixedit.local` | `User_FixedIT_2026!` | Mobilna |
| Profesionalac | `professional2@fixedit.local` | `User_FixedIT_2026!` | Mobilna |
| Profesionalac | `professional3@fixedit.local` | `User_FixedIT_2026!` | Mobilna |

Lozinke odgovaraju vrijednostima `SEED_ADMIN_PASSWORD` i `SEED_USER_PASSWORD` iz `.env` fajla.

## Dokumentacija

Kompletna tehnička dokumentacija, uključujući arhitekturu, API katalog, konfiguraciju i isporuku, nalazi se u [documentation.md](documentation.md).

Detaljan opis sistema preporuke nalazi se u [recommender-dokumentacija.md](recommender-dokumentacija.md).
