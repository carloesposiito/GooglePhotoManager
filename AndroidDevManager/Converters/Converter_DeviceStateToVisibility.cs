using AdvancedSharpAdbClient.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AndroidDevManager.Converters
{
    internal class Converter_DeviceStateToVisibility : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility operationResult = Visibility.Collapsed;

            if (value != null && value is DeviceData device)
            {
                operationResult = device.State.Equals(DeviceState.Unauthorized) ? Visibility.Visible : Visibility.Collapsed;
            }

            return operationResult;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
