using Auth0.OidcClient;

namespace ChatAppRum
{
    public partial class App : Application
    {
        public App()
        {
            //if (Auth0.OidcClient.Platforms.Windows.Activator.Default.CheckRedirectionActivation())
            //    return;
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}
