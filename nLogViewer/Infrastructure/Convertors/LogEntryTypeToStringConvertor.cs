using System;
using System.Globalization;
using System.Windows.Data;
using nLogViewer.Model;

namespace nLogViewer.Infrastructure.Convertors;

public class LogEntryTypeToStringConvertor : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is LogEntryType type)) return null;
        return type.ToString().ToUpper();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is string type)) return null;
        
        return type.ToUpper() switch
        {
            "TRACE" => LogEntryType.Trace,
            "DEBUG" => LogEntryType.Debug,
            "INFO" => LogEntryType.Info,
            "WARN" => LogEntryType.Warn,
            "ERROR" => LogEntryType.Error,
            "FATAL" => LogEntryType.Fatal,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}