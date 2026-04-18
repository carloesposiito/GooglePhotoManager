# Google Photo Manager

App desktop cross-platform (Windows, Linux, macOS) per gestire e fare backup di foto tra dispositivi Android via ADB. Scritta in C# con [Avalonia UI](https://avaloniaui.net/) e .NET 9.0.

## Requisiti

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- ADB — incluso su Windows; su Linux/macOS va installato (`sudo apt install adb` / `brew install android-platform-tools`)
- Dispositivo Android con Debug USB o Wireless ADB abilitato

## Compilazione

### Da terminale

```bash
dotnet restore
dotnet build -c Release
dotnet run --project GooglePhotoManager
```

### Visual Studio 2022 (Windows)

1. Installa il workload **".NET Desktop Development"**
2. (Opzionale) Installa l'estensione **"Avalonia for Visual Studio 2022"** da Extensions > Manage Extensions
3. Apri `GooglePhotoManager/GooglePhotoManager.csproj`
4. `Ctrl+Shift+B` per compilare, `F5` per avviare

### JetBrains Rider (Windows / Linux / macOS)

1. Apri `GooglePhotoManager/GooglePhotoManager.csproj`
2. `Shift+F10` per avviare

### VS Code (Windows / Linux / macOS)

1. Installa l'estensione **C# Dev Kit**
2. Apri la cartella del progetto
3. `dotnet run --project GooglePhotoManager` dal terminale integrato

## Pubblicazione

```bash
# Windows
dotnet publish -c Release -r win-x64

# Linux
dotnet publish -c Release -r linux-x64

# macOS Intel
dotnet publish -c Release -r osx-x64

# macOS Apple Silicon
dotnet publish -c Release -r osx-arm64

# Linux ARM (Raspberry Pi)
dotnet publish -c Release -r linux-arm64
```

Output in `bin/Release/net9.0/<runtime>/publish/`.

Aggiungere `--self-contained true` per includere il runtime .NET nell'eseguibile (~80MB).

## Configurazione

Al primo avvio viene creato `config.xml`:

```xml
<Configuration>
  <BackupDevice>
    <Model>Pixel_5</Model>
    <Product>redfin</Product>
  </BackupDevice>
</Configuration>
```

Per trovare Model e Product: `adb devices -l`

## Licenza

MIT
