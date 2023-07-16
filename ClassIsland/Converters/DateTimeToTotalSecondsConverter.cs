﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class TimeSpanToTotalSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var r = (TimeSpan)value;
        return r.TotalSeconds.ToString("#");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}