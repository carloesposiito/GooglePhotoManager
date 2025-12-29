using System;
using System.IO;
using System.IO.Compression;

namespace GooglePhotoTransferTool.Helpers
{
    internal static class FileManager
    {
        /// <summary>
        /// Restores platform tools inside passed folder path.
        /// </summary>
        /// <param name="platformToolsPath">Path to destination folder.</param>
        public static void RestorePlatformTools(string platformToolsPath)
        {
            // First of all recreate zip archive in program directory
            string currentDirectory = Directory.GetCurrentDirectory();
            string pathToArchive = $"{currentDirectory}\\PlatformTools.zip";
            CopyPlatformToolsZip(pathToArchive);

            // At this point passed path shoud not exists (checked before)
            // Additional check just in case then recreate folder
            if (Directory.Exists(platformToolsPath))
            {
                Directory.Delete(platformToolsPath, true);
            }
            Directory.CreateDirectory(platformToolsPath);

            // Extract zip archive
            ExtractZip(pathToArchive, platformToolsPath);

            // Delete zip file at the end
            if (File.Exists(pathToArchive))
            {
                File.Delete(pathToArchive);
            }
        }

        /// <summary>
        /// Extract zip (given its filename) to passed destination folder.
        /// </summary>
        /// <param name="zipFilename">Zip archive filename.</param>
        /// <param name="destinationFolder">Destination folder.</param>
        private static void ExtractZip(string zipFilename, string destinationFolder)
        {
            using (var archive = ZipFile.OpenRead(zipFilename))
            {
                foreach (var entry in archive.Entries)
                {
                    string destinationPath = Path.GetFullPath(Path.Combine(destinationFolder, entry.FullName));

                    // Zip slip protection
                    if (!destinationPath.StartsWith(Path.GetFullPath(destinationFolder), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException("Unsafe zip file!");
                    }

                    // Directory
                    if (string.IsNullOrEmpty(entry.Name)) 
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    string directory = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Extract with overwrite flag set to true just in case
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }

        /// <summary>
        /// Creates platform tools zip archive if not existing.<br/>
        /// Checks if file exists at the end of the operation.<br/>
        /// Throws exception if something wrong.
        /// </summary>
        /// <param name="zipFilename">Zip archive filename (full path to file).</param>
        private static void CopyPlatformToolsZip(string zipFilename)
        {
            if (!File.Exists(zipFilename))
            {
                var platformToolsZip = Properties.Resources.PlatformTools;
                if (platformToolsZip != null && platformToolsZip.Length > 0)
                {
                    // Get info about folder where zip will be extracted
                    string destinationFolder = Path.GetDirectoryName(zipFilename);

                    // Check that folder exists
                    if (!string.IsNullOrWhiteSpace(destinationFolder))
                    {
                        // Create directory if doesn't exist
                        if (!Directory.Exists(destinationFolder))
                        {
                            Directory.CreateDirectory(destinationFolder);
                        }

                        // Save zip file
                        File.WriteAllBytes(zipFilename, platformToolsZip);
                    }
                    else
                    {
                        throw new Exception("Empty destination path!");
                    }
                }
                else
                {
                    throw new Exception("Platform zip tools archive damaged!");
                }

                // Check again bc at this point archive should exist
                if (!File.Exists(zipFilename))
                {
                    throw new Exception("Error recreating platform tools zip archive!");
                }
            }
        }
    }
}
