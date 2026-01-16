using System.Text;

namespace GooglePhotoManager.Utils
{
    /// <summary>
    /// Classe per gestire l'interfaccia grafica della console.
    /// </summary>
    internal static class ConsoleUI
    {
        #region "Constants"

        private const string BANNER = @"
   ██████╗ ██████╗ ███╗   ███╗
  ██╔════╝ ██╔══██╗████╗ ████║
  ██║  ███╗██████╔╝██╔████╔██║
  ██║   ██║██╔═══╝ ██║╚██╔╝██║
  ╚██████╔╝██║     ██║ ╚═╝ ██║
   ╚═════╝ ╚═╝     ╚═╝     ╚═╝";

        private static readonly string[] SpinnerFrames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

        #endregion

        #region "Private fields"

        private static CancellationTokenSource? _spinnerCts;
        private static Task? _spinnerTask;

        #endregion

        #region "Public methods"

        /// <summary>
        /// Mostra il banner ASCII del programma.
        /// </summary>
        /// <param name="version">Versione del programma.</param>
        public static void ShowBanner(string version)
        {
            Console.OutputEncoding = Encoding.UTF8;

            WriteColored(BANNER, ConsoleColor.Cyan);
            Console.WriteLine();
            WriteColored("    Google Photo Manager", ConsoleColor.White);
            Console.Write("  ");
            WriteColored($"v{version}", ConsoleColor.DarkGray);
            Console.WriteLine();
            Console.WriteLine();

            // Linea separatrice
            WriteColored(new string('─', 35), ConsoleColor.DarkGray);
            Console.WriteLine();
            Console.WriteLine();
        }

        /// <summary>
        /// Mostra un titolo di sezione.
        /// </summary>
        /// <param name="title">Titolo della sezione.</param>
        public static void ShowSectionTitle(string title)
        {
            Console.WriteLine();
            WriteColored($"► {title}", ConsoleColor.Yellow);
            Console.WriteLine();
            Console.WriteLine();
        }

