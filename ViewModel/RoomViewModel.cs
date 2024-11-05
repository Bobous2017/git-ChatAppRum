using ChatAppRum.Model;
using ChatRumLibrary;
using FunWithFlags_Library;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatAppRum.ViewModel
{
    public class RoomViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        private string _userProfilePicture;
        private string _userId; // Field to store the user ID
        private bool _isRefreshing;

        public ObservableCollection<Room> Rooms { get; set; }
        public ICommand RefreshCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand UpdateRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }

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

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    OnPropertyChanged(nameof(IsRefreshing));
                }
            }
        }

        public RoomViewModel(HttpClient httpClient, string userId)
        {
            _httpClient = httpClient;
            _userId = userId; // Set the user ID
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

            // Commands for update and delete operations
            Rooms = new ObservableCollection<Room>();
            RefreshCommand = new Command(async () => await OnRefresh());
            CreateRoomCommand = new Command(async () => await OnCreateRoomAsync());
            UpdateRoomCommand = new Command<Room>(OnUpdateRoom);
            DeleteRoomCommand = new Command<Room>(OnDeleteRoom);

            // Start the async initialization after the constructor
            InitializeUserProfilePicture();

            LoadRooms(); // Load all rooms in everytime is going to Room Page
        }
        // Optional method to set userId after constructor
        public void SetUserIdAndLoadRooms(string userId)
        {
            _userId = userId;
            LoadRooms(); // Load rooms for this user
        }

        // Method to load chat rooms from the HTTP API
        private async void LoadRooms()
        {
            try
            {
                // Check if userId is set, otherwise fetch it from SecureStorage
                if (string.IsNullOrEmpty(_userId))
                {
                    Console.WriteLine("[DEBUG] User ID is not set. Attempting to retrieve it from SecureStorage.");
                    _userId = await SecureStorage.GetAsync("user_id");

                    if (string.IsNullOrEmpty(_userId))
                    {
                        Console.WriteLine("[ERROR] User ID is not available. Please login.");
                        return; // Cannot proceed without a valid user ID
                    }
                }

                Console.WriteLine("[DEBUG] Attempting to fetch rooms from API...");

                // Update request URL to include userId as a query parameter
                var requestUrl = $"api/Room/room_get?userId={_userId}";
                Console.WriteLine($"[DEBUG] Request URL: {_httpClient.BaseAddress}{requestUrl}");

                var response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("[DEBUG] Successfully received response from API.");
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DEBUG] Response Content: {responseContent}");

                    var rooms = await response.Content.ReadFromJsonAsync<List<Room>>();
                    if (rooms != null && rooms.Any())
                    {
                        Console.WriteLine($"[DEBUG] Number of rooms fetched: {rooms.Count}");
                        Rooms.Clear();
                        foreach (var room in rooms.OrderByDescending(r => r.CreatedAt))
                        {
                            // Validate if ProfileImageRoom path is still valid
                            if (!string.IsNullOrEmpty(room.ProfileImageRoom) && !File.Exists(room.ProfileImageRoom))
                            {
                                Console.WriteLine($"[WARNING] Profile image for room '{room.Name}' is missing. Path: {room.ProfileImageRoom}");
                                room.ProfileImageRoom = null; // Remove the invalid path or handle it gracefully
                            }
                            Rooms.Add(room);
                            Console.WriteLine($"[DEBUG] Loaded Room: {room.Name}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG] No rooms found or parsing issue occurred.");
                    }
                }
                else
                {
                    Console.WriteLine($"[ERROR] Failed to load rooms. Server response: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Could not load chat rooms: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false; // Mark refresh as complete
            }
        }
        // Refresh method to reload chat rooms
        private async Task OnRefresh()
        {
            IsRefreshing = true; // Start refresh indicator
            // Reload chat rooms when manually refreshed
            LoadRooms();
        }

        // Method to create a new chat room using HTTP API
        private async Task OnCreateRoomAsync()
        {
            string roomName = await Application.Current.MainPage.DisplayPromptAsync("Create Room", "Enter room name:");
            string roomDescription = await Application.Current.MainPage.DisplayPromptAsync("Create Room", "Enter room description:");

            if (!string.IsNullOrEmpty(roomName) && !string.IsNullOrEmpty(roomDescription))
            {
                var newRoom = new Room { Name = roomName, Description = roomDescription, CreatedAt = DateTime.Now };

                try
                {
                    // Get userId from SecureStorage
                    var userId = await SecureStorage.GetAsync("user_id");

                    if (string.IsNullOrEmpty(userId))
                    {
                        Console.WriteLine("[ERROR] User ID not available. Please login.");
                        return;
                    }

                    // Set userId in the headers
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("UserId", userId);

                    // Send HTTP POST request to create a new room
                    var response = await _httpClient.PostAsJsonAsync("api/Room/room_post", newRoom);
                    if (response.IsSuccessStatusCode)
                    {
                        // Update the user's RoomIds list with the newly created room ID
                        var createdRoom = await response.Content.ReadFromJsonAsync<Room>(); // Assuming the response returns the saved room, including the generated Id.
                        if (createdRoom != null)
                        {
                            await UpdateUserRoomListAsync(userId, createdRoom.Id);

                            // Insert the new room into the Rooms collection, but ensure it's done on the main thread
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                Rooms.Insert(0, createdRoom);
                            });

                            Console.WriteLine("[DOTNET] Room created successfully.");
                        }
                        else
                        {
                            Console.WriteLine("[ERROR] Failed to deserialize the created room from the server.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to create room. Server response: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to create room: {ex.Message}");
                }
            }
        }

        // Helper method to update the user's room list in MongoDB
        private async Task UpdateUserRoomListAsync(string userId, string roomId)
        {
            try
            {
                // Prepare request data
                var updateData = new { RoomId = roomId };

                // Send HTTP PATCH request to update the user's RoomIds list
                var response = await _httpClient.PatchAsync($"api/User/{userId}/addRoom", JsonContent.Create(updateData));

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("[DOTNET] User's room list updated successfully.");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Failed to update user's room list. Server response: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update user's room list: {ex.Message}");
            }
        }
        // Method to update a room using HTTP API
        private async void OnUpdateRoom(Room room)
        {
            if (room == null)
                return;

            string updatedRoomName = await Application.Current.MainPage.DisplayPromptAsync("Update Room", "Enter new room name:", initialValue: room.Name);
            string updatedRoomDescription = await Application.Current.MainPage.DisplayPromptAsync("Update Room", "Enter new room description:", initialValue: room.Description);

            if (!string.IsNullOrEmpty(updatedRoomName) && !string.IsNullOrEmpty(updatedRoomDescription))
            {
                room.Name = updatedRoomName;
                room.Description = updatedRoomDescription;

                try
                {
                    // Get userId from SecureStorage
                    var userId = await SecureStorage.GetAsync("user_id");

                    if (string.IsNullOrEmpty(userId))
                    {
                        Console.WriteLine("[ERROR] User ID not available. Please login.");
                        return;
                    }

                    // Set userId in the headers
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("UserId", userId);

                    // Send HTTP PUT request to update the room
                    var response = await _httpClient.PutAsJsonAsync($"api/Room/room_update/{room.Id}", room);
                    if (response.IsSuccessStatusCode)
                    {
                        // Reload rooms to update the UI
                        LoadRooms();
                        Console.WriteLine("[DOTNET] Room updated successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to update room. Server response: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to update room: {ex.Message}");
                }
            }
        }

        // Method to delete a room using HTTP API
        private async void OnDeleteRoom(Room room)
        {
            if (room == null)
                return;

            bool confirmDelete = await Application.Current.MainPage.DisplayAlert("Delete Room", $"Are you sure you want to delete room: {room.Name}?", "Yes", "No");

            if (confirmDelete)
            {
                try
                {
                    // Get userId from SecureStorage
                    var userId = await SecureStorage.GetAsync("user_id");

                    if (string.IsNullOrEmpty(userId))
                    {
                        Console.WriteLine("[ERROR] User ID not available. Please login.");
                        return;
                    }

                    // Set userId in the headers to authenticate the delete request
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("UserId", userId);

                    // Send HTTP DELETE request to delete the room
                    var response = await _httpClient.DeleteAsync($"api/Room/room_delete/{room.Id}");
                    if (response.IsSuccessStatusCode)
                    {
                        Rooms.Remove(room);
                        Console.WriteLine("[DOTNET] Room deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Failed to delete room. Server response: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to delete room: {ex.Message}");
                }
            }
        }

        // Method to User Profil from login
        private async void InitializeUserProfilePicture()
        {
            // Fetching user profile picture from SecureStorage asynchronously
            var userProfile = await SecureStorage.GetAsync("user_profile_picture");
            UserProfilePicture = userProfile ?? "default_avatar.png"; // Set a default avatar if the picture isn't found
        }

        // Method to Upadate room profil picture using HTTP API
        public ICommand ChangeProfilePictureCommand => new Command<Room>(async (room) =>
        {
            if (room == null) return;

            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Pick a profile picture"
            });

            if (result != null)
            {
                // Update the profile image in the room object
                //room.ProfileImageRoom = result.FullPath;



                // Define a permanent storage directory (e.g., Documents folder)
                var targetDirectory = FileSystem.AppDataDirectory; // This is a persistent app-specific directory
                var targetFilePath = Path.Combine(targetDirectory, Path.GetFileName(result.FullPath));

                // Copy the file to the persistent storage
                File.Copy(result.FullPath, targetFilePath, true);

                // Update the profile image in the room object
                room.ProfileImageRoom = targetFilePath; // Use the persistent path instead of the temporary one

                // Re-assign the updated room to ensure UI refresh
                var index = Rooms.IndexOf(room);
                if (index >= 0)
                {
                    Rooms[index] = null;  // Set to null to temporarily break the binding
                    Rooms[index] = room;   // Assign the updated object to trigger UI refresh
                }

                // Get userId from SecureStorage
                var userId = await SecureStorage.GetAsync("user_id");

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("[ERROR] User ID not available. Please login.");
                    return;
                }

                // Persist the changes in the database
                try
                {
                    // Set userId in the headers to authenticate the delete request
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("UserId", userId);
                    var response = await _httpClient.PutAsJsonAsync($"api/Room/room_update/{room.Id}", room);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[ERROR] Failed to update room profile image. Server response: {response.ReasonPhrase}");
                    }
                    else
                    {
                        // Update the room in the Rooms collection manually
                        var updatedRoom = Rooms.FirstOrDefault(r => r.Id == room.Id);
                        if (updatedRoom != null)
                        {
                            updatedRoom.ProfileImageRoom = room.ProfileImageRoom;
                            updatedRoom.Name = room.Name;
                            updatedRoom.Description = room.Description;
                            // Manually trigger a UI refresh
                            OnPropertyChanged(nameof(Rooms));
                        }
                        Console.WriteLine("[DOTNET] Room profile picture updated successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to update room profile picture: {ex.Message}");
                }
            }
        });

        //  Allowing us to make change immediately
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
