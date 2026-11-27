using System.Globalization;
using System.Windows.Data;
using iPart.App.ViewModels;

namespace iPart.App.Controls;

/// <summary>Comparator: czy komórka (wiersz + kolumna po ścieżce wiązania) zawiera wartość różną od P&amp;P — wtedy pomarańczowe tło i pogrubienie.</summary>
public sealed class ComparatorCellDifference : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Length == 2 && values[0] is BomLineItem line && values[1] is string member && line.CellDiffers(member);

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
