using ChatRumLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatAppRum.Model
{
    public class ChatRoomService
    {
        public Dictionary<string, ObservableCollection<Message>> MessagesPerRoom { get; set; }

        public ChatRoomService()
        {
            MessagesPerRoom = new Dictionary<string, ObservableCollection<Message>>();
        }

        public ObservableCollection<Message> GetMessages(string roomName)
        {
            if (!MessagesPerRoom.ContainsKey(roomName))
            {
                MessagesPerRoom[roomName] = new ObservableCollection<Message>();
            }

            return MessagesPerRoom[roomName];
        }
    }

}
