using System.Windows.Controls;
using System.Windows.Input;
using iPart.App.ViewModels;

namespace iPart.App.Views;

public partial class PackageBomView : UserControl
{
    public PackageBomView() => InitializeComponent();

    // iPart roboczy: panel „Dane Klienta” pokazuje pozycję pod kursorem; po zjechaniu z tabeli — pozycję zaznaczoną.
    private void CoreGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.MouseEnter -= Row_MouseEnter;
        e.Row.MouseEnter += Row_MouseEnter;
    }

    private void Row_MouseEnter(object sender, MouseEventArgs e)
    {
        if (DataContext is PackageBomViewModel vm && sender is DataGridRow { DataContext: BomLineItem line }) vm.HoveredCoreLine = line;
    }

    private void CoreGrid_MouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is PackageBomViewModel vm) vm.HoveredCoreLine = null;
    }
}
