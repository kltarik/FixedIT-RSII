# Testne instrukcije - dorade prema povratnim informacijama

## Šta je implementirano

Implementirano je svih 17 stavki iz `IB200112-avgust-ok.md`: CRUD referentnih podataka, puna admin izmjena korisnika i profesionalaca, serverska pretraga/filteri, moderacija recenzija sa razlogom, slobodni termini i kategorija rezervacije, fotografije oglasa, sortiranje po završenim poslovima, detalj recenzije, reset lozinke, trajne obavijesti i historija statusa, završeni PayPal tok, jedinstvena EUR valuta, dva ispravna PDF izvještaja, posljedice verifikacije, objašnjive preporuke, potpuna suspenzija naloga uz očuvanje historije, UTC round-trip, izolacija post-commit obavijesti i RabbitMQ retry sa backoffom.

## 1. Preduslovi i pokretanje sistema

1. Otvoriti PowerShell u root direktoriju:

```powershell
Set-Location C:\Users\TarikK\Desktop\dev\FixedIT-RSII
```

2. Raspakovati `.env-tajne.zip` u `.env` koristeći šifru dostavljenu uz projekat. Provjeriti da `.env` sadrži PayPal sandbox i ostale potrebne vrijednosti.

3. Pokrenuti cijeli sistem i sačekati health provjeru:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify-stack.ps1
docker compose ps
Invoke-RestMethod http://localhost:5000/health
```

Očekivanje: svi servisi su `running`, a health odgovor je `Healthy`. API pri startu automatski primjenjuje migracije na bazu `200112`.

4. Ako start ne uspije, prikazati dijagnostiku:

```powershell
docker compose logs --tail 150 fixedit-api
docker compose logs --tail 150 fixedit-notifications
docker compose logs --tail 100 sqlserver
```

## 2. Build i automatizovani testovi

```powershell
Set-Location C:\Users\TarikK\Desktop\dev\FixedIT-RSII\backend
dotnet build FixedIT.sln -c Release --no-restore

Set-Location ..\desktop\fixedit_desktop
flutter analyze
flutter test

Set-Location ..\..\mobilne\fixedit_mobile
flutter analyze
flutter test
```

PASS: .NET build ima 0 upozorenja i 0 grešaka, desktop ima 5/5 testova, mobilna aplikacija 6/6 testova, a oba analyzera prijavljuju `No issues found`.

## 3. Windows Desktop aplikacija

Release build sa lokalnim API-jem:

```powershell
Set-Location C:\Users\TarikK\Desktop\dev\FixedIT-RSII\desktop\fixedit_desktop
flutter build windows --release --dart-define=API_BASE_URL=http://localhost:5000
$release = (Resolve-Path .\build\windows\x64\runner\Release).Path
Start-Process -FilePath "$release\fixedit_desktop.exe" -WorkingDirectory $release
```

Prijaviti se administratorskim nalogom navedenim u `README.md`. Cijeli `Release` direktorij mora ostati na okupu jer EXE koristi prateće DLL i data fajlove.

## 4. Android AVD emulator

```powershell
Set-Location C:\Users\TarikK\Desktop\dev\FixedIT-RSII\mobilne\fixedit_mobile
flutter build apk --release --dart-define=API_BASE_URL=http://10.0.2.2:5000
adb devices
adb install -r .\build\app\outputs\flutter-apk\app-release.apk
adb shell monkey -p com.example.fixedit_mobile 1
adb logcat -c
```

Za snimak ekrana tokom provjere:

```powershell
adb exec-out screencap -p > "$env:USERPROFILE\Desktop\fixedit-test.png"
```

## 5. Fizički Android uređaj

1. Uključiti Developer options i USB debugging te spojiti telefon i računar na istu Wi-Fi/LAN mrežu.
2. Komandom `ipconfig` pronaći IPv4 adresu aktivnog mrežnog adaptera, npr. `192.168.1.10`.
3. Na telefonu u browseru otvoriti `http://<IP_RAČUNARA>:5000/health`. Mora se prikazati `Healthy`. Ako ne radi, dozvoliti dolazni TCP port 5000 u Windows Firewallu.
4. Izgraditi APK posebno za tu adresu i instalirati ga:

```powershell
Set-Location C:\Users\TarikK\Desktop\dev\FixedIT-RSII\mobilne\fixedit_mobile
$pcIp = "192.168.1.10" # zamijeniti stvarnom adresom
flutter build apk --release "--dart-define=API_BASE_URL=http://${pcIp}:5000"
adb devices
adb install -r .\build\app\outputs\flutter-apk\app-release.apk
adb shell monkey -p com.example.fixedit_mobile 1
```

PASS: aplikacija se pokreće, prijava radi i liste se učitavaju preko lokalnog API-ja.

## 6. API pomoćne komande

Lozinke unesite iz vlastitog `.env` fajla; nemojte ih zapisivati u ovaj dokument niti u Git.

```powershell
$base = "http://localhost:5000"
$adminPassword = Read-Host "SEED_ADMIN_PASSWORD"
$clientPassword = Read-Host "SEED_USER_PASSWORD"
$professionalPassword = $clientPassword

function Login-FixedIT([string]$email, [string]$password) {
    Invoke-RestMethod -Method Post -Uri "$base/api/auth/login" -ContentType "application/json" -Body (@{
        email = $email
        password = $password
    } | ConvertTo-Json)
}

$admin = Login-FixedIT "admin@fixedit.local" $adminPassword
$client = Login-FixedIT "client1@fixedit.local" $clientPassword
$professional = Login-FixedIT "professional1@fixedit.local" $professionalPassword
$adminHeaders = @{ Authorization = "Bearer $($admin.token)" }
$clientHeaders = @{ Authorization = "Bearer $($client.token)" }
$professionalHeaders = @{ Authorization = "Bearer $($professional.token)" }
```

Negativna auth provjera:

```powershell
try { Invoke-WebRequest "$base/api/admin/users" -SkipHttpErrorCheck } catch { $_.Exception.Response.StatusCode.value__ }
Invoke-WebRequest "$base/api/admin/users" -Headers $clientHeaders -SkipHttpErrorCheck | Select-Object StatusCode
```

PASS: bez tokena je 401, a klijentski token na admin ruti dobija 403.

## 7. Funkcionalni scenariji po stavkama

### 1. Referentni podaci i baza

1. U desktop aplikaciji otvoriti `Referentni podaci`.
2. Kreirati testnu državu, grad u toj državi i kategoriju; zatim svaku izmijeniti.
3. Pokušati obrisati državu prije grada. Očekuje se poslovna greška, ne HTTP 500.
4. Obrisati prvo grad, zatim kategoriju i državu.
5. Izmijeniti opis statusa rezervacije, provjeriti prikaz i vratiti izvornu vrijednost.
6. Provjeriti `docker compose logs fixedit-api` i potvrditi primjenu migracija nad bazom `200112`.

PASS: create/edit/delete rade, relacijski zaštićeno brisanje daje jasnu bosansku poruku, a API nema 500.

### 2. Admin uređivanje i status profesionalca

1. Na ekranu `Korisnici` urediti ime, telefon i grad testnog korisnika, osvježiti listu i potvrditi trajnost.
2. Na ekranu `Profesionalci` urediti opis, satnicu, iskustvo i kategorije.
3. Zasebno promijeniti `Verifikovan` i `Aktivan`; jedna kontrola ne smije automatski promijeniti drugu.
4. Pokušati deaktivirati nalog sa aktivnom rezervacijom. Očekuje se odbijanje.
5. Vratiti sve testne podatke i status naloga.

PASS: server validira podatke, odvojeni statusi se pravilno prikazuju i aktivna rezervacija blokira suspenziju.

### 3. Serverska pretraga i filteri

1. Na `Korisnici` tražiti email/ime korisnika koji nije na trenutno otvorenoj stranici.
2. Na `Profesionalci`, `Oglasi` i `Rezervacije` promijeniti filter nakon odlaska na drugu stranicu.
3. Provjeriti da se nakon promjene filtera učitava stranica 1 i da rezultat može doći iz cijelog skupa.
4. API provjera korisničke pretrage:

```powershell
Invoke-RestMethod "$base/api/admin/users?page=1&pageSize=1&search=client2%40fixedit.local" -Headers $adminHeaders
```

