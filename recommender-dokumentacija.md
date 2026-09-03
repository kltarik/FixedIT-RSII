# FixedIT sistem preporuke

## 1. Problem koji sistem rješava

Sistem rangira verifikovane profesionalce za prijavljenog klijenta. Cilj je da klijent prvo vidi profesionalce povezane s njegovim stvarnim ponašanjem u aplikaciji, a da novi korisnik i dalje dobije smislen poredak.

## 2. Algoritam

Implementiran je ML.NET `MatrixFactorizationTrainer`. `UserId` i `ProfessionalProfileId` mapiraju se u key kolone, a vrijednost interakcije koristi se kao `Label`. Parametri modela (`Seed`, `NumberOfIterations`, `ApproximationRank`, `MinimumTrainingRatings`, `RetrainingIntervalHours` i `CandidatePoolSize`) dolaze iz konfiguracijske sekcije `Recommendations`.

## 3. Ulazni podaci i težine

Trening kombinuje tri vrste ponašanja:

| Signal | Izvor | Težina |
|---|---|---:|
| Rezervacija koja nije otkazana | `Reservations` | 5 |
| Pregled detalja profesionalca | `RecommendationActivities`, `ProfileView` | 2 |
| Pretraga po kategoriji | `RecommendationActivities`, `CategorySearch` | 1 |

Za pretragu kategorije signal se tokom pripreme treninga povezuje sa svim trenutno verifikovanim profesionalcima koji nude tu kategoriju. Otkazane rezervacije i neverifikovani profesionalci ne ulaze u trening. Recenzije se i dalje čuvaju u `UserRatings` radi evidencije postojeće funkcionalnosti, ali nisu ulaz u ovaj model aktivnosti.

## 4. Trening i ponovno treniranje

`ModelRetrainingService` je ASP.NET Core `BackgroundService`. Model trenira odmah nakon pokretanja API-ja i zatim periodično, prema `RetrainingIntervalHours` (zadano 24 sata). Za svaki ciklus kreira se DI scope i koristi scoped `AppDbContext`.

Ako ukupan broj signala ne dostigne `MinimumTrainingRatings`, trening se preskače. Uspješno istreniran model i skup poznatih korisnika/profesionalaca zamjenjuju prethodno stanje unutar zaključane sekcije singleton servisa.

## 5. Generisanje preporuka

Endpoint `GET /api/recommendations` dostupan je klijentu i razmatra najviše `CandidatePoolSize` verifikovanih profesionalaca. Za poznatog korisnika i profesionalca koristi se ML.NET predikcija. Rezultati se zatim stabilno sortiraju po izračunatom rangu, javnoj ocjeni i identifikatoru.

Kada predikcija nije dostupna, cold-start rang se računa kao:

`AverageRating * log(CompletedReservations + 1)`

API vraća profil, kategorije, javnu i predviđenu ocjenu, oznaku `isPersonalized` i tekst `explanation`.

## 6. Objašnjivost

Za svaki rezultat API bira najkonkretniji dostupni razlog ovim redom:

1. klijent je ranije rezervisao tog profesionalca;
2. klijent je ranije pregledao taj profil;
3. profesionalac nudi kategoriju za koju klijent ima najjači zbir signala rezervacija i pretraga;
4. fallback objašnjenje navodi javnu ocjenu i broj završenih poslova.

Mobilna početna stranica prikazuje `explanation` neposredno uz preporučenog profesionalca. Objašnjenje opisuje evidentiran poslovni signal; ne pokušava tumačiti latentne faktore ML modela.

## 7. Ograničenja

- Novi korisnik nema personalizovane interakcije i zato dobija cold-start poredak.
- Novi profesionalac bez završenih poslova ima slabiji cold-start signal.
- Pretraga kategorije se povezuje sa svim profesionalcima te kategorije i ne zna koji je rezultat korisnik stvarno namjeravao odabrati.
- Model se osvježava periodično, pa novi signal utiče na ML predikciju tek nakon sljedećeg treninga ili ponovnog pokretanja API-ja.
- Matrica je rijetka na malom skupu korisnika i profesionalaca.

## 8. Relevantna implementacija

- `backend/FixedIT.API/BackgroundServices/ModelRetrainingService.cs`
- `backend/FixedIT.API/Services/ML/RecommendationService.cs`
- `backend/FixedIT.API/Services/RecommendationActivityService.cs`
- `backend/FixedIT.API/Services/RecommendationQueryService.cs`
- `backend/FixedIT.API/Models/RecommendationActivity.cs`
- `backend/FixedIT.API/DTOs/Recommendations/RecommendationResponse.cs`
