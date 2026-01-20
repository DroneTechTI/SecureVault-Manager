using Microsoft.UI.Xaml.Data;

namespace SecureVault.App.Converters;

public class BoolNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue ? !boolValue : value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue ? !boolValue : value;
    }
}