PASS: `total` odgovara filteru nezavisno od prethodne stranice; UI ne prikazuje nepotrebni interni user GUID.

### 4. Moderacija recenzija

1. Otvoriti `Recenzije`, koristiti tekstualnu pretragu i filter ocjene.
2. Kliknuti uklanjanje i pokušati potvrditi prazan razlog. UI mora ostati otvoren sa validacijskom porukom.
3. Ukloniti testnu recenziju sa jasnim razlogom.
4. U `Evidencija aktivnosti` pronaći zapis `Review / Deleted` i potvrditi da detalji sadrže uneseni razlog.
5. Direktni DELETE bez body-ja mora vratiti 400, ne 500.

PASS: razlog je obavezan i trajno evidentiran, recenzija nestaje, a prosječna ocjena se ponovo izračuna.

### 5. Kategorija rezervacije i slobodni termini

1. Kao profesionalac podesiti najmanje jedan period dostupnosti u profilu.
2. Kao klijent otvoriti detalj tog profesionalca, odabrati jednu od njegovih kategorija, datum, trajanje i ponuđeni slobodni slot.
3. Kreirati rezervaciju i potvrditi kategoriju i UTC/lokalno vrijeme na detalju.
4. Pokušati rezervisati isti preklapajući slot. Mora biti odbijen ili izostavljen iz liste slotova.
5. API spot test, uz stvarne vrijednosti:

```powershell
Invoke-RestMethod "$base/api/professionals/<PROFILE_ID>/available-slots?date=2026-09-10&categoryId=<CATEGORY_ID>&durationMinutes=60"
```

PASS: nije moguće poslati proizvoljno vrijeme niti kategoriju koju profesionalac ne nudi.

### 6. Fotografije, sortiranje i detalj recenzije

1. Kao klijent kreirati oglas i dodati jednu ili više JPG/PNG fotografija problema.
2. Otvoriti oglas ponovo i potvrditi prikaz galerije; obrisati jednu sliku.
3. Pokušati uploadovati tekstualni fajl preimenovan u `.jpg`. Mora biti odbijen MIME/magic-byte validacijom.
4. U pretrazi profesionalaca izabrati `Najviše poslova`; uporediti redoslijed sa završenim rezervacijama.
5. Otvoriti profesionalca, kliknuti pojedinačnu recenziju i potvrditi master-detail prikaz komentara, ocjene, autora i datuma.
6. API sortiranje:

```powershell
Invoke-RestMethod "$base/api/professionals/search?sortBy=completed&sortOrder=desc&page=1&pageSize=20"
```

PASS: redoslijed dolazi iz API-ja, fotografije su trajne i lažna slika je odbijena bez 500.

### 7. Zaboravljena lozinka

1. Na mobilnoj prijavi otvoriti `Zaboravljena lozinka` i poslati zahtjev za postojeći email.
2. Otvoriti MailHog na `http://localhost:8025`, pronaći kod i postaviti novu jaku lozinku.
3. Potvrditi prijavu novom lozinkom i neuspjeh stare lozinke.
4. Ponovna upotreba istog koda, pogrešan kod i istekao kod moraju biti odbijeni.
5. Za nepostojeći email odgovor ne smije otkriti postoji li nalog.

PASS: kod je jednokratan i vremenski ograničen, validacija lozinke je prikazana uz polja.

### 8. Obavijesti, nova ponuda i historija statusa

1. Kao profesionalac poslati ponudu na klijentov otvoreni oglas.
2. Bez ručnog osvježavanja potvrditi SignalR obavijest klijentu `Nova ponuda`.
3. Označiti je pročitanom; mora ostati u listi sa read stanjem i nakon ponovnog otvaranja ekrana.
4. Provesti rezervaciju kroz više statusa i otvoriti detalj rezervacije.

PASS: timeline je hronološki, prikazuje aktera/razlog, a pročitane obavijesti ne nestaju.

### 9. PayPal i refund pravila

