using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AdvancedFileViewer_WPF.Converters
{
    class FileStatusConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
                Uri uri;
                    uri = System.Convert.ToBoolean(value)
                        ? new Uri($"pack://application:,,,/Images/Change_Enabled.png")
                        : new Uri($"pack://application:,,,/Images/Change_Disabled.png");

                BitmapImage source = new BitmapImage(uri);
                return source;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
