
Kør denne kommando fra min egen pc i terminalen så jeg bare kan overføre .exe filen
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:IncludeNativeLibrariesForSelfExtract=true

# 🖥️ Installation af ScreenTimer

Denne guide forklarer, hvordan du opsætter ScreenTimer på en Windows-PC, så den starter automatisk og synkroniserer med Todo-appen.

## 📥 Trin 1: Hent programmet

### Mulighed A: Hent kildekoden (Kræver .NET SDK)
1. Log på PC'en og gå til: `https://github.com/Kobogo/ScreenTimer`
2. Tryk på den grønne knap **Code** og vælg **Download ZIP**.
3. Udpak mappen et sted, hvor den må blive liggende (f.eks. `C:\Programmer\ScreenTimer`).

### Mulighed B: Brug den færdige .exe (Anbefales)
*Hvis du har kørt `dotnet publish` på din egen maskine, skal du kun overføre den ene fil fra `publish` mappen til hans PC.*

---

## 🚀 Trin 2: Konfiguration & Start

1. **Find filen:** Gå til mappen med programmet.
2. **Kør programmet:** Dobbeltklik på `ScreenTimer.exe`.
3. **Windows Beskyttelse:** Da det er dit eget program, vil Windows advare dig. Tryk på **"Flere oplysninger"** og derefter **"Kør alligevel"**.
4. **Ikon:** Tjek at programmet dukker op nede i systembakken (ved siden af uret) med det lille ur-ikon.

---

## ⚙️ Trin 3: Opsæt automatisk start (Vigtigt!)

For at skærmtiden tæller hver dag, skal programmet starte sammen med Windows:

1. Tryk på `Windows-tasten + R` på tastaturet.
2. Skriv `shell:startup` og tryk på **Enter**. En mappe åbner nu.
3. Højreklik på din `ScreenTimer.exe` i din programmed-mappe og vælg **Opret genvej**.
4. Træk denne nye genvej ind i mappen, du lige åbnede (`Startup` mappen).

**Nu starter timeren automatisk, når computeren tændes.**

---

## 🛠️ Fejlsøgning

* **Ingen forbindelse:** Hvis der står "OFFLINE", skal du tjekke om PC'en har internet, og om Render-API'et kører.
* **NaN i Dashboardet:** Sørg for at `_userId` i koden er sat korrekt til din søns ID (normalt `3`).
* **Mangler ikon:** Sørg for at `timer.ico` ligger i samme mappe som `.exe` filen.

---

*Lavet med ❤️ af Far (og Gemini)*