﻿using Auth0.OidcClient;

namespace ChatAppRum
{
    public partial class App : Application
    {
        public App()
        {

            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}
