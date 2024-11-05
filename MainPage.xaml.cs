using System;
using System.Threading.Tasks;
using Auth0.OidcClient;
using ChatRumDataAccess;  // Import your SqliteDataAccess class
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Required for SecureStorage
using Microsoft.Extensions.DependencyInjection;
using ChatRumLibrary;
using System.Net.Http.Json;
using FunWithFlags_Library;

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

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // Initialize HttpClient with the custom handler
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri($"http://{NetworkUtils.GlobalIPAddress()}:5000/")  // Using HTTP here
            };

            // Log the HttpClient's BaseAddress
            Console.WriteLine($"[DEBUG] HttpClient initialized with BaseAddress: {_httpClient.BaseAddress}");

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
            var userId = await SecureStorage.GetAsync("user_id"); // Retrieve the saved user ID

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(userId))
            {
                // If a token exists, navigate directly to the chat room overview page using IServiceProvider to get RoomPage instance
                Console.WriteLine("[DEBUG] User is already logged in, navigating to RoomPage.");
                var roomPage = _serviceProvider.GetRequiredService<RoomPage>();
                roomPage.SetUserId(userId); // Set the user ID in RoomPage
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
                    Console.WriteLine("[DEBUG] Access token already exists, skipping login.");
                    // If token exists, navigate directly to the chat room overview page
                    var roomPage = _serviceProvider.GetRequiredService<RoomPage>();
                    await Navigation.PushAsync(roomPage);
                    return; // Skip login as token is valid
                }

                // If no token exists, proceed with Auth0 login
                Console.WriteLine("[DEBUG] Initiating Auth0 login.");
                var loginResult = await _auth0Client.LoginAsync(new { prompt = "login" });

                // Declare the userId variable at the start of the method
                string userId = null;
                string userName = null;

                if (!loginResult.IsError)
                {
                    Console.WriteLine("[DEBUG] Login successful.");
                    // Save the token securely
                    await SecureStorage.SetAsync("access_token", loginResult.AccessToken);

                    if (loginResult.User is not null)
                    {
                        // Extract the user's unique ID (usually "sub" from Auth0)
                        userId = loginResult.User.FindFirst(c => c.Type == "sub")?.Value;

                        if (!string.IsNullOrEmpty(userId))
                        {
                            Console.WriteLine($"[DEBUG] User ID retrieved: {userId}");
                            // Save the userId to SecureStorage
                            await SecureStorage.SetAsync("user_id", userId);

                            // Validate the user ID is saved correctly
                            var savedUserId = await SecureStorage.GetAsync("user_id");
                            Console.WriteLine($"[DEBUG] Validating saved user ID from SecureStorage: {savedUserId}");

                            // Now, create or ensure that the user is registered in MongoDB
                            userName = loginResult.User.FindFirst(c => c.Type == "nickname")?.Value ??
                                       loginResult.User.FindFirst(c => c.Type == "given_name")?.Value ??
                                       loginResult.User.FindFirst(c => c.Type == "name")?.Value;

                            // Save the username to SecureStorage
                            if (!string.IsNullOrEmpty(userName))
                            {
                                await SecureStorage.SetAsync("user_name", userName);
                                // Validate the user name is saved correctly
                                var savedUserName = await SecureStorage.GetAsync("user_name");
                                Console.WriteLine($"[DEBUG] Validating saved user name from SecureStorage: {savedUserName}");
                            }
                            else
                            {
                                Console.WriteLine("[ERROR] User name could not be retrieved from login.");
                                return;
                            }

                            // Prepare user object and send to backend to ensure registration
                            var user = new User
                            {
                                Id = userId,
                                Username = userName
                            };

                            Console.WriteLine("[DEBUG] Preparing to send user creation request to backend.");
                            var response = await _httpClient.PostAsJsonAsync("api/User/user_post", user);

                            if (!response.IsSuccessStatusCode)
                            {
                                var errorResponse = await response.Content.ReadAsStringAsync();
                                Console.WriteLine($"[ERROR] Failed to create user in MongoDB. Response: {errorResponse}");
                                await DisplayAlert("Error", $"Failed to register the user in the database. Server Response: {response.ReasonPhrase}", "OK");
                                return;
                            }
                            else
                            {
                                Console.WriteLine("[DEBUG] User created successfully in MongoDB.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[ERROR] User ID could not be retrieved. Login failed.");
                            await DisplayAlert("Login Error", "Could not retrieve user ID from login result.", "OK");
                            return;
                        }

                        // Retrieve and store the user's profile picture URL
                        var pictureClaim = loginResult.User.FindFirst(c => c.Type == "picture")?.Value;

                        if (!string.IsNullOrEmpty(pictureClaim))
                        {
                            await SecureStorage.SetAsync("user_profile_picture", pictureClaim);
                            // Validate the profile picture URL
                            var savedProfilePicture = await SecureStorage.GetAsync("user_profile_picture");
                            Console.WriteLine($"[DEBUG] Validating saved user profile picture URL from SecureStorage: {savedProfilePicture}");
                        }
                        else
                        {
                            Console.WriteLine("[DEBUG] User profile picture claim not found.");
                            await DisplayAlert("Error", "User profile picture not found. Please check Auth0 configuration.", "OK");
                        }
                    }

                    // On successful login, navigate to the chat room overview
                    Console.WriteLine("[DEBUG] Navigating to RoomPage after successful login.");
                    var roomPage = _serviceProvider.GetRequiredService<RoomPage>();

                    // Use the userId that was retrieved earlier
                    roomPage.SetUserId(userId); // Set the user ID in RoomPage
                    await Navigation.PushAsync(roomPage);
                }
                else
                {
                    Console.WriteLine($"[ERROR] Login error: {loginResult.Error}");
                    await DisplayAlert("Login Error", $"An error occurred during login: {loginResult.Error}", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle login failure
                Console.WriteLine($"[ERROR] An exception occurred during login: {ex.Message}");
                await DisplayAlert("Login Error", $"An error occurred during login: {ex.Message}", "OK");
            }
        }


        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                await _auth0Client.LogoutAsync();

                // Clear all stored tokens and user information using SecureStorage
                Console.WriteLine("[DEBUG] Attempting to clear SecureStorage data...");
                SecureStorage.Remove("access_token");
                Console.WriteLine("[DEBUG] Removed 'access_token' from SecureStorage.");

                SecureStorage.Remove("user_id");
                Console.WriteLine("[DEBUG] Removed 'user_id' from SecureStorage.");

                SecureStorage.Remove("user_name");
                Console.WriteLine("[DEBUG] Removed 'user_name' from SecureStorage.");

                SecureStorage.Remove("user_profile_picture");
                Console.WriteLine("[DEBUG] Removed 'user_profile_picture' from SecureStorage.");

                await DisplayAlert("Logged Out", "You have been logged out successfully.", "OK");
                Console.WriteLine("[DEBUG] Logout successful, all SecureStorage data cleared.");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Logout Error", $"An error occurred during logout: {ex.Message}", "OK");
                Console.WriteLine($"[ERROR] Failed during logout: {ex.Message}");
            }
        }



    }
}





