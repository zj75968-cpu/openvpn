using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TopEdgeTodo.Models;

namespace TopEdgeTodo.Converters
{
    public class PriorityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Priority p)
            {
                return p switch
                {
                    Priority.High => new SolidColorBrush(Color.FromRgb(230, 85, 60)),
                    Priority.Medium => new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                    _ => new SolidColorBrush(Color.FromRgb(80, 180, 90))
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Priority.Medium;
    }
}