        /// <summary>
        /// Mostra un messaggio di successo.
        /// </summary>
        /// <param name="message">Messaggio da mostrare.</param>
        public static void ShowSuccess(string message)
        {
            WriteColored("✓ ", ConsoleColor.Green);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Mostra un messaggio di errore.
        /// </summary>
        /// <param name="message">Messaggio da mostrare.</param>
        public static void ShowError(string message)
        {
            WriteColored("✗ ", ConsoleColor.Red);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Mostra un messaggio di warning.
        /// </summary>
        /// <param name="message">Messaggio da mostrare.</param>
        public static void ShowWarning(string message)
        {
            WriteColored("⚠ ", ConsoleColor.Yellow);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Mostra un messaggio informativo.
        /// </summary>
        /// <param name="message">Messaggio da mostrare.</param>
        public static void ShowInfo(string message)
        {
            WriteColored("ℹ ", ConsoleColor.Cyan);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Mostra una card utente stilizzata.
        /// </summary>
        /// <param name="index">Indice per la selezione.</param>
        /// <param name="userName">Nome dell'utente.</param>
        /// <param name="isSelected">Se true, evidenzia la card.</param>
        public static void ShowUserCard(int index, string userName)
        {
            int cardWidth = 30;
            string paddedName = userName.Length > cardWidth - 8
                ? userName.Substring(0, cardWidth - 11) + "..."
                : userName;

            WriteColored($"  ({index}) ", ConsoleColor.Cyan);
            WriteColored("╭", ConsoleColor.DarkGray);
            WriteColored(new string('─', cardWidth), ConsoleColor.DarkGray);
            WriteColored("╮", ConsoleColor.DarkGray);
            Console.WriteLine();

            Console.Write("      ");
            WriteColored("│", ConsoleColor.DarkGray);
            WriteColored("  👤 ", ConsoleColor.White);
            WriteColored(paddedName.PadRight(cardWidth - 5), ConsoleColor.White);
            WriteColored("│", ConsoleColor.DarkGray);
            Console.WriteLine();

            Console.Write("      ");
            WriteColored("╰", ConsoleColor.DarkGray);
            WriteColored(new string('─', cardWidth), ConsoleColor.DarkGray);
            WriteColored("╯", ConsoleColor.DarkGray);
            Console.WriteLine();
        }

        /// <summary>
        /// Mostra una card dispositivo stilizzata.
        /// </summary>
        /// <param name="index">Indice per la selezione.</param>
        /// <param name="model">Modello del dispositivo.</param>
        /// <param name="product">Prodotto del dispositivo.</param>
        /// <param name="name">Nome del dispositivo (opzionale).</param>
        public static void ShowDeviceCard(int index, string model, string product, string? name = null)
        {
            int cardWidth = 40;
            string deviceInfo = $"{model} ({product})";
            if (deviceInfo.Length > cardWidth - 6)
            {
                deviceInfo = deviceInfo.Substring(0, cardWidth - 9) + "...";
            }

            WriteColored($"  ({index}) ", ConsoleColor.Cyan);
            WriteColored("╭", ConsoleColor.DarkGray);
            WriteColored(new string('─', cardWidth), ConsoleColor.DarkGray);
            WriteColored("╮", ConsoleColor.DarkGray);
            Console.WriteLine();

            Console.Write("      ");
            WriteColored("│", ConsoleColor.DarkGray);
            WriteColored("  📱 ", ConsoleColor.White);
            WriteColored(deviceInfo.PadRight(cardWidth - 5), ConsoleColor.White);
            WriteColored("│", ConsoleColor.DarkGray);
            Console.WriteLine();

            if (!string.IsNullOrWhiteSpace(name))
            {
                string displayName = name.Length > cardWidth - 8
                    ? name.Substring(0, cardWidth - 11) + "..."
                    : name;

                Console.Write("      ");
                WriteColored("│", ConsoleColor.DarkGray);
                WriteColored($"     \"{displayName}\"".PadRight(cardWidth), ConsoleColor.DarkGray);
                WriteColored("│", ConsoleColor.DarkGray);
                Console.WriteLine();
            }

            Console.Write("      ");
            WriteColored("╰", ConsoleColor.DarkGray);
            WriteColored(new string('─', cardWidth), ConsoleColor.DarkGray);
            WriteColored("╯", ConsoleColor.DarkGray);
            Console.WriteLine();
        }

        /// <summary>
        /// Mostra un menu di opzioni stilizzato.
        /// </summary>
        /// <param name="options">Lista di opzioni (key = indice, value = descrizione).</param>
        public static void ShowMenu(params (string key, string description)[] options)
        {
            Console.WriteLine();
            foreach (var option in options)
            {
                WriteColored($"  [{option.key}] ", ConsoleColor.Cyan);
                Console.WriteLine(option.description);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Mostra un prompt per l'input dell'utente.
        /// </summary>
        /// <param name="message">Messaggio del prompt.</param>
        /// <returns>Input dell'utente.</returns>
        public static string? Prompt(string message)
        {
            WriteColored("→ ", ConsoleColor.Green);
            Console.Write(message);
            return Console.ReadLine();
        }

        /// <summary>
        /// Avvia uno spinner animato con un messaggio.
        /// </summary>
        /// <param name="message">Messaggio da mostrare accanto allo spinner.</param>
        public static void StartSpinner(string message)
        {
            StopSpinner();

            _spinnerCts = new CancellationTokenSource();
            var token = _spinnerCts.Token;

            _spinnerTask = Task.Run(async () =>
            {
                int frameIndex = 0;
                while (!token.IsCancellationRequested)
                {
                    Console.Write($"\r  {SpinnerFrames[frameIndex]} {message}   ");
                    frameIndex = (frameIndex + 1) % SpinnerFrames.Length;
                    try
                    {
                        await Task.Delay(80, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        /// <summary>
        /// Ferma lo spinner animato.
        /// </summary>
        public static void StopSpinner()
        {
            if (_spinnerCts != null)
            {
                _spinnerCts.Cancel();
                try
                {
                    _spinnerTask?.Wait(500);
                }
                catch { }

                _spinnerCts.Dispose();
                _spinnerCts = null;
                _spinnerTask = null;

                // Pulisce la riga dello spinner
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            }
        }

        /// <summary>
        /// Mostra un box di riepilogo.
        /// </summary>
        /// <param name="title">Titolo del box.</param>
        /// <param name="items">Elementi da mostrare (label, valore).</param>
        public static void ShowSummaryBox(string title, params (string label, string value)[] items)
        {
            int maxLabelLen = items.Max(i => i.label.Length);
            int maxValueLen = items.Max(i => i.value.Length);
            int boxWidth = Math.Max(title.Length + 4, maxLabelLen + maxValueLen + 7);

            Console.WriteLine();
            WriteColored("  ╔", ConsoleColor.Cyan);
            WriteColored(new string('═', boxWidth), ConsoleColor.Cyan);
            WriteColored("╗", ConsoleColor.Cyan);
            Console.WriteLine();

            // Titolo
            Console.Write("  ");
            WriteColored("║", ConsoleColor.Cyan);
            WriteColored($" {title}".PadRight(boxWidth), ConsoleColor.White);
            WriteColored("║", ConsoleColor.Cyan);
            Console.WriteLine();

            // Separatore
            Console.Write("  ");
            WriteColored("╟", ConsoleColor.Cyan);
            WriteColored(new string('─', boxWidth), ConsoleColor.DarkCyan);
            WriteColored("╢", ConsoleColor.Cyan);
            Console.WriteLine();

            // Items
            foreach (var item in items)
            {
                Console.Write("  ");
                WriteColored("║", ConsoleColor.Cyan);
                Console.Write($" {item.label}: ".PadRight(maxLabelLen + 3));

                // Colore basato sul valore
                ConsoleColor valueColor = item.value.ToLower() switch
                {
                    "sì" or "si" or "yes" or "completato" => ConsoleColor.Green,
                    "no" or "fallito" => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };
                WriteColored(item.value.PadRight(boxWidth - maxLabelLen - 4), valueColor);
                WriteColored("║", ConsoleColor.Cyan);
                Console.WriteLine();
            }

            Console.Write("  ");
            WriteColored("╚", ConsoleColor.Cyan);
            WriteColored(new string('═', boxWidth), ConsoleColor.Cyan);
            WriteColored("╝", ConsoleColor.Cyan);
            Console.WriteLine();
        }

        /// <summary>
        /// Pulisce lo schermo e mostra nuovamente il banner.
        /// </summary>
        /// <param name="version">Versione del programma.</param>
        public static void ClearAndShowBanner(string version)
        {
            Console.Clear();
            ShowBanner(version);
        }

        #endregion

        #region "Private methods"

        /// <summary>
        /// Scrive testo colorato nella console.
        /// </summary>
        /// <param name="text">Testo da scrivere.</param>
        /// <param name="color">Colore del testo.</param>
        private static void WriteColored(string text, ConsoleColor color)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = previousColor;
        }

        #endregion
    }
}
