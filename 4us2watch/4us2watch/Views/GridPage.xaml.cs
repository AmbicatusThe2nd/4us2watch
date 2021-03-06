﻿using _4us2watch.Components;
using _4us2watch.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using MoreLinq;
using Rg.Plugins.Popup.Services;

namespace _4us2watch.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GridPage : ContentPage
    {
        ProfilePage profile = null;
        GridPage grid = null;
        LoginPage lgnpg = null;
        public string email;
        public string MainApi = "https://api.themoviedb.org/3/movie/";
        List<Movie> UserMovies;
        public GridPage(string text)
        {
            InitializeComponent();
            email = text;
            NavigationPage.SetHasNavigationBar(this, false);
            BindingContext = this;
            //_menuItemsView = new[] { (View)LabelSlikaTest, LabelTest, LabelSlikaDvaTest, LabelDvaTest };
            CreateAndFillGrid(MovieGrid, 0);
            FillFriendsList(Friends);
        }
        private const string ExpandAnimationName = "ExpandAnimation";
        private const string CollapseAnimationName = "CollapseAnimation";
        private const string KeyApi = "/recommendations?api_key=9d2bff12ed955c7f1f74b83187f188ae&language=en-US&page=";
        private const double SlideAnimationDuration = 0.25;
        private const int AnimationDuration = 600;
        private const double PageScale = 0.9;
        private const double PageTranslation = 0.35;
        //private IEnumerable<View> _menuItemsView;
        private bool _isAnimationRun;
        private double _safeInsetsTop;

        private async void onAddFriendClick(object sender, EventArgs args)
        {
            var user = await ReaderWriter.GetPerson(email);
            var response = await ReaderWriter.GetPersonByUsername(friendsEntry.Text);
            if (response == null || user.email == response.email) //Dodal da nemores dodat sebe.
            {
                if (string.IsNullOrWhiteSpace(friendsEntry.Text))
                {
                    await DisplayAlert("Error", "Enter a username first!", "OK");
                    return;
                }
                else
                {
                    await DisplayAlert("User not found", "The user you searched for not found!", "OK");
                    friendsEntry.Text = "";
                    return;
                }
            }
            foreach (string a in user.friends)
            {
                if (a == response.username) //Da nemoreš dodat večkrat.
                {
                    await DisplayAlert("Already friends", "The user is already your friend!", "OK");
                    friendsEntry.Text = "";
                    return;
                }
            }
            if (user.friends.Count >= 20)
            {
                await DisplayAlert("Friend limit reached", "You have reached the maximum number of friends!", "OK"); //max frendov
                friendsEntry.Text = "";
                return;
            }
            await ReaderWriter.UpdateFriendsList(email, response);
            friendsEntry.Text = "";
            FillFriendsList(Friends);
            await DisplayAlert("Success", "Friend added!", "Close");
        }

        private Queue<Movie> FillTheQueueWithMovies(List<string> MovieIds)
        {
            // TO DO: Fill the queue with the recommandations of the user
            // 10 Movies on one recomandation 
            // If the user doesn't like any movie or only one out of 20 get him a genre assignment page again with diffrent movies 
            //var random = new Random();
            var pageNumber = 1;
            var mainQueue = new Queue<Movie>();
            //var movieId = MovieIds[0]; // Lets first see for the first one and lets see how it goes
            foreach (var MovieId in MovieIds)
            {
                var concatAPI = MainApi + MovieId + KeyApi;
                var client = new HttpClient();
                client.BaseAddress = new Uri(concatAPI);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.GetAsync(concatAPI + pageNumber.ToString()).Result; // Main result for API
                if (response.IsSuccessStatusCode)
                {
                    var convertedString = response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MoviePage>(convertedString.Result);
                    data.Results.Skip(Math.Max(0, data.Results.Count() - 10)); // This takes only the first 10 movies in the API call
                    foreach (var movie in data.Results)
                    {
                        mainQueue.Enqueue(movie);
                    }
                }
                else
                {
                    DisplayAlert("Error", "The api did not return a success status code", "OK");
                }
            }
            return new Queue<Movie>(mainQueue.DistinctBy(x => x.Name));
        }
        private async void FillFriendsList(Grid grid)
        {
            try
            {
                var user = await ReaderWriter.GetPerson(email);
                var counter = 3;
                if (user == null)
                {
                    return;
                }
                foreach (var friend in user.friends)
                {
                    Image img = new Image
                    {
                        Source = "profile.jpg", //profilna uporabnika
                        HeightRequest = 25,
                        VerticalOptions = LayoutOptions.End,
                        HorizontalOptions = LayoutOptions.Center
                    };
                    Label lbl = new Label
                    {
                        Text = friend, //spremeni pol v username
                        FontSize = 18,
                        TextColor = Color.Black,
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.Start,
                        Padding = new Thickness(0, -3, 0, 0)
                    };
                    var eventOnTap = new TapGestureRecognizer();
                    eventOnTap.Tapped += async (s, e) =>
                    {
                        string action = await DisplayActionSheet("What would you like to do with your friend " + friend + "?", "Cancel", null, "Display movies you both like", "Remove friend");
                        //bool decision = await DisplayAlert(friend, "What would you like to do?", "Display movies you both like", "Remove friend");
                        if (action == "Display movies you both like")
                        {
                            await PopupNavigation.Instance.PushAsync(new BusyPopUp());

                            try
                            {
                                var friendMovies = FillTheQueueWithMovies(await ReaderWriter.GetUserMoviesByUsername(friend)).ToList();

                                // Two lists of movie rec. (friendMovies, UserMovies) what now?

                                var commonList = friendMovies.Concat(UserMovies).ToList();
                                var avgOfList = commonList.Select(x => x.Vote_Average).DefaultIfEmpty(0).Average();
                                var finalList = commonList.Where(x => x.Vote_Average > avgOfList).ToList();
                                MovieGrid.Children.Clear();

                                RefreshGrid(MovieGrid, finalList);
                            }
                            finally
                            {
                                await PopupNavigation.Instance.PopAsync();
                            }
                        }
                        else if (action == "Remove friend")
                        {
                            await PopupNavigation.Instance.PushAsync(new BusyPopUp());

                            try
                            {
                                user.friends.Remove(friend);
                                await ReaderWriter.UpdatePerson(user.username, user.email, user.friends, user.movies);

                                // To remove from the list once its removed from the databse
                                var button = (BindableObject)s;
                                var row = Grid.GetRow(button);
                                var column = Grid.GetColumn(button);
                                var getgrid = button as Grid;
                                //assuming the image is in column 1
                                var result = grid.Children.Where(x => Grid.GetRow(x) == row && Grid.GetColumn(x) == column);
                                var resultImg = grid.Children.Where(x => Grid.GetRow(x) == row && Grid.GetColumn(x) == column - 1);
                                foreach (var label in result)
                                {
                                    grid.Children.Remove(label);
                                    break;
                                }
                                foreach (var image in resultImg)
                                {
                                    grid.Children.Remove(image);
                                    break;
                                }
                                await DisplayAlert("Success", "User successfully removed from your friends list", "Close");
                            }
                            finally
                            {
                                await PopupNavigation.Instance.PopAsync();
                            }
                        }
                        else
                        {
                            return;
                        }
                    };
                    lbl.GestureRecognizers.Add(eventOnTap);

                    grid.Children.Add(img, 0, counter); //column, row
                    grid.Children.Add(lbl, 1, counter);
                    Grid.SetColumnSpan(lbl, 2);
                    counter++;
                }
            }
            catch (Exception e)
            {
                await DisplayAlert("Exception", e.Message, "Ok");
            }
        }
        private async void CreateAndFillGrid(Grid grid, int moviegenre)
        {

            // First we need to get the movies the user liked
            var userMovies = await ReaderWriter.GetUserMovies(email);
            var movieQueue = FillTheQueueWithMovies(userMovies);
            UserMovies = movieQueue.ToList();
            movieQueue = new Queue<Movie>(movieQueue.Where(x => x.ImagePath != null).OrderBy(x => Guid.NewGuid()));

            if (moviegenre != 0)
            {
                if (movieQueue.Any(x => x.Genre_Ids.Contains(moviegenre)))
                {
                    movieQueue = new Queue<Movie>(movieQueue.Where(x => x.Genre_Ids.Contains(moviegenre)));
                }
            }

            var fixedMovieQueueCount = movieQueue.Count;
            for (int i = 0; i < fixedMovieQueueCount / 2 + fixedMovieQueueCount % 2; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = 260 });

                for (int j = 0; j < 2; j++)
                {
                    if (movieQueue.Count < 1)
                    {
                        return;
                    }
                    var currentMovie = movieQueue.Dequeue();
                    Frame frame = new Frame
                    {
                        BorderColor = Color.Black,
                        HasShadow = true,
                        Padding = 0,
                        Margin = 0,
                        Content = new Image
                        {
                            Source = "https://image.tmdb.org/t/p/w500" + currentMovie.ImagePath,
                            Aspect = Aspect.AspectFill
                        }
                    };
                    var eventOnTap = new TapGestureRecognizer();
                    eventOnTap.Tapped += (s, e) =>
                    {
                        DisplayAlert(currentMovie.Name + " (" + currentMovie.ReleaseDate.Substring(0, 4) + ")", currentMovie.Overview, "Close");
                    };
                    frame.GestureRecognizers.Add(eventOnTap);

                    grid.Children.Add(frame, j, i); //row, column
                }
            }
        }

        private void RefreshGrid(Grid grid, List<Movie> movies)
        {
            var mainQueue = new Queue<Movie>(movies);
            // movieQueue = new Queue<Movie>(movieQueue.Where(x => x.ImagePath != null).OrderBy(x => Guid.NewGuid()));
            mainQueue = new Queue<Movie>(mainQueue.Where(x => x.ImagePath != null).OrderBy(x => Guid.NewGuid()));
            var fixedMovieQueueCount = mainQueue.Count;

            for (int i = 0; i < fixedMovieQueueCount / 2 + fixedMovieQueueCount % 2; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = 260 });

                for (int j = 0; j < 2; j++)
                {
                    if (mainQueue.Count < 1)
                    {
                        return;
                    }
                    var currentMovie = mainQueue.Dequeue();
                    Frame frame = new Frame
                    {
                        BorderColor = Color.Black,
                        HasShadow = true,
                        Padding = 0,
                        Margin = 0,
                        Content = new Image
                        {
                            Source = "https://image.tmdb.org/t/p/w500" + currentMovie.ImagePath,
                            Aspect = Aspect.AspectFill
                        }
                    };
                    var eventOnTap = new TapGestureRecognizer();
                    eventOnTap.Tapped += (s, e) =>
                    {
                        DisplayAlert(currentMovie.Name + " (" + currentMovie.ReleaseDate.Substring(0, 4) + ")", currentMovie.Overview, "Close");
                    };
                    frame.GestureRecognizers.Add(eventOnTap);

                    grid.Children.Add(frame, j, i); //row, column
                }
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            MenuTopRow.Height = MenuBottomRow.Height = Device.Info.ScaledScreenSize.Height * (1 - PageScale) / 2;
        }
        void HelpCommand(object sender, EventArgs args)
        {
            DisplayAlert("Help", "The movies displayed in the grid below are the result of your choices in the liking/disliking the movies we've shown you" +
                " at your first log in passed into our recommendation system. \nIf you wish to know more about a movie you see in the grid, just tap it! \nIf you'd like to recalibrate your recommendations, go to your " +
                "profile page and tap the 'recommendation recalibration' button. \nIf you'd like to see which movies you and your friends share, add a friend, tap on their name in the friends list " +
                "and select the 'Display movies you both like' option.", "OK");
        }
        async void LogOutCommand(object sender, EventArgs args)
        {
            bool decision = await DisplayAlert("Log out", "Are you sure you want to log out?", "Yes", "No");
            if (decision == true)
            {
                if (lgnpg == null)
                {
                    lgnpg = new LoginPage();
                }
                App.Current.MainPage = new NavigationPage(lgnpg);
            }
            else
            {
                //nič
            };
        }
        async void HomeCommand(object sender, EventArgs args)
        {
            bool decision = await DisplayAlert("Refresh", "Are you sure you want to refresh your recommendations?", "Yes", "No");
            if (decision == true)
            {
                await PopupNavigation.Instance.PushAsync(new BusyPopUp());

                try
                {
                    CreateAndFillGrid(MovieGrid, 0);
                }
                finally
                {
                    await PopupNavigation.Instance.PopAsync();
                }
            }
            else
            {
                //nič
            };
        }
        void FriendsCommand(object sender, EventArgs args)
        {
            OnShowMenu();
        }
        void ProfileCommand(object sender, EventArgs args)
        {
            if (profile == null)
            {
                profile = new ProfilePage(email);
            }
            App.Current.MainPage = new NavigationPage(profile);
        }
        async void AdventureCommand(object sender, EventArgs args)//Adventure
        {
            await PopupNavigation.Instance.PushAsync(new BusyPopUp());

            try
            {
                CreateAndFillGrid(MovieGrid, 12);
            }
            finally
            {
                await PopupNavigation.Instance.PopAsync();
            }
        }
        async void ActionCommand(object sender, EventArgs args)//Action
        {
            await PopupNavigation.Instance.PushAsync(new BusyPopUp());

            try
            {
                CreateAndFillGrid(MovieGrid, 28);
            }
            finally
            {
                await PopupNavigation.Instance.PopAsync();
            }
        }
        async void ComedyCommand(object sender, EventArgs args)//Comedy
        {
            await PopupNavigation.Instance.PushAsync(new BusyPopUp());

            try
            {
                CreateAndFillGrid(MovieGrid, 35);
            }
            finally
            {
                await PopupNavigation.Instance.PopAsync();
            }
        }
        async void HorrorCommand(object sender, EventArgs args)//Horror
        {
            await PopupNavigation.Instance.PushAsync(new BusyPopUp());

            try
            {
                CreateAndFillGrid(MovieGrid, 27);
            }
            finally
            {
                await PopupNavigation.Instance.PopAsync();
            }
        }

        public async void OnShowMenu()
        {
            if (_isAnimationRun)
                return;

            _isAnimationRun = true;
            var animationDuration = AnimationDuration;
            if (Page.Scale < 1)
            {
                animationDuration = (int)(AnimationDuration * SlideAnimationDuration);
                GetCollapseAnimation().Commit(this, CollapseAnimationName, 16,
                    (uint)(AnimationDuration * SlideAnimationDuration),
                    Easing.Linear,
                    null, () => false);
            }
            else
            {
                GetExpandAnimation().Commit(this, ExpandAnimationName, 16,
                    AnimationDuration,
                    Easing.Linear,
                    null, () => false);
            }

            await Task.Delay(animationDuration);
            _isAnimationRun = false;
        }

        private Animation GetExpandAnimation()
        {
            //var iconAnimationTime = (1 - SlideAnimationDuration) / _menuItemsView.Count();
            var animation = new Animation
            {
                {0, SlideAnimationDuration, new Animation(v => ToolbarSafeAreaRow.Height = v, _safeInsetsTop, 0)},
                {
                    0, SlideAnimationDuration,
                    new Animation(v => Page.TranslationX = v, 0, Device.Info.ScaledScreenSize.Width * PageTranslation)
                },
                {0, SlideAnimationDuration, new Animation(v => Page.Scale = v, 1, PageScale)},
                {
                    0, SlideAnimationDuration,
                    new Animation(v => Page.Margin = new Thickness(0, v, 0, 0), 0, _safeInsetsTop)
                },
                {0, SlideAnimationDuration, new Animation(v => Page.CornerRadius = (float) v, 0, 5)}
            };

            //foreach (var view in _menuItemsView)
            //{
            //    var index = _menuItemsView.IndexOf(view);
            //    animation.Add(SlideAnimationDuration + iconAnimationTime * index - 0.05,
            //        SlideAnimationDuration + iconAnimationTime * (index + 1) - 0.05, new Animation(
            //            v => view.Opacity = (float)v, 0, 1));
            //    animation.Add(SlideAnimationDuration + iconAnimationTime * index,
            //        SlideAnimationDuration + iconAnimationTime * (index + 1), new Animation(
            //            v => view.TranslationY = (float)v, -10, 0));
            //}

            return animation;
        }

        private Animation GetCollapseAnimation()
        {
            var animation = new Animation
            {
                {0, 1, new Animation(v => ToolbarSafeAreaRow.Height = v, 0, _safeInsetsTop)},
                {0, 1, new Animation(v => Page.TranslationX = v, Device.Info.ScaledScreenSize.Width * PageTranslation, 0)},
                {0, 1, new Animation(v => Page.Scale = v, PageScale, 1)},
                {0, 1, new Animation(v => Page.Margin = new Thickness(0, v, 0, 0), _safeInsetsTop, 0)},
                {0, 1, new Animation(v => Page.CornerRadius = (float) v, 5, 0)}
            };

            //foreach (var view in _menuItemsView)
            //{
            //    animation.Add(0, 1, new Animation(
            //        v => view.Opacity = (float)v, 1, 0));
            //    animation.Add(0, 1, new Animation(
            //        v => view.TranslationY = (float)v, 0, -10));
            //}

            return animation;
        }
        protected override bool OnBackButtonPressed() => true; //da ne more backoutat, ker se ruši navigacija
    }
}