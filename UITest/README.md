# Selenium UI-tests

Testene bruger xUnit og Selenium WebDriver til at afprøve den rigtige frontend i Google Chrome:

- Søgning i titel og kunstner, delvise match, store/små bogstaver og danske tegn.
- Mellemrum omkring søgningen samt en søgning med kun mellemrum.
- Filtrering på udgivelsesår og unikke årstal i faldende rækkefølge.
- Kombination og ændring af søgning og årsfilter.
- Ingen resultater, resultatantal, rydning af enkelte filtre og knappen **Nulstil**.

## Kørsel

Kræver .NET 10 SDK og Google Chrome. Kør fra denne mappe:

```powershell
dotnet test
```

Testene kan også køres fra Test Explorer i Visual Studio. Chrome kører som standard uden et synligt vindue. For at se browseren:

```powershell
$env:UITEST_HEADLESS = "false"
dotnet test
```

Selenium Manager finder/henter normalt den passende ChromeDriver automatisk. Hvis du allerede har en driver, kan mappen med `chromedriver.exe` angives i miljøvariablen `CHROMEDRIVER_DIRECTORY`.

## Testopsætning

Testprojektet kopierer `index.html` og `App.js` fra `Frontend/Frontend` til sin egen outputmappe ved build. Kildeprojektets placering i forhold til `UITest` skal derfor bevares. Ingen filer i frontend- eller API-mappen ændres.

En lokal testserver starter automatisk på en ledig port og leverer seks faste musiknumre. Den erstatter kun API-adressen i JavaScript-svaret i hukommelsen, så frontenden henter testdata fra testserveren. Søgning og filtrering udføres af den eksisterende frontendkode, og Selenium bruger sidens inputfelt, dropdown og knap. Testene dækker frontendens funktioner; de tester ikke REST API'ets implementering eller forbindelsen til API'et på port 7295.

REST API'et og en separat frontendserver behøver ikke køre. Der kræves internetadgang til frontendens eksisterende Vue-, Axios- og Bootstrap-CDN'er samt eventuel første hentning af NuGet-pakker og ChromeDriver. Browser og testserver lukkes efter testkørslen, og siden genindlæses før hver test.

Seleniums officielle introduktion: https://www.selenium.dev/documentation/webdriver/getting_started/
