﻿using System;
using System.Threading.Tasks;
using Auth0.OidcClient;
using ChatRumDataAccess;  // Import your SqliteDataAccess class
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Required for SecureStorage
using Microsoft.Extensions.DependencyInjection;

namespace ChatAppRum
{
    public partial class MainPage : ContentPage
    {
        private readonly Auth0Client _auth0Client;
        //private readonly HubConnection _hubConnection;
        //private readonly SqliteDataAccess _sqliteDataAccess;
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _serviceProvider;

        // Update constructor to accept IServiceProvider along with other dependencies
        public MainPage(IServiceProvider serviceProvider, /*HubConnection hubConnection,*/ HttpClient httpClient)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            //_hubConnection = hubConnection;
            _httpClient = httpClient;

            // Initialize Auth0 client with your domain and client ID
            _auth0Client = new Auth0Client(new Auth0ClientOptions
            {
                Domain = "dev-ugkqfy8x63krryr7.us.auth0.com",
                ClientId = "fBsExHdlXsQMOowNYNsiEjEJvCxPm1Tg",
                RedirectUri = "myapp://callback",
                PostLogoutRedirectUri = "myapp://callback",
                Scope = "openid profile email"
            });

            // Check if the user is already logged in
            CheckLoginStatus();
        }

        private async void CheckLoginStatus()
        {
            // Check if a token already exists
            var accessToken = await SecureStorage.GetAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                // If a token exists, navigate directly to the chat room overview page using IServiceProvider to get RoomPage instance
                var roomPage = _serviceProvider.GetRequiredService<RoomPage>();
                await Navigation.PushAsync(roomPage);
            }
        }

        private async void OnGoogleLoginClicked(object sender, EventArgs e)
        {
            try
            {
                // Check if a token already exists
                var existingToken = await SecureStorage.GetAsync("access_token");
                if (!string.IsNullOrEmpty(existingToken))
                {
                    // If token exists, navigate directly to the chat room overview page
                    var roomPage = _serviceProvider.GetRequiredService<RoomPage>();
                    await Navigation.PushAsync(roomPage);
                    return; // Skip login as token is valid
                }

                // If no token exists, proceed with Auth0 login
                var loginResult = await _auth0Client.LoginAsync(new { prompt = "login" });

                if (!loginResult.IsError)
                {
                    // Save the token securely
                    await SecureStorage.SetAsync("access_token", loginResult.AccessToken);

                    // On successful login, navigate to the chat room overview
                    var roomPage = _serviceProvider.GetRequiredService<RoomPage>();
                    await Navigation.PushAsync(roomPage);
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

                // Clear any stored tokens using SecureStorage
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
