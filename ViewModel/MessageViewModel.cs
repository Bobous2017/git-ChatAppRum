using ChatAppRum.Model;
using ChatRumLibrary;
using FunWithFlags_Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;

namespace ChatAppRum.ViewModel
{
    public class MessageViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private  Room _chatRoom;

        private readonly NotificationService _notificationService;
        private string _roomId;
        private string _userProfilePicture;
        private string _roomProfil;
        private string _roomName;
        

        private string _messageText;
        public string MessageText
        {
            get => _messageText;
            set
            {
                if (_messageText != value)
                {
                    _messageText = value;
                    OnPropertyChanged(nameof(MessageText));
                }
            }
        }


        private readonly ContentPage _page; // DisplayAlert, DisplayPromptAsync, and MessageInput do not exist in the current context.

        // Delegate for DisplayPromptAsync
        public Func<string, string, string, Task<string>> DisplayPromptMessage { get; set; } // OnCreateMessage

        // Initialize the commands
     
        public Command CreateMessageCommand { get; }
        


        public string UserProfilePicture
        {
            get => _userProfilePicture;
            set
            {
                if (_userProfilePicture != value)
                {
                    _userProfilePicture = value;
                    OnPropertyChanged(nameof(UserProfilePicture));
                }
            }
        }

        public string RoomProfil
        {
            get => _roomProfil;
            set
            {
                if (_roomProfil != value)
                {
                    _roomProfil = value;
                    OnPropertyChanged(nameof(RoomProfil));
                }
            }
        }

        public string RoomName
        {
            get => _roomName;
            set
            {
                if (_roomName != value)
                {
                    _roomName = value;
                    OnPropertyChanged(nameof(RoomName));
                }
            }
        }

        public string RoomId
        {
            get => _roomId;
            set
            {
                if (_roomId != value)
                {
                    _roomId = value;
                    OnPropertyChanged(nameof(_roomId)); 
                }
            }
        }

        public ObservableCollection<Message> Messages { get; set; }

        // Constructor
        public MessageViewModel(Room selectedRoom, HttpClient httpClient)
        {
            if (selectedRoom == null)
            {
                throw new ArgumentNullException(nameof(selectedRoom), "Selected room cannot be null.");
            }
            _chatRoom = selectedRoom;
            RoomId = _chatRoom.Id;  // Assigning RoomId
            //RoomId = selectedRoom.Id; // Get Room Id and pass it to room local Room ID
            RoomName = selectedRoom.Name;
            //RoomProfil = selectedRoom.ProfileImageRoom;
            // Assign RoomProfile with null check and fallback
            RoomProfil = !string.IsNullOrEmpty(selectedRoom.ProfileImageRoom) ? selectedRoom.ProfileImageRoom : "default_room_avatar.png";
            Console.WriteLine($"[DEBUG] Room profile picture URL: {RoomProfil}");

            _httpClient = httpClient;
            //_page = page; // Store the reference to the ContentPage

            //Create the HttpClientHandler and bypass SSL certificate validation for development purposes

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // Initialize HttpClient with the custom handler
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri($"http://{NetworkUtils.GlobalIPAddress()}:5000/")  // Using HTTP here
            };
            Messages = new ObservableCollection<Message>();
            //MessagesCollection.ItemsSource = Messages;
            // Initialize commands
            CreateMessageCommand = new Command(OnCreateMessage);



            // Notify about the initial property values
            OnPropertyChanged(nameof(RoomName));
            OnPropertyChanged(nameof(RoomProfil));
            OnPropertyChanged(nameof(RoomId));
            OnPropertyChanged(nameof(Messages)); // Notify the UI about the change.

            _notificationService = new NotificationService(_httpClient);

