using System;
using System.Threading.Tasks;
using Auth0.OidcClient;
using IdentityModel.OidcClient;
using Microsoft.Maui.Controls;

namespace ChatAppRum
{
    public partial class MainPage : ContentPage
    {
        private readonly Auth0Client _auth0Client;

        public MainPage()
        {
            InitializeComponent();

            // Initialize Auth0 client with your domain and client ID
            _auth0Client = new Auth0Client(new Auth0ClientOptions
            {
                Domain = "dev-ugkqfy8x63krryr7.us.auth0.com",
                ClientId = "fBsExHdlXsQMOowNYNsiEjEJvCxPm1Tg",
                RedirectUri = "myapp://callback",
                PostLogoutRedirectUri = "myapp://callback",
                Scope = "openid profile email"
            });
        }

        private async void OnGoogleLoginClicked(object sender, EventArgs e)
        {
            try
            {
                // Pass 'prompt=login' via extra parameters
                var loginResult = await _auth0Client.LoginAsync(new { prompt = "login" });

                if (!loginResult.IsError)
                {
                    // On successful login, navigate to the chat room overview
                    await Navigation.PushAsync(new ChatRoomOverviewPage());
                }
                else
                {
                    await DisplayAlert("Login Error", $"An error occurred during login: {loginResult.Error}", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle login failure
                await DisplayAlert("Login Error", $"An error occurred during login: {ex.Message}", "OK");
            }
        }


        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                await _auth0Client.LogoutAsync();

                // Clear any stored tokens using SecureStorage.RemoveAsync
                SecureStorage.Remove("access_token");

                await DisplayAlert("Logged Out", "You have been logged out successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Logout Error", $"An error occurred during logout: {ex.Message}", "OK");
            }
        }


    }
}
