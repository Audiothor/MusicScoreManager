using System.Globalization;

namespace MusicScoreManager.Converters
{
    public class ScaleToFontSizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double scale)
            {
                return 22.0 * scale;
            }
            return 22.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