1. Na `Pending`, `Accepted` i `InProgress` rezervaciji potvrditi da klijent nema dugme za plaćanje i da direktni create-order vraća 400.
2. Profesionalac vodi rezervaciju redom `Accepted -> InProgress -> Completed`.
3. Tek tada klijent bira PayPal; checkout se otvara unutar aplikacije, prikazuje konačni iznos u EUR i automatski obrađuje povratnu adresu.
4. Ne koristiti ručno dugme tipa `Platio/la sam`; stanje smije postati plaćeno samo nakon provjerenog capture/webhook rezultata.
5. Na zasebnoj `Accepted` rezervaciji kao profesionalac izabrati `Otkaži`, unijeti razlog i potvrditi `Cancelled`.
6. Ako postoji historijski plaćena `Accepted` rezervacija, otkazivanje mora pokrenuti idempotentan PayPal refund.

PASS: nedozvoljeni statusi vraćaju 400, završeni posao omogućava checkout, dupli capture/refund ne duplira uplatu.

### 10. Jedinstvena EUR valuta

1. Pregledati početnu, pretragu, profil profesionalca, oglase, rezervacije, plaćanje, dashboard i izvještaje.
2. Provjeriti `.env` vrijednost `PAYPAL_CURRENCY=EUR`.
3. Provjeriti da create-order iznos brojčano odgovara `Reservation.TotalPrice` i da PayPal prikazuje EUR.

PASS: nigdje u UI/API/payment payloadu nema `KM` ili `BAM`, niti implicitne konverzije.

### 11. Finansijski i profesionalni PDF izvještaji

1. Na desktop `Izvještaji` postaviti period i kategoriju.
2. Uporediti prihod po kategoriji sa rezervacijama: jedna uplata pripada samo kategoriji sačuvanoj na rezervaciji.
3. Preuzeti finansijski PDF i PDF uspješnosti profesionalaca.
4. Otvoriti oba dokumenta, pregledati dijakritiku, zbir, filtere, redove i prelom stranica.
5. Pokrenuti `Ispiši` za oba dokumenta i zatvoriti dijalog bez stvarnog štampanja ako printer nije dostupan.

PASS: zbir nije uvećan brojem trenutnih kategorija profesionalca, oba PDF-a se otvaraju i nude ispis.

### 12. Posljedice verifikacije

1. Registrovati novi profesionalni nalog; dovršiti profil i portfolio dok je neverifikovan.
2. Kao klijent potvrditi da se taj profil ne pojavljuje u javnoj listi, pretrazi i preporukama te da direktni URL vraća 404.
3. Kao neverifikovan profesionalac pokušati poslati ponudu; mora biti odbijeno.
4. Pokušati kreirati rezervaciju prema neverifikovanom profesionalcu; mora biti odbijeno.
5. Administrator verifikuje nalog; ponoviti i potvrditi da javne funkcije sada rade.

PASS: vlastiti profil/portfolio ostaju dostupni, javne poslovne radnje zavise od verifikacije.

### 13. Preporuke i objašnjivost

1. Kao klijent otvoriti nekoliko profila i pretražiti kategoriju; zatim kreirati rezervaciju.
2. Restartovati samo API da se model ponovo trenira pri startu:

```powershell
docker compose restart fixedit-api
Start-Sleep -Seconds 15
Invoke-RestMethod http://localhost:5000/health
```

3. Otvoriti početni ekran i pregledati preporuke.

PASS: svaki rezultat ima konkretno bosansko objašnjenje; signali koriste težine rezervacija 5, pregleda 2 i pretrage 1; cold start daje rang po reputaciji i završenim poslovima.

### 14. Suspenzija i poslovna historija

1. Prijaviti testnog korisnika i sačuvati access/refresh tok.
2. Administrator deaktivira korisnika bez aktivne rezervacije.
3. Ponoviti API poziv starim access tokenom i pokušati refresh. Oba moraju vratiti 401.
4. Druga ovlaštena strana i administrator i dalje moraju vidjeti stare rezervacije, uplate, recenzije i poruke sa oznakom `neaktivan`.
5. Aktivirati korisnika i potvrditi novu prijavu.

PASS: suspenzija je trenutna, refresh tokeni su opozvani, historija nije sakrivena.

### 15. UTC round-trip

1. Kreirati rezervaciju za jasno prepoznatljiv lokalni termin.
2. Pregledati sirovi API JSON detalja rezervacije i polja `scheduledAt`, `createdAt`, `updatedAt` i `statusHistory[].changedAt`.

