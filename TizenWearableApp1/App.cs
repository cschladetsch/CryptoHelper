﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace TizenWearableApp1
{
    public class App : Application
    {
        //CoinSpotUpdater.CoinSpot.CoinspotService _coinSpotService;

        public App()
        {
            // The root page of your application
            //_coinSpotService = new CoinSpotUpdater.CoinSpot.CoinspotService();

            MainPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = {
                        new Label {
                            HorizontalTextAlignment = TextAlignment.Center,
                            Text = $"Hello "//{_coinSpotService.GetPortfolioValue()}"
                        }
                    }
                }
            };
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
