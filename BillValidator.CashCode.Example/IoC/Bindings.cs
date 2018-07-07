using BillValidator.CashCode.Driver;
using BillValidator.CashCode.Example.NavigationService;
using BillValidator.CashCode.Example.Pages.MainExample;
using BillValidator.CashCode.Example.Pages.MainWindow;

namespace BillValidator.CashCode.Example.IoC
{
    public static class Bindings
    {
        public static void RegisterAllBindings()
        {
            RegisterBillValidator();
            RegisterPages();
        }

        private static void RegisterPages()
        {
            var myNavigationService = Container.Instance.Resolve<NavigationService.NavigationService>();
            Container.Instance.Register<INavigationService, NavigationService.NavigationService>(myNavigationService);

            Container.Instance.Register<IMainExampleViewModel, MainExampleViewModel>();

            var mainWindowViewModel = Container.Instance.Resolve<MainWindowViewModel>();
            Container.Instance.Register<IMainWindowViewModel, MainWindowViewModel>(mainWindowViewModel);
        }

        private static void RegisterBillValidator()
        {
            Container.Instance.Register<ICashCodeBillValidator, CashCodeBillValidator>();
        }
    }
}
