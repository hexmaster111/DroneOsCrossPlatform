using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DroneOsCrossPlatform.BindingConverters;

public class TechnicalConverter: IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null or not int)
            throw new ArgumentException("TechnicalConverter: Value must be a string");
        
        //Convert the string to an int then turn the int to a formatted string
        return "0x"+((int)value).ToString("X2");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        //Not doing that.
        throw new NotImplementedException();
    }
}