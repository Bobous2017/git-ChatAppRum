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

namespace ChatAppRum
{
    public partial class MessagePage : ContentPage
    {
        private readonly Room _chatRoom;
        private readonly HttpClient _httpClient;
        public string RoomName => _chatRoom.Name; // Property to bind RoomName

        private readonly NotificationService _notificationService;
        public ObservableCollection<Message> Messages { get; set; }

        public MessagePage(Room selectedRoom, HttpClient httpClient)
        {
            InitializeComponent();
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

            // Initialize messages collection
            Messages = new ObservableCollection<Message>();
            MessagesCollection.ItemsSource = Messages;

            // Set BindingContext for Commands
            BindingContext = this;

            // Connect to load messages
            LoadMessages();
        }

        // Commands for update and delete operations
        public Command<Message> UpdateMessageCommand => new Command<Message>(async (message) => await OnUpdateMessage(message));
        public Command<Message> DeleteMessageCommand => new Command<Message>(async (message) => await OnDeleteMessage(message));
        public Command<Message> SendToAnotherRoomCommand => new Command<Message>(async (message) => await OnSendToAnotherRoom(message));

        // Method to load messages using HTTP API
        private async Task LoadMessages()
        {
            try
            {
                // HTTP GET request to fetch messages by room ID
                var response = await _httpClient.GetAsync($"api/message/room_get_message/{_chatRoom.Id}");
                if (response.IsSuccessStatusCode)
                {
                    var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
                    if (messages != null)
                    {
                        // Clear the existing messages and reload from API
                        Messages.Clear(); // Optionally clear existing messages
                        foreach (var message in messages)
                        {
                            // Check if the message belongs to the current room
                            //bool isSentFromCurrentRoom = message.IsFromCurrentRoom(_chatRoom.Id);
                            message.RoomName = _chatRoom.Name;
                            Messages.Add(message); // Add all messages from the current room
                        }
                    }
                }
                else
                {
                    await DisplayAlert("Error", $"Could not load messages: {response.ReasonPhrase}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not load messages: {ex.Message}", "OK");
            }
        }


        // Method to add a message using HTTP API
        private async void OnCreateMessage(object sender, EventArgs e)
        {
            var messageText = MessageInput.Text;

            Console.WriteLine("[DOTNET] OnCreateMessage invoked.");

            if (!string.IsNullOrEmpty(messageText))
            {
                Console.WriteLine($"[DOTNET] Message input is not empty. Proceeding with message: {messageText}");

                try
                {
                    var newMessage = new Message
                    {
                        SenderName = "Emilie", // This should ideally come from logged-in user context
                        Text = messageText,
                        Timestamp = DateTime.Now.ToString("g"),
                        RoomName = _chatRoom.Name, // Make sure RoomName is set to the current chat room
                        RoomId = _chatRoom.Id // Use RoomId instead of RoomName, assuming _chatRoom.Id is the MongoDB RoomId
                    };

                    // Convert message to JSON and send HTTP POST request
                    var response = await _httpClient.PostAsJsonAsync($"api/message/room_post_message", newMessage);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[DOTNET] Message successfully sent. Status code: {response.StatusCode}");
                        Messages.Add(newMessage);
                        MessageInput.Text = string.Empty;
                    }
                    else
                    {
                        await DisplayAlert("Error", $"Failed to create message. Server response: {response.ReasonPhrase}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to create message: {ex.Message}", "OK");
                }
            }
            else
            {
                Console.WriteLine("[DOTNET] Message input is empty. No action taken.");
            }
        }

        // Method to update a message using HTTP API
        private async Task OnUpdateMessage(Message message)
        {
            if (message == null)
            {
                Console.WriteLine("[DOTNET] Update requested but the message is null. Exiting.");
                return;
            }

            // Prompt user for new sender name
            string updatedSenderName = await DisplayPromptAsync("Update Sender", "Enter new sender name:", initialValue: message.SenderName);

            // Prompt user for new message text
            string updatedText = await DisplayPromptAsync("Update Message", "Enter new message text:", initialValue: message.Text);

            if (!string.IsNullOrEmpty(updatedText) && !string.IsNullOrEmpty(updatedSenderName))
            {
                Console.WriteLine($"[DOTNET] Attempting to update message with ID: {message.Id}");

                try
                {
                    // Update message in maui
                    message.SenderName = updatedSenderName;
                    message.Text = updatedText;
                    message.Timestamp = DateTime.Now.ToString("g");

                    // Send HTTP PUT request to update the message
                    var response = await _httpClient.PutAsJsonAsync($"api/message/room_update_message/{message.Id}", message);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[DOTNET] Message successfully updated on the server. Status code: {response.StatusCode}");
                        var index = Messages.IndexOf(message);
                        if (index >= 0)
                        {
                            Messages[index] = message;
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", $"Failed to update message. Server response: {response.ReasonPhrase}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to update message: {ex.Message}", "OK");
                }
            }
        }

        // Method to delete a message using HTTP API
        private async Task OnDeleteMessage(Message message)
        {
            if (message == null)
            {
                Console.WriteLine("[DOTNET] Delete requested but the message is null. Exiting.");
                return;
            }

            bool confirmDelete = await DisplayAlert("Delete Message", "Are you sure you want to delete this message?", "Yes", "No");

            if (confirmDelete)
            {
                try
                {
                    // Send HTTP DELETE request to delete the message
                    var response = await _httpClient.DeleteAsync($"api/message/room_delete_message/{message.Id}");
                    if (response.IsSuccessStatusCode)
                    {   
                        Messages.Remove(message);
                        Console.WriteLine("[DOTNET] Message removed from local collection.");
                    }
                    else
                    {
                        await DisplayAlert("Error", $"Failed to delete message. Server response: {response.ReasonPhrase}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete message: {ex.Message}", "OK");
                }
            }
        }

        // Method to send a message to another room
        private async Task OnSendToAnotherRoom(Message message)
        {
            if (message == null)
            {
                Debug.WriteLine("Message object is null. Exiting method.");
                return;
            }

            Debug.WriteLine($"Message to send: {message.Text}");

            try
            {
                Debug.WriteLine($"Attempting to get rooms");

                // Fetch the rooms
                var response = await _httpClient.GetAsync($"api/Room/room_get");
                var responseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API response: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Failed to get rooms. Status Code: {response.StatusCode}");
                    await DisplayAlert("Error", "Failed to retrieve rooms.", "OK");
                    return;
                }

                // Deserialize the room data
                var rooms = await response.Content.ReadFromJsonAsync<List<Room>>();
                if (rooms == null || rooms.Count == 0)
                {
                    Debug.WriteLine("Rooms list is empty or null.");
                    await DisplayAlert("No Rooms", "No rooms available to send the message.", "OK");
                    return;
                }

                Debug.WriteLine($"Rooms retrieved: {rooms.Count}");

                // Extract the room names for selection
                var roomNames = rooms.Select(r => r.Name).ToArray();

                Debug.WriteLine($"Room names for selection: {string.Join(", ", roomNames)}");

                // Show the action sheet for room selection
                string selectedRoomName = await Application.Current.MainPage.DisplayActionSheet(
                    "Select Room to Send Message",
                    "Cancel",
                    null,
                    roomNames);

                Debug.WriteLine($"Selected room: {selectedRoomName}");

                if (string.IsNullOrEmpty(selectedRoomName) || selectedRoomName == "Cancel")
                {
                    Debug.WriteLine("No room selected or user canceled.");
                    return;
                }

                // Find the selected room object
                var selectedRoom = rooms.FirstOrDefault(r => r.Name == selectedRoomName);
                if (selectedRoom == null)
                {
                    Debug.WriteLine("Selected room not found.");
                    await DisplayAlert("Error", "Selected room not found.", "OK");
                    return;
                }

                // Prepare the new message to be sent to the selected room
                var newMessage = new Message
                {
                    SenderName = message.SenderName,
                    Text = message.Text,
                    Timestamp = DateTime.Now.ToString("g"),
                    RoomName = selectedRoomName,  // Use the selected room name
                    RoomId = selectedRoom.Id,      // Use the selected room's ID
                    FromRoomName = _chatRoom.Name  // This is the room where the message was sent from
                };

                Debug.WriteLine($"Sending message to room: {selectedRoomName}, Room ID: {selectedRoom.Id}");

                // Send the message
                var postResponse = await _httpClient.PostAsJsonAsync("api/message/room_post_message", newMessage);
                if (postResponse.IsSuccessStatusCode)
                {
                    // Store the new message flag for the receiving room
                    // This could be done via backend or any shared state
                    //await StoreNewMessageFlag(selectedRoom.Id); // A function to store the flag

                    Debug.WriteLine("Message successfully sent.");
                    await Application.Current.MainPage.DisplayAlert("Message Sent", $"Message has been sent to {selectedRoomName}", "OK");


                    /// Show a toast for the sent message
                    Toast.Make($"Message sent to {selectedRoomName}", CommunityToolkit.Maui.Core.ToastDuration.Long, 30).Show();

                    // Now store the new message flag for the other room
                    await _notificationService.StoreNewMessageFlag(selectedRoom.Id);
                }
                else
                {
                    Debug.WriteLine($"Failed to send message. Status Code: {postResponse.StatusCode}");
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to send the message.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception caught: {ex.Message}");
                await DisplayAlert("Error", $"Failed to send message to another room: {ex.Message}", "OK");
            }
        }

        //-----------------------------------------------------------------------Notification send message from room to room---------------
        // Method triggered when the page is loaded or navigated to.
        // It checks for new messages and displays a toast notification if applicable.
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Check if there is a new message flag for this room
            var hasNewMessage = await _notificationService.CheckNewMessageFlag(_chatRoom.Id);

            if (hasNewMessage)
            {
                // Fetch the latest message to get the 'FromRoomName' (which is the sending room)
                var latestMessage = Messages.LastOrDefault(); // Assuming Messages are already loaded
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
            await LoadMessages();
        }

        //// Store a new message flag for a specific room.
        //// This method sets a flag to indicate that a new message has been sent to a room.
        //private async Task StoreNewMessageFlag(string roomId)
        //{
        //    try
        //    {
        //        // Store the flag using an API call
        //        var response = await _httpClient.PostAsync($"api/notification/set_new_message_flag/{roomId}", null);

        //        // Debugging log
        //        if (response.IsSuccessStatusCode)
        //        {
        //            Console.WriteLine($"[DEBUG] Successfully stored new message flag for room: {roomId}");
        //        }
        //        else
        //        {
        //            Console.WriteLine($"[ERROR] Failed to store new message flag for room: {roomId}. Status code: {response.StatusCode}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[ERROR] Exception during flag storage: {ex.Message}");
        //    }
        //}

        //// Check if a room has a new message flag.
        //// This method verifies if there are new messages for a given room by checking the stored flag.
        //private async Task<bool> CheckNewMessageFlag(string roomId)
        //{
        //    try
        //    {
        //        // Call the API to check the new message flag for the room
        //        var response = await _httpClient.GetAsync($"api/notification/check_new_message_flag/{roomId}");

        //        if (response.IsSuccessStatusCode)
        //        {
        //            // Parse the response and check if the flag is set
        //            var flagStatus = await response.Content.ReadAsStringAsync();
        //            bool hasNewMessage = bool.Parse(flagStatus);

        //            Console.WriteLine($"[DEBUG] New message flag for room {roomId}: {hasNewMessage}");

        //            // Return true only if the flag is explicitly set to true
        //            return hasNewMessage;
        //        }
        //        else
        //        {
        //            Console.WriteLine($"[DEBUG] No new message flag for room: {roomId}, Status code: {response.StatusCode}");
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log any errors during the flag check
        //        Console.WriteLine($"[ERROR] Exception during flag check for room {roomId}: {ex.Message}");
        //        return false;
        //    }
        //}

        //// Clear the new message flag for a specific room.
        //// After the user sees the notification for new messages, this method clears the flag to prevent duplicate notifications.
        //private async Task ClearNewMessageFlag(string roomId)
        //{
        //    try
        //    {
        //        // Clear the flag by calling the API
        //        var response = await _httpClient.PostAsync($"api/notification/clear_new_message_flag/{roomId}", null);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            Console.WriteLine($"[DEBUG] Successfully cleared new message flag for room: {roomId}");
        //        }
        //        else
        //        {
        //            Console.WriteLine($"[ERROR] Failed to clear new message flag for room: {roomId}, Status code: {response.StatusCode}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle errors during the flag clear
        //        Console.WriteLine($"[ERROR] Exception during flag clear for room {roomId}: {ex.Message}");
        //    }
        //}

        private void OnMessageInputFocused(object sender, FocusEventArgs e)
        {
            // Your logic here
        }
    }
}
