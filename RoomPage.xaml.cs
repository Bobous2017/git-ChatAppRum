using ChatAppRum.Model;
using ChatAppRum.ViewModel;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls;
using System.Net.Http;

namespace ChatAppRum
{
    public partial class RoomPage : ContentPage
    {
        //private readonly HubConnection _hubConnection;
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient _httpClient;

        // Update constructor to accept IHttpClientFactory from Dependency Injection
        public RoomPage(/*HubConnection hubConnection,*/ IHttpClientFactory httpClientFactory)
        {
            InitializeComponent();

            // Assign the IHttpClientFactory from the DI container
            _httpClientFactory = httpClientFactory;

            // Use the factory to create an HttpClient instance
            _httpClient = _httpClientFactory.CreateClient("ChatAPI");

            // Pass HttpClient to ViewModel for dynamic room loading
            BindingContext = new RoomViewModel(_httpClient);
        }

        // Add the OnRoomTapped event handler: it navigates to ChatRoomPage, where it shows all messages for the room you enter
        private async void OnRoomTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is Room selectedRoom)
            {
                var grid = sender as Grid;
                if (grid != null)
                {
                    // Change background color to indicate tap feedback
                    grid.BackgroundColor = Colors.LightGray;

                    // Delay to let the user see the feedback
                    await Task.Delay(100);

                    // Revert the background color
                    grid.BackgroundColor = Colors.Transparent;
                }
                await Navigation.PushAsync(new MessagePage(selectedRoom, /*_hubConnection,*/ _httpClient));
            }
        }
    }
}
