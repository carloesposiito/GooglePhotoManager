using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AndroidDevManager.Converters
{
    internal class Converter_BooleanToVisibility : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility operationResult = Visibility.Collapsed;

            if (value != null && value is bool booleanValue)
            {
                operationResult = booleanValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return operationResult;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
