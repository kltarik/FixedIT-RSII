# FixedIT sistem preporuke

## 1. Problem koji sistem rješava

FixedIT klijentima prikazuje rangiranu listu profesionalaca. Cilj sistema preporuke je da profesionalce pored opće reputacije poreda i prema procijenjenoj ocjeni koju bi konkretni klijent mogao dati konkretnom profesionalcu.

Personalizacija se zasniva na prethodnim ocjenama klijenata. Kada nema dovoljno podataka za trening, korisnik još nije poznat modelu ili profesionalac nije bio prisutan u trening podacima, sistem kao rezervnu vrijednost koristi prosječnu javnu ocjenu profesionalca. Endpoint zato ostaje funkcionalan i za nove korisnike.

## 2. Odabrani algoritam

Implementiran je ML.NET `MatrixFactorizationTrainer`. Matrična faktorizacija modelira rijetku matricu u kojoj redovi predstavljaju profesionalce, kolone korisnike, a poznate vrijednosti njihove ocjene od 1 do 5.

Prije treniranja ML.NET transformacije `MapValueToKey` kodiraju tekstualni `UserId` i identifikator profesionalnog profila u ključne kolone. Trener zatim uči latentne reprezentacije korisnika i profesionalaca i iz njih procjenjuje nedostajuće ocjene.

Parametri dolaze iz sekcije `Recommendations` u konfiguraciji:

| Parametar | Zadana vrijednost | Namjena |
|---|---:|---|
| `Seed` | 42 | Ponovljivost treninga |
| `NumberOfIterations` | 20 | Broj iteracija optimizacije |
| `ApproximationRank` | 100 | Broj latentnih faktora |
| `MinimumTrainingRatings` | 5 | Minimalan broj ocjena za trening |
| `RetrainingIntervalHours` | 24 | Period između ponovnih treninga |
| `CandidatePoolSize` | 200 | Maksimalan broj kandidata za rangiranje |

## 3. Ulazni podaci i signali

Trening koristi zapise iz tabele `UserRatings`. Svaki zapis se u `ModelRetrainingService` mapira u `UserRatingData` sa sljedećim poljima:

| Polje | Izvor | Uloga u modelu |
|---|---|---|
| `UserId` | Klijent koji je ostavio recenziju | Identifikator korisničke kolone |
| `ProfessionalId` | `ProfessionalProfileId`, pretvoren u string | Identifikator profesionalnog reda |
| `Label` | Ocjena od 1 do 5 | Vrijednost koju model uči i predviđa |

`UserRating` nastaje kada klijent ostavi recenziju za završenu rezervaciju. Zapis u bazi sadrži i `ReviewId` te UTC vrijeme nastanka, ali ta polja nisu proslijeđena ML.NET treningu.

`reservationId`, opis usluge, tekst recenzije, kategorija, grad, cijena, trajanje rezervacije i datum nisu ulazne karakteristike trenutnog modela. Kategorije i ostali podaci profesionalca vraćaju se klijentu kao dio rezultata, ali ne utiču na ML.NET predikciju.

Seed podaci sadrže pet završenih rezervacija i pripadajuće ocjene. Time je ispunjen zadani minimum od pet ocjena potreban za početni trening.

## 4. Treniranje i ponovno treniranje

`RecommendationService` je registrovan kao singleton jer trenirani model mora biti dostupan između HTTP zahtjeva i ne smije zavisiti od životnog vijeka `AppDbContext` instance. Pristup `PredictionEngine` instanci, zamjena modela i predikcija zaštićeni su zaključavanjem.

`ModelRetrainingService` je ASP.NET Core `BackgroundService`. Pri pokretanju API-ja odmah izvršava trening, a zatim koristi `PeriodicTimer` i ponavlja ga svakih `RetrainingIntervalHours`, odnosno svakih 24 sata sa zadanom konfiguracijom.

Za svaki trening servis kreira novi DI scope, iz scoped `AppDbContext` instance učita sve `UserRatings` zapise pomoću `AsNoTracking`, mapira ih u `UserRatingData` i pozove `TrainModel`. Ako ima manje od `MinimumTrainingRatings` zapisa, postojeći model se ne zamjenjuje i trening se preskače uz upozorenje u logu.

Nakon uspješnog treninga servis pamti identifikatore korisnika i profesionalaca koji su bili prisutni u skupu podataka, broj korištenih ocjena i UTC vrijeme treninga. Novi model atomski zamjenjuje prethodni unutar zaključane sekcije.

## 5. Generisanje preporuka

Zaštićeni endpoint `GET /api/recommendations` dostupan je klijentima i vraća paginirani rezultat.

`RecommendationQueryService` prvo iz baze učitava najviše `CandidatePoolSize` profesionalaca. Kandidati se početno biraju prema prosječnoj ocjeni, statusu verifikacije i identifikatoru. Za svaki kandidatni profil servis zatim traži ML.NET predikciju za prijavljenog korisnika.

Personalizovana predikcija postoji samo kada je model treniran i kada su i korisnik i profesionalac poznati modelu. Rezultat se ograničava na raspon od 1 do 5. Ako predikcija nije dostupna, kao `predictedRating` koristi se trenutni `AverageRating` profesionalca.

Kandidati se konačno sortiraju prema:

1. `predictedRating` opadajuće
2. `averageRating` opadajuće
3. identifikatoru profesionalnog profila rastuće

API za svakog profesionalca vraća identitet, sliku, grad, opis, satnicu, iskustvo, verifikaciju, prosječnu ocjenu, kategorije, `predictedRating` i `isPersonalized`.

## 6. Objašnjivost preporuka

Trenutna implementacija pruža osnovnu, ali ograničenu objašnjivost:

- `isPersonalized = true` označava da je rang rezultat ML.NET predikcije za poznatog korisnika i poznatog profesionalca.
- `isPersonalized = false` označava rezervno rangiranje zasnovano na javnoj prosječnoj ocjeni profesionalca.
- `predictedRating` prikazuje numeričku vrijednost korištenu za rangiranje.
- `averageRating`, `isVerified`, kategorije, iskustvo i satnica omogućavaju korisniku da procijeni profil nezavisno od ML rezultata.

Model ne vraća doprinose pojedinačnih latentnih faktora niti tekstualno objašnjenje tipa "preporučeno zbog kategorije". Takva tvrdnja ne bi odgovarala trenutnoj implementaciji jer kategorije nisu trening signal.

## 7. Ograničenja

- **Cold start korisnika:** novi korisnik nema historiju ocjena, pa dobija rezervno rangiranje.
- **Cold start profesionalca:** profesionalac bez ocjene u trening skupu ne može dobiti personalizovanu predikciju.
- **Mali skup podataka:** početnih pet seed ocjena dovoljno je za pokretanje modela, ali nije dovoljno za stabilnu personalizaciju u realnom sistemu.
- **Rijetka matrica:** većina korisnika neće ocijeniti većinu profesionalaca, što smanjuje količinu zajedničkih signala.
- **Samo eksplicitna ocjena:** model ne koristi pregled profila, pretrage, klikove, rezervacije bez recenzije ili ponovljene angažmane.
- **Bez konteksta usluge:** kategorija, grad, cijena i termin ne utiču na predikciju.
- **Periodično osvježavanje:** nova ocjena utiče na model tek nakon sljedećeg treninga ili ponovnog pokretanja API-ja.
- **Ograničen skup kandidata:** model rangira samo do `CandidatePoolSize` profesionalaca koje inicijalni SQL upit odabere prema općoj reputaciji i verifikaciji.

## 8. Relevantna implementacija

- `backend/FixedIT.API/Services/ML/RecommendationService.cs`
- `backend/FixedIT.API/Services/ML/UserRatingData.cs`
- `backend/FixedIT.API/BackgroundServices/ModelRetrainingService.cs`
- `backend/FixedIT.API/Services/RecommendationQueryService.cs`
- `backend/FixedIT.API/Models/UserRating.cs`
- `backend/FixedIT.API/Configuration/RecommendationOptions.cs`
- `backend/FixedIT.API/DTOs/Recommendations/RecommendationResponse.cs`
- `backend/FixedIT.API/Controllers/RecommendationsController.cs`
