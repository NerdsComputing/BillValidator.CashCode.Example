using BillValidator.CashCode.Example.IoC;
using MahApps.Metro.Controls;

namespace BillValidator.CashCode.Example.Pages.MainWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Bindings.RegisterAllBindings();

            DataContext = Container.Instance.Resolve<IMainWindowViewModel>();
        }
    }
}
