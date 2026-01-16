namespace GooglePhotoManager.Utils
{
    /// <summary>
    /// Classe per visualizzare una barra di avanzamento nella console.
    /// </summary>
    internal class ConsoleProgressBar
    {
        private readonly int _totalItems;
        private readonly int _barWidth;
        private readonly string _operationName;
        private int _currentItem;
        private int _lastPercentage;

        /// <summary>
        /// Crea una nuova barra di avanzamento.
        /// </summary>
        /// <param name="totalItems">Numero totale di elementi da processare.</param>
        /// <param name="operationName">Nome dell'operazione (es. "Download", "Upload").</param>
        /// <param name="barWidth">Larghezza della barra in caratteri (default 30).</param>
        public ConsoleProgressBar(int totalItems, string operationName, int barWidth = 30)
        {
            _totalItems = totalItems > 0 ? totalItems : 1;
            _operationName = operationName;
            _barWidth = barWidth;
            _currentItem = 0;
            _lastPercentage = -1;
        }

        /// <summary>
        /// Aggiorna la barra di avanzamento con il file corrente.
        /// </summary>
        /// <param name="currentFileName">Nome del file attualmente in elaborazione.</param>
        public void Update(string currentFileName)
        {
            _currentItem++;
            int percentage = (int)((_currentItem * 100.0) / _totalItems);

            // Calcola quanti caratteri riempire nella barra
            int filledWidth = (int)((_currentItem * _barWidth) / _totalItems);
            int emptyWidth = _barWidth - filledWidth;

            // Costruisci la barra
            string progressBar = new string('█', filledWidth) + new string('░', emptyWidth);

            // Tronca il nome del file se troppo lungo
            string displayFileName = currentFileName;
            int maxFileNameLength = Console.WindowWidth - _barWidth - 25;
            if (maxFileNameLength < 10) maxFileNameLength = 10;

            if (displayFileName.Length > maxFileNameLength)
            {
                displayFileName = "..." + displayFileName.Substring(displayFileName.Length - maxFileNameLength + 3);
            }

            // Pulisci la riga e scrivi il progresso
            Console.Write($"\r{_operationName}: [{progressBar}] {percentage,3}% ({_currentItem}/{_totalItems}) {displayFileName}".PadRight(Console.WindowWidth - 1));

            _lastPercentage = percentage;
        }

        /// <summary>
        /// Completa la barra di avanzamento e va a capo.
        /// </summary>
        public void Complete()
        {
            string progressBar = new string('█', _barWidth);
            Console.Write($"\r{_operationName}: [{progressBar}] 100% ({_totalItems}/{_totalItems}) Completato!".PadRight(Console.WindowWidth - 1));
            Console.WriteLine();
        }
    }
}