            // Start the async initialization after the constructor
            InitializeUserProfilePicture();
            // Connect to load messages
            LoadMessages();
        }

        // Commands for update and delete operations
        public Command<Message> UpdateMessageCommand => new Command<Message>(async (message) => await OnUpdateMessage(message));
        public Command<Message> DeleteMessageCommand => new Command<Message>(async (message) => await OnDeleteMessage(message));
        public Command<Message> SendToAnotherRoomCommand => new Command<Message>(async (message) => await OnSendToAnotherRoom(message));
       
        // Method to load messages using HTTP API
        public async Task LoadMessages()
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
                    await Application.Current.MainPage.DisplayAlert("Error", $"Could not load messages: {response.ReasonPhrase}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Could not load messages: {ex.Message}", "OK");
            }
        }

        private async void OnCreateMessage()
        {

            var messageText = MessageText;

            if (string.IsNullOrEmpty(messageText))
            {
                Console.WriteLine("[DOTNET] Message input is empty. No action taken.");
                return;
            }

            Console.WriteLine("[DOTNET] OnCreateMessage invoked.");

            // var messageText = await Application.Current.MainPage.DisplayPromptAsync("Create Room", "Enter room name:");


            if (!string.IsNullOrEmpty(messageText))
            {
                Console.WriteLine($"[DOTNET] Message input is not empty. Proceeding with message: {messageText}");

                try
                {
                    // Retrieve the user email from SecureStorage
                    //string userEmail = await SecureStorage.GetAsync("user_email");
                    string userName = await SecureStorage.GetAsync("user_name");
                    if (string.IsNullOrEmpty(userName))
                    {
                        Console.WriteLine("[DEBUG] User email not found in SecureStorage.");
                        //await _page.DisplayAlert("Error", "Unable to retrieve user email. Please log in again.", "OK");

                        return;
                    }
                    // Retrieve the user's profile picture URL from SecureStorage
                    string userProfilePictureUrl = await SecureStorage.GetAsync("user_profile_picture");

                    if (string.IsNullOrEmpty(userProfilePictureUrl))
                    {
                        Console.WriteLine("[DEBUG] User profile picture not found in SecureStorage.");
                        userProfilePictureUrl = "default_avatar.png"; // Fallback to a default profile picture if none is found
                    }

                    Console.WriteLine($"[DEBUG] Retrieved user name from SecureStorage: {userName}");
                    Console.WriteLine($"[DEBUG] Retrieved profile picture URL from SecureStorage: {userProfilePictureUrl}");

                    var newMessage = new Message
                    {
                        //SenderName = "Emilie", // Hardcoded
                        SenderName = userName,  // Use the usern from SecureStorage
                        UserProfilePicture = userProfilePictureUrl,
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
                        //Messages.Add(newMessage);

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Messages.Add(newMessage);
                        });

                    }
                    else
                    {
                        //await _page.DisplayAlert("Error", $"Failed to create message. Server response: {response.ReasonPhrase}", "OK");
                        await Application.Current.MainPage.DisplayAlert("Error", $"Failed to create message. Server response: {response.ReasonPhrase}", "OK");

                    }
                }
                catch (Exception ex)
                {
                    //await _page.DisplayAlert("Error", $"Failed to create message: {ex.Message}", "OK");
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to create message: {ex.Message}", "OK");
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
            string updatedSenderName = await Application.Current.MainPage.DisplayPromptAsync("Update Sender", "Enter new sender name:", initialValue: message.SenderName);

            // Prompt user for new message text
            string updatedText = await Application.Current.MainPage.DisplayPromptAsync("Update Message", "Enter new message text:", initialValue: message.Text);

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
                        await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update message. Server response: {response.ReasonPhrase}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update message: {ex.Message}", "OK");
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

            bool confirmDelete = await Application.Current.MainPage.DisplayAlert("Delete Message", "Are you sure you want to delete this message?", "Yes", "No");

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
                        await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete message. Server response: {response.ReasonPhrase}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete message: {ex.Message}", "OK");
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
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to retrieve rooms.", "OK");
                    return;
                }

                // Deserialize the room data
                var rooms = await response.Content.ReadFromJsonAsync<List<Room>>();
                if (rooms == null || rooms.Count == 0)
                {
                    Debug.WriteLine("Rooms list is empty or null.");
                    await Application.Current.MainPage.DisplayAlert("No Rooms", "No rooms available to send the message.", "OK");
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
                    await Application.Current.MainPage.DisplayAlert("Error", "Selected room not found.", "OK");
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
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to send message to another room: {ex.Message}", "OK");
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {

                Console.WriteLine($"[DOTNET] OnPropertyChanged called for: {propertyName}");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private async void InitializeUserProfilePicture()
        {
            // Fetching user profile picture from SecureStorage asynchronously
            var userProfile = await SecureStorage.GetAsync("user_profile_picture");
            UserProfilePicture = userProfile ?? "default_avatar.png"; // Set a default avatar if the picture isn't found
        }
       

    }

}