```powershell
(Invoke-RestMethod "$base/api/reservations/<ID>" -Headers $clientHeaders) | ConvertTo-Json -Depth 8
```

3. Vrijednosti trenutaka moraju imati završno `Z`; mobilna aplikacija ih mora prikazati kao očekivano lokalno vrijeme bez pomaka od jednog ili dva sata.

PASS: ponovni API read nakon upisa zadržava UTC oznaku i Flutter `toLocal()` daje tačan termin.

### 16. Greške SignalR/RabbitMQ nakon commita

1. Zaustaviti RabbitMQ pa kreirati rezervaciju ili promijeniti status:

```powershell
docker compose stop rabbitmq
```

2. Poslovna operacija mora vratiti uspješan 2xx i ostati sačuvana iako se sekundarna obavijest loguje kao greška.
3. Ponovo pokrenuti broker:

```powershell
docker compose start rabbitmq
docker compose logs --tail 100 fixedit-api
```

PASS: nema lažnog HTTP business failurea nakon commita i nema duplog unosa pri osvježavanju.

### 17. RabbitMQ retry i DLQ

1. Zaustaviti MailHog, a zatim pokrenuti događaj koji šalje email, npr. reset lozinke:

```powershell
docker compose stop mailhog
docker compose logs -f fixedit-notifications
```

2. U logu moraju biti pokušaji i odmaci približno 1, 2, 4 i 8 sekundi; poruka ne smije odmah završiti u DLQ.
3. Nakon iscrpljenih pokušaja provjeriti dead-letter queue u RabbitMQ UI-ju `http://localhost:15672`.
4. Ponovo pokrenuti MailHog:

```powershell
docker compose start mailhog
```

PASS: uspješna poruka dobija ACK, privremeni kvar se ponavlja ograničeno, a tek konačni neuspjeh ide u DLQ.

## 8. Regresijski fokus

- Provjeriti da admin filteri nikada ne rade samo nad trenutno učitanom stranicom.
- Provjeriti da deaktivacija i verifikacija nisu zamijenjene i da historijski podaci ostaju vidljivi.
- Provjeriti vlasništvo: drugi klijent ne može mijenjati tuđi oglas, rezervaciju, sliku ili recenziju.
- Provjeriti state machine bez preskakanja i obavezan razlog za svako otkazivanje/moderaciju.
- Provjeriti PayPal sandbox idempotentnost, `payer-action` povratni link i 401 za neuspješnu webhook verifikaciju.
- Provjeriti da gašenje RabbitMQ/SignalR servisa ne može poništiti već commitovanu poslovnu operaciju.
- Tokom svih scenarija pratiti `docker compose logs fixedit-api`; nijedan očekivani 4xx scenario ne smije proizvesti 500 ili stack trace klijentu.

## 9. Već izvršeno i preostalo

Već izvršeno 03.09.2026: .NET Debug i Release build (0 upozorenja/0 grešaka), oba `flutter analyze`, mobilni testovi 6/6, desktop testovi 5/5, Docker Compose config, Docker image build, Windows Release build i Android Release APK build.

Puni Docker health/E2E nije izvršen jer `.env` nije raspakovan u ovom repozitoriju. PayPal sandbox, SMTP/MailHog, SignalR, retry/DLQ, fizički Android uređaj, vizuelni PDF pregled i ručni UI scenariji ostaju za gore opisanu provjeru korisnika.

## Kriteriji prolaza (PASS/FAIL)

- [ ] Svih 17 funkcionalnih scenarija prolazi bez neočekivanog HTTP 500.
- [ ] Autorizacija i vlasništvo daju očekivane 401/403/404 odgovore.
- [ ] Buildovi, analyzeri i testovi prolaze.
- [ ] API, SQL Server, RabbitMQ, notification worker i MailHog su zdravi sa stvarnim `.env` fajlom.
- [ ] Desktop i Android UI prikazuju bosanski tekst, tačno lokalno vrijeme i isključivo EUR.
- [ ] Oba PDF-a su vizuelno ispravna i mogu se preuzeti i poslati na ispis.
- [ ] Nema commitovanih tajni, `.env` fajla ni release binarnih artefakata.
