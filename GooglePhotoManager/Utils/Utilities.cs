namespace GooglePhotoManager.Utils
{
    /// <summary>
    /// Class related to program utilities.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Writes exception to console permitting to keep same format each time.
        /// </summary>
        internal static void DisplayException(string className, string functionName, string exceptionMessage)
        {
            if (!string.IsNullOrWhiteSpace(className) && !string.IsNullOrWhiteSpace(functionName) && !string.IsNullOrWhiteSpace(exceptionMessage))
            {
                Console.WriteLine(
                    $"***************\n" +
                    $"Eccezione nella funzione: {className}.{functionName}\n" +
                    $"{exceptionMessage}\n" +
                    $"***************\n");
            }
        }
    }
}
