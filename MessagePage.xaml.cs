using ChatAppRum.Model;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using ChatRumLibrary;
using FunWithFlags_Library;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using ChatAppRum.ViewModel;


namespace ChatAppRum
{
    public partial class MessagePage : ContentPage
    {
        private readonly Room _chatRoom;
        private readonly MessageViewModel _viewModel; // Store reference to the view model
        private readonly HttpClient _httpClient;
        public string RoomName => _chatRoom.Name; // Property to bind RoomName
        public string RoomProfil => _chatRoom.ProfileImageRoom; // Property to bind RoomProfil


        private readonly NotificationService _notificationService;
        public ObservableCollection<Message> Messages { get; set; }

        public MessagePage(Room selectedRoom, HttpClient httpClient)
        {
            InitializeComponent();
            if (selectedRoom == null)
            {
                throw new ArgumentNullException(nameof(selectedRoom), "Selected room cannot be null.");
            }

            _chatRoom = selectedRoom;
            _httpClient = httpClient;

           
            // Create the HttpClientHandler and bypass SSL certificate validation for development purposes
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // Initialize HttpClient with the custom handler
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri($"http://{NetworkUtils.GlobalIPAddress()}:5000/")  // Using HTTP here
            };
            // Initialize the NotificationService with the HttpClient
            _notificationService = new NotificationService(_httpClient);

            // Create the view model and set it as the BindingContext
            _viewModel = new MessageViewModel(_chatRoom, _httpClient);
            BindingContext = _viewModel; // Use the ViewModel as the BindingContext for data binding

        }

        
        //-----------------------------------------------------------------------Notification send message from room to room---------------
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Check if there is a new message flag for this room
            var hasNewMessage = await _notificationService.CheckNewMessageFlag(_chatRoom.Id);

            if (hasNewMessage)
            {
                // Fetch the latest message to get the 'FromRoomName' (which is the sending room)
                var latestMessage = _viewModel.Messages.LastOrDefault(); //  _viewModel.Messages instead
                if (latestMessage != null)
                {
                    // Show toast with both the receiving room and the sending room names
                    var toastMessage = $"You got a new message in your: {_chatRoom.Name} from {latestMessage.FromRoomName}";
                    Toast.Make(toastMessage, CommunityToolkit.Maui.Core.ToastDuration.Long, 30).Show();
                }

                // Clear the flag once the toast is shown to avoid repeated notifications
                await _notificationService.ClearNewMessageFlag(_chatRoom.Id);
            }

            // Load messages for the current room
            await _viewModel.LoadMessages(); // Load the messages using the view model
        }

        
        private void OnMessageInputFocused(object sender, FocusEventArgs e)
        {
            // Your logic here
        }
    }
}
