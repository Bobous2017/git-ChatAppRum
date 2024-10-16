using ChatAppRum.Model;

namespace ChatAppRum;

public partial class ChatRoomPage : ContentPage
{
    private ChatRoom _chatRoom;

    // Constructor that accepts a ChatRoom object
    public ChatRoomPage(ChatRoom selectedRoom)
    {
        InitializeComponent();
        _chatRoom = selectedRoom;

        // Now you can use _chatRoom to display information or load messages
        Title = _chatRoom.Name; // Set the page title to the chat room's name
    }
}