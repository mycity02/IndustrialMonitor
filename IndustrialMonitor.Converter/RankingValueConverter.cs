using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace IndustrialMonitor.Converter
{
    /// <summary>
    /// 用气排行转换器
    /// </summary>
    public class RankingValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 ||
                values[0] == DependencyProperty.UnsetValue ||
                values[1] == DependencyProperty.UnsetValue ||
                values[0] is null ||
                values[1] is null ||
                !double.TryParse(values[0].ToString(), out var value) ||
                !double.TryParse(values[1].ToString(), out var width))
            {
                return 0d;
            }

            return value / 240 * width;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
