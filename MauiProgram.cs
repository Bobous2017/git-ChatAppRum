using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;  // Add this to use SignalR client
using Microsoft.Extensions.DependencyInjection;
using ChatAppRum.Model;
using ChatRumDataAccess;
using FunWithFlags_Library;
using ChatAppRum.ViewModel;
using CommunityToolkit.Maui;
namespace ChatAppRum
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();


            builder.Services.AddSingleton<MongoDataAccess>(); // Assuming MongoDataAccess is used as a singleton

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Register SignalR HubConnection as a singleton
            //builder.Services.AddSingleton(provider =>
            //{
            //    var hubConnection = new HubConnectionBuilder()
            //    .WithUrl("https://192.168.0.12:5284/chatHub", options =>
            //    {
            //        options.HttpMessageHandlerFactory = (message) =>
            //        {
            //            if (message is HttpClientHandler clientHandler)
            //                // Accept self-signed certificates for development
            //                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            //            return message;
            //        };
            //    })
            //    .WithAutomaticReconnect()
            //    .Build();


            //    return hubConnection;
            //});
            //builder.Services.AddSingleton<HttpClient>(provider =>
            //{
            //    // Create an instance with the base address configured
            //    var client = new HttpClient
            //    {
            //        BaseAddress = new Uri($"http://{NetworkUtils.GlobalIPAddress()}:5000/"),  // Using HTTP here

            //        Timeout = TimeSpan.FromSeconds(30)
            //    };
            //    return client;
            //});

            builder.Services.AddHttpClient("ChatAPI", client =>
            {
                client.BaseAddress = new Uri($"http://{NetworkUtils.GlobalIPAddress()}:5000/");  // Using HTTP here
                client.Timeout = TimeSpan.FromSeconds(30);
            });


            //builder.Services.AddTransient<RoomPage>(provider =>
            //{
            //    var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
            //    var httpClient = clientFactory.CreateClient("ChatAPI");
            //    return new RoomPage(httpClient);
            //});

            // Register MainPage with dependency injection
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<RoomPage>();
            builder.Services.AddTransient<MessagePage>();
            builder.Services.AddSingleton<HttpClient>();

            builder.Services.AddTransient<RoomViewModel>();
            builder.Services.AddTransient<MessageViewModel>();
            //builder.Services.AddSingleton<ChatRoomService>();

            //builder.Services.AddSingleton<SqliteDataAccess>();

            //builder.Services.AddHttpClient();

            return builder.Build();
        }
    }
}
