using ChatAppRum.Model;
using ChatAppRum.ViewModel;

namespace ChatAppRum;

public partial class ChatRoomOverviewPage : ContentPage
{
    public ChatRoomOverviewPage()
    {
        InitializeComponent();
        BindingContext = new ChatRoomOverviewViewModel();
    }

    // This method handles the room selection event
    private void OnRoomSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ChatRoom selectedRoom)
        {
            // Navigate to the chat room page when a room is selected
            Navigation.PushAsync(new ChatRoomPage(selectedRoom));
        }
    }
}