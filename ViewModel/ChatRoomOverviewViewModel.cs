using ChatAppRum.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatAppRum.ViewModel
{
    public class ChatRoomOverviewViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ChatRoom> ChatRooms { get; set; }
        public ICommand RefreshCommand { get; }

        public ChatRoomOverviewViewModel()
        {
            ChatRooms = new ObservableCollection<ChatRoom>();
            RefreshCommand = new Command(OnRefresh);
            LoadChatRooms();
        }

        private void LoadChatRooms()
        {
            //Load chat rooms sorted by the latest message time
           var rooms = GetChatRooms().OrderByDescending(r => r.LatestMessageTime).ToList();
            ChatRooms.Clear();
            foreach (var room in rooms)
            {
                ChatRooms.Add(room);
            }



        }

        private void OnRefresh()
        {
            // Reload chat rooms when manually refreshed
            LoadChatRooms();
        }

        private IEnumerable<ChatRoom> GetChatRooms()
        {
            // Fetch chat rooms (replace this with your real data source)
            return new List<ChatRoom>
        {
            new ChatRoom { Name = "Room 1", Description = "Description of Room 1", LatestMessageTime = DateTime.Now.AddMinutes(-5) },
            new ChatRoom { Name = "Room 2", Description = "Description of Room 2", LatestMessageTime = DateTime.Now.AddMinutes(-1) },
            new ChatRoom { Name = "Room 3", Description = "Description of Room 3", LatestMessageTime = DateTime.Now.AddMinutes(-2) }
        };
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

 

}
