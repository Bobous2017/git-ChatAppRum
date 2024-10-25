using ChatAppRum.Model;
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
        private bool _isRefreshing;

        public ObservableCollection<Room> Rooms { get; set; }
        public ICommand RefreshCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand UpdateRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }

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

        public RoomViewModel(HttpClient httpClient)
        {
            _httpClient = httpClient;

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

            Rooms = new ObservableCollection<Room>();
            RefreshCommand = new Command(async () => await OnRefresh());
            CreateRoomCommand = new Command(OnCreateRoom);
            UpdateRoomCommand = new Command<Room>(OnUpdateRoom);
            DeleteRoomCommand = new Command<Room>(OnDeleteRoom);

            LoadRooms(); // Load all rooms in everytime is going to Room Page
        }

        // Method to load chat rooms from the HTTP API
        private async void LoadRooms()
        {
            try
            {
                Console.WriteLine("[DEBUG] Attempting to fetch rooms from API...");
                Console.WriteLine($"[DEBUG] Request URL: {_httpClient.BaseAddress}api/Room/room_get");
                var response = await _httpClient.GetAsync("api/Room/room_get");


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
        private async void OnCreateRoom()
        {
            string roomName = await Application.Current.MainPage.DisplayPromptAsync("Create Room", "Enter room name:");
            string roomDescription = await Application.Current.MainPage.DisplayPromptAsync("Create Room", "Enter room description:");

            if (!string.IsNullOrEmpty(roomName) && !string.IsNullOrEmpty(roomDescription))
            {
                var newRoom = new Room { Name = roomName, Description = roomDescription, CreatedAt = DateTime.Now };

                try
                {
                    // Send HTTP POST request to create a new room
                    var response = await _httpClient.PostAsJsonAsync("api/Room/room_post", newRoom);
                    if (response.IsSuccessStatusCode)
                    {
                        Rooms.Insert(0, newRoom); // The new room will immediately appear at the top of the list without requiring a refresh.
                        //Rooms.Add(newRoom);     // Appends to the end of the list, which is why you were seeing it at the bottom initially.
                        Console.WriteLine("[DOTNET] Room created successfully.");
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
                    // Send HTTP PUT request to update the room
                    var response = await _httpClient.PutAsJsonAsync($"api/Room/room_update/{room.Id}", room);
                    if (response.IsSuccessStatusCode)
                    {
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
                room.ProfileImageRoom = result.FullPath;

                // Re-assign the updated room to ensure UI refresh
                var index = Rooms.IndexOf(room);
                if (index >= 0)
                {
                    Rooms[index] = null;  // Set to null to temporarily break the binding
                    Rooms[index] = room;   // Assign the updated object to trigger UI refresh
                }

                // Persist the changes in the database
                try
                {
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


    }
}
