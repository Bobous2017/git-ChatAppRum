namespace ChatAppRum
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} Time";
            else
                CounterBtn.Text = $"Clicked {count} Times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
