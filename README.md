# K4U1 - Movie Look Up

Detta är ett REST API byggt i ASP.NET Core som integrerar mot TMDB och OMDb. 

### Installation & Körning
1. Klona repot: `git clone <din-url>`
2. Navigera till mappen: `cd K4U1`
3. Kör applikationen: `dotnet run`

API:et körs som standard på `https://localhost:7157/swagger` (eller den port som visas i terminalen).

## Konfiguration (User Secrets)
För att köra API:et behöver du sätta upp följande secrets lokalt:

```bash
dotnet user-secrets set "ExternalApis:TmdbToken" "DIN_TMDB_TOKEN"
dotnet user-secrets set "ExternalApis:OmdbApiKey" "DIN_OMDB_NYCKEL"
```

## Prestandamätning
<img width="257" height="84" alt="image" src="https://github.com/user-attachments/assets/50579980-a826-4e35-a340-33d6e68ba3a1" />
