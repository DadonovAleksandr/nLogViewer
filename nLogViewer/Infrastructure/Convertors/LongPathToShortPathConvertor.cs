using System;
using System.Globalization;
using System.Windows.Data;
using System.IO;
using System.Text;

namespace nLogViewer.Infrastructure.Convertors;

[ValueConversion(typeof(string), typeof(string))]
public class LongPathToShortPathConvertor : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is string strValue)) return null;
        var pathParts = strValue.Split(Path.DirectorySeparatorChar);
        if (pathParts.Length > 6)
        {
            var retData = new StringBuilder();
            retData.Append(Path.Combine(pathParts[0], pathParts[1], pathParts[3]));
            retData.Append("...");
            retData.Append(Path.Combine(pathParts[pathParts.Length - 2], pathParts[pathParts.Length - 1]));

            return retData.ToString();
        }
        
        return strValue;
        
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}