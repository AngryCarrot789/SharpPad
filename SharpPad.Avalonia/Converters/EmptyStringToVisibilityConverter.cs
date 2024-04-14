// 
// Copyright (c) 2023-2024 REghZy
// 
// This file is part of SharpPad.
// 
// SharpPad is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// SharpPad is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
// 

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SharpPad.Avalonia.Converters;

public class EmptyStringToBoolConverter : IValueConverter
{
    public object NullValue { get; set; }

    public object EmptyValue { get; set; }

    public object NonEmptyValue { get; set; }

    public object NonStringValue { get; set; }
    public object UnsetValue { get; set; }

    public bool ThrowForUnset { get; set; }
    public bool ThrowForNonString { get; set; }

    public EmptyStringToBoolConverter()
    {
        this.UnsetValue = AvaloniaProperty.UnsetValue;
        this.NonStringValue = AvaloniaProperty.UnsetValue;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.Length < 1 ? this.EmptyValue : this.NonEmptyValue;
        }
        else if (value == AvaloniaProperty.UnsetValue)
        {
            return this.ThrowForUnset ? throw new Exception("Unset value not allowed") : this.UnsetValue;
        }
        else if (value == null)
        {
            return this.NullValue;
        }
        else if (this.ThrowForNonString)
        {
            throw new Exception("Expected string, got " + value);
        }
        else
        {
            return this.NonStringValue;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}