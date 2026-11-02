using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HavenSoft.HexManiac.AvaloniaUI.Controls;

/// <summary>
/// Core's highlight layers use NUL characters for positions painted by another layer.
/// Avalonia gives those characters no advance, so replace them with ordinary spaces
/// for display. Keep the character indices, line breaks and actual editable text intact.
/// </summary>
public sealed class SyntaxLayerTextConverter : IValueConverter {
   public static string Normalize(string text) => (text ?? string.Empty).Replace('\0', ' ');

   public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      Normalize(value as string);

   public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException("Syntax highlight layers are display-only.");
}
