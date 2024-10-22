using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatAppRum.Model
{
    public class NotificationService
    {
        private readonly HttpClient _httpClient;

        public NotificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Store a new message flag for a specific room.
        public async Task StoreNewMessageFlag(string roomId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/notification/set_new_message_flag/{roomId}", null);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DEBUG] Successfully stored new message flag for room: {roomId}");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Failed to store new message flag for room: {roomId}. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception during flag storage: {ex.Message}");
            }
        }

        // Check if a room has a new message flag.
        public async Task<bool> CheckNewMessageFlag(string roomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/notification/check_new_message_flag/{roomId}");
                if (response.IsSuccessStatusCode)
                {
                    var flagStatus = await response.Content.ReadAsStringAsync();
                    bool hasNewMessage = bool.Parse(flagStatus);
                    Console.WriteLine($"[DEBUG] New message flag for room {roomId}: {hasNewMessage}");
                    return hasNewMessage;
                }
                else
                {
                    Console.WriteLine($"[DEBUG] No new message flag for room: {roomId}, Status code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception during flag check for room {roomId}: {ex.Message}");
                return false;
            }
        }

        // Clear the new message flag for a specific room.
        public async Task ClearNewMessageFlag(string roomId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/notification/clear_new_message_flag/{roomId}", null);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DEBUG] Successfully cleared new message flag for room: {roomId}");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Failed to clear new message flag for room: {roomId}, Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception during flag clear for room {roomId}: {ex.Message}");
            }
        }
    }

}
