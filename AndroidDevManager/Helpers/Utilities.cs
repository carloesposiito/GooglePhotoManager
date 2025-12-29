using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GooglePhotoTransferTool.Helpers
{
    internal static class Utilities
    {
        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            BitmapImage operationResult = null;

            try
            {
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memory;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    operationResult = bitmapImage;
                }
            }
            catch (Exception exception)
            {
                operationResult = null;
                MessageBox.Show("Errore durante la conversione da Bitmap a BitmapImage:\n" +
                    $"{exception.Message}");
            }

            return operationResult;
        }
    }
}
