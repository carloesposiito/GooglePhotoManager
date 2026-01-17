# Google Photo Manager

Applicazione console per la gestione e il backup di foto tra dispositivi Android tramite ADB.

## Requisiti

- .NET 8.0 Runtime (o SDK per compilare)
- ADB (Android Debug Bridge)
- Dispositivo Android con "Debug USB" o "Wireless ADB" abilitato

---

## Lingue supportate

L'applicazione supporta le seguenti lingue:
- **Italiano** (default)
- **English**

La lingua puo' essere cambiata dal menu **Impostazioni** > **Lingua** e viene salvata nel file di configurazione.

---

## Installazione per piattaforma

### Windows

**1. Installa .NET 8.0 Runtime**

Scarica e installa da: https://dotnet.microsoft.com/download/dotnet/8.0

Oppure con winget:
```powershell
winget install Microsoft.DotNet.Runtime.8
```

**2. ADB**

ADB e' incluso nell'applicazione (Platform Tools embedded). Non serve installare nulla.

**3. Avvio**

```powershell
dotnet GooglePhotoManager.dll
```

Oppure se hai l'eseguibile:
```powershell
GooglePhotoManager.exe
```

---

### Linux (incluso Raspberry Pi)

**1. Installa .NET 8.0 Runtime**

Ubuntu/Debian/Raspberry Pi OS:
```bash
sudo apt update
sudo apt install dotnet-runtime-8.0
```

Se il pacchetto non e' disponibile:
```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --runtime dotnet
echo 'export PATH="$PATH:$HOME/.dotnet"' >> ~/.bashrc
source ~/.bashrc
```

**2. Installa ADB**

```bash
sudo apt install adb
```

**3. Configura permessi USB (opzionale ma consigliato)**

```bash
sudo usermod -aG plugdev $USER
echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="*", MODE="0666", GROUP="plugdev"' | sudo tee /etc/udev/rules.d/51-android.rules
sudo udevadm control --reload-rules
```

Effettua logout e login per applicare i permessi.

**4. Avvio**

```bash
dotnet GooglePhotoManager.dll
```

---

### macOS

**1. Installa .NET 8.0 Runtime**

Con Homebrew:
```bash
brew install dotnet@8
```

Oppure scarica da: https://dotnet.microsoft.com/download/dotnet/8.0

**2. Installa ADB**

```bash
brew install android-platform-tools
```

**3. Avvio**

```bash
dotnet GooglePhotoManager.dll
```

---

## Compilazione e pubblicazione

### Compilare il progetto

```bash
dotnet build -c Release
```

### Pubblicare per una piattaforma specifica

**Windows x64:**
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

**Linux x64:**
```bash
dotnet publish -c Release -r linux-x64 --self-contained false
```

**Linux ARM64 (Raspberry Pi 4/5):**
```bash
dotnet publish -c Release -r linux-arm64 --self-contained false
```

**Linux ARM32 (Raspberry Pi 3):**
```bash
dotnet publish -c Release -r linux-arm --self-contained false
```

**macOS x64:**
```bash
dotnet publish -c Release -r osx-x64 --self-contained false
```

**macOS ARM64 (Apple Silicon):**
```bash
dotnet publish -c Release -r osx-arm64 --self-contained false
```

I file pubblicati si trovano in: `bin/Release/net8.0/<runtime>/publish/`

### Pubblicazione self-contained (senza dipendenze)

Aggiungi `--self-contained true` per includere il runtime .NET nell'applicazione:

```bash
dotnet publish -c Release -r linux-arm64 --self-contained true
```

Questo crea un eseguibile standalone che non richiede .NET installato, ma il file sara' piu' grande (~80MB).

---

## Configurazione

Al primo avvio viene creato un file `config.xml` nella stessa cartella dell'eseguibile:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration>
  <BackupDevice>
    <Model>Pixel_5</Model>
    <Product>redfin</Product>
  </BackupDevice>
  <Settings>
    <Language>Italian</Language>
  </Settings>
</Configuration>
```

### Parametri configurabili

| Parametro | Descrizione | Valori |
|-----------|-------------|--------|
| `Model` | Model del dispositivo di backup | es. `Pixel_5` |
| `Product` | Product del dispositivo di backup | es. `redfin` |
| `Language` | Lingua dell'interfaccia | `Italian`, `English` |

Per trovare Model e Product del tuo dispositivo:
```bash
adb devices -l
```

Output esempio:
```
XXXXXXXX device product:redfin model:Pixel_5 transport_id:1
```

---

## Funzionalita'

### Menu Principale

| # | Funzione | Descrizione |
|---|----------|-------------|
| 1 | Scansiona dispositivi | Cerca dispositivi Android connessi |
| 2 | Connetti dispositivo | Connessione via ADB Wireless |
| 3 | Abbina dispositivo | Prima associazione ADB Wireless |
| 4 | Trasferisci file in Documents | Copia file dal PC al dispositivo |
| 5 | Backup cartella dispositivo | Copia una cartella dal dispositivo al PC |
| 6 | Trasferisci foto a dispositivo backup | Sincronizza foto tra dispositivi |
| 7 | Impostazioni | Configura dispositivo backup e lingua |
| 8 | Ricarica dispositivi | Aggiorna lista dispositivi |
| 9 | Esci | Chiude l'applicazione |

### Menu Impostazioni

| # | Funzione | Descrizione |
|---|----------|-------------|
| 1 | Imposta dispositivo di backup | Configura Model e Product del dispositivo backup |
| 2 | Lingua | Cambia la lingua dell'interfaccia (Italiano/English) |
| 0 | Indietro | Torna al menu principale |

---

## Risoluzione problemi

### ADB non trovato (Linux/macOS)

Verifica che ADB sia installato e nel PATH:
```bash
which adb
adb version
```

### Dispositivo non rilevato

1. Verifica che "Debug USB" sia abilitato sul dispositivo
2. Accetta il prompt "Consenti debug USB" sul dispositivo
3. Prova a riavviare il server ADB:
   ```bash
   adb kill-server
   adb start-server
   ```

### Errore permessi USB (Linux)

```bash
sudo adb kill-server
sudo adb start-server
adb devices
```

### Caratteri non visualizzati correttamente

Assicurati che il terminale supporti UTF-8:
```bash
export LANG=en_US.UTF-8
```

---

## Licenza

Questo progetto e' distribuito sotto licenza MIT.
