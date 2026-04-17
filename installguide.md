
Kør denne kommando fra min egen pc i terminalen.
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true

# 🖥️ Installation af ScreenTimer

Denne guide forklarer, hvordan du opsætter ScreenTimer på en Windows-PC, så den synkroniserer med Todo-appen.

## 📥 Trin 1: Find filerne i C:\udvikling\C#\ScreenTimer\bin\Release\net9.0-windows\win-x64

## Trin 2: Lav en Zip-fil af samtlige filer og kald den ScreenTimerVx (giv den et nyt versions nummer)

## Trin 3: Læg Zip-filen ned på en usb-stick

## Trin 4: Overfør og udpak filerne til :D\ScreenTimer

## Trin 5: Start programmet via ScreenTimer.exe og sæt appen fast i processlinjen.



**Mulig løsning på udfordringen

Ja, det gør faktisk en stor forskel – men måske ikke på den måde, du tror!

Selvom din app er en "rigtig" Windows-app (WPF) og ikke en hjemmeside, så bruger moderne WPF-apps ofte WebView2-kontrollen til at vise indhold, hvis du f.eks. bruger biblioteker, der minder om Blazor Hybrid, eller hvis du har indsat et browser-vindue i din XAML.

Hvis du IKKE bruger WebView2 overhovedet, men kun standard WPF-elementer (Labels, Buttons, Grids), så er her de 3 ting, der typisk går galt, når en WPF-app kører perfekt på udvikler-maskinen, men "hænger" i proceslinjen på en anden PC:

1. Manglende "WPF Desktop Runtime"
Selvom du kører --self-contained, kan WPF-apps nogle gange fejle, hvis de specifikke Windows Desktop-komponenter i operativsystemet er forældede eller mangler.

Løsning: Installer .NET 9 Desktop Runtime (x64) på din søns PC. Det tager 2 minutter og udelukker versionsfejl i Windows-bibliotekerne.

2. Skærmopløsning og "Vindue uden for skærmen"
Dette er en klassisk WPF-fejl! Hvis du på din egen PC har gemt vinduets position (f.eks. i en konfigurationsfil eller i koden), og din søns PC har en anden opløsning eller færre skærme, kan WPF finde på at tegne vinduet "uden for" det synlige område.

Tjek: Hold musen over ikonet i proceslinjen på din søns PC. Vises der et lille preview af vinduet? Hvis ja, så findes vinduet, det er bare usynligt/gemt.

Løsning: Prøv at sætte WindowStartupLocation="CenterScreen" i din MainWindow.xaml.

3. "Unblock" af DLL-filer (Meget vigtigt for WPF)
Når du udpakker en WPF-app fra en zip-fil, blokerer Windows ofte de underliggende DLL-filer af sikkerhedshensyn. En WPF-app består af mange små filer, og hvis én af dem (f.eks. din API-service DLL) er blokeret, fejler appen lydløst.

Prøv denne specifikke rækkefølge på din søns PC:

Slet den nuværende mappe med appen.

Højreklik på Zip-filen.

Vælg Egenskaber.

Sæt flueben i Fjern blokering (Unblock) helt nederst og tryk OK.

Udpak nu filerne til en mappe på hans Skrivebord.

Hvordan ser vi fejlen? (Event Viewer er din ven)
Da det er en WPF-app, vil Windows næsten altid logge en "Application Crash", hvis den ikke kan starte ordentligt.

Højreklik på Start-knappen og vælg Logbog (Event Viewer).

Gå til Windows-logfiler -> Program.

Kig efter en fejl med kilden ".NET Runtime".

I beskrivelsen vil der stå noget i stil med: Exception Info: System.IO.FileNotFoundException eller System.Net.Http.HttpRequestException.

Et hurtigt spørgsmål til din kode:
Bruger du System.Text.Json eller et andet bibliotek til at læse din API? Hvis du bruger Auto-start (altså at appen starter med Windows), kan den nogle gange fejle, fordi netværket ikke er klar endnu, når appen prøver at kontakte din API (userId=3).

Prøv at køre den via CMD (Kommandoprompt) på hans PC – det tvinger ofte fejlen frem i lyset!
