using Auth0.OidcClient;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAppRum
{
    public partial class App : Application
    {
        public App()
        {

            InitializeComponent();

            MainPage = new AppShell();
           // MainPage = serviceProvider.GetRequiredService<AppShell>();
        }
    }
}
