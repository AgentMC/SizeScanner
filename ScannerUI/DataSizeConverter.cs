using System;
using System.Globalization;
using System.Windows.Data;
using ScannerCore;

namespace ScannerUI
{
    public class DataSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var source = (FsItem) value;
            return Humanize.FsItem(source);
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
