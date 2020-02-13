﻿using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using GitTrends.Mobile.Shared;
using GitTrends.Shared;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace GitTrends
{
    public class ReferringSitesPage : BaseContentPage<ReferringSitesViewModel>
    {
        readonly RefreshView _refreshView;

        public ReferringSitesPage(ReferringSitesViewModel referringSitesViewModel, Repository repository) : base(PageTitles.ReferringSitesPage, referringSitesViewModel)
        {
            const int titleRowHeight = 50;
            const int titleTopMargin = 15;

            var collectionView = new CollectionView
            {
                ItemTemplate = new ReferringSitesDataTemplateSelector(),
                SelectionMode = SelectionMode.Single
            };
            collectionView.SelectionChanged += HandleCollectionViewSelectionChanged;
            collectionView.SetBinding(CollectionView.ItemsSourceProperty, nameof(ReferringSitesViewModel.ReferringSitesCollection));

            _refreshView = new RefreshView
            {
                CommandParameter = (repository.OwnerLogin, repository.Name),
                Content = collectionView
            };
            _refreshView.SetDynamicResource(RefreshView.RefreshColorProperty, nameof(BaseTheme.RefreshControlColor));
            _refreshView.SetBinding(RefreshView.CommandProperty, nameof(ReferringSitesViewModel.RefreshCommand));
            _refreshView.SetBinding(RefreshView.IsRefreshingProperty, nameof(ReferringSitesViewModel.IsRefreshing));

            //Add Title and Back Button to UIModalPresentationStyle.FormSheet 
            if (Device.RuntimePlatform is Device.iOS)
            {
                var closeButton = new Button
                {
                    Text = "Close",
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center,
                    HeightRequest = titleRowHeight * 3 / 5,
                    Padding = new Thickness(5, 0),
                    BorderWidth = 1,
                    IsVisible = DeviceDisplay.MainDisplayInfo.Orientation is DisplayOrientation.Landscape ? true : false
                };
                closeButton.Clicked += HandleCloseButtonClicked;
                closeButton.SetDynamicResource(Button.TextColorProperty, nameof(BaseTheme.NavigationBarTextColor));
                closeButton.SetDynamicResource(Button.BorderColorProperty, nameof(BaseTheme.TrendsChartSettingsBorderColor));
                closeButton.SetDynamicResource(Button.BackgroundColorProperty, nameof(BaseTheme.NavigationBarBackgroundColor));


                var titleRowBlurView = new BoxView { Opacity = 0.5 };
                titleRowBlurView.SetDynamicResource(BackgroundColorProperty, nameof(BaseTheme.PageBackgroundColor));

                var collectionViewHeader = new BoxView { HeightRequest = titleRowHeight + titleTopMargin };
                collectionViewHeader.SetDynamicResource(BackgroundColorProperty, nameof(BaseTheme.PageBackgroundColor));
                collectionView.Header = collectionViewHeader;

                var titleLabel = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    Text = PageTitles.ReferringSitesPage,
                    FontSize = 30
                };
                titleLabel.SetDynamicResource(Label.TextColorProperty, nameof(BaseTheme.TextColor));

                closeButton.Margin = titleLabel.Margin = new Thickness(0, titleTopMargin, 0, 0);

                var activityIndicator = new ActivityIndicator();
                activityIndicator.SetDynamicResource(ActivityIndicator.ColorProperty, nameof(BaseTheme.RefreshControlColor));
                activityIndicator.SetBinding(IsVisibleProperty, nameof(ReferringSitesViewModel.IsRefreshing));
                activityIndicator.SetBinding(ActivityIndicator.IsRunningProperty, nameof(ReferringSitesViewModel.IsRefreshing));

                var relativeLayout = new RelativeLayout();

                relativeLayout.Children.Add(_refreshView,
                                             Constraint.Constant(0),
                                             Constraint.Constant(0),
                                             Constraint.RelativeToParent(parent => parent.Width),
                                             Constraint.RelativeToParent(parent => parent.Height));

                relativeLayout.Children.Add(titleRowBlurView,
                                            Constraint.Constant(0),
                                            Constraint.Constant(0),
                                            Constraint.RelativeToParent(parent => parent.Width),
                                            Constraint.Constant(titleRowHeight));

                relativeLayout.Children.Add(titleLabel,
                                            Constraint.Constant(10),
                                            Constraint.Constant(0));

                relativeLayout.Children.Add(closeButton,
                                            Constraint.RelativeToParent(parent => parent.Width - getWidth(parent, closeButton) - 5),
                                            Constraint.Constant(0),
                                            Constraint.RelativeToParent(parent => getWidth(parent, closeButton)));

                relativeLayout.Children.Add(activityIndicator,
                                            Constraint.RelativeToParent(parent => parent.Width / 2 - getWidth(parent, activityIndicator) / 2),
                                            Constraint.RelativeToParent(parent => parent.Height / 2 - getHeight(parent, activityIndicator) / 2));

                Content = relativeLayout;

                static double getWidth(RelativeLayout parent, View view) => view.Measure(parent.Width, parent.Height).Request.Width;
                static double getHeight(RelativeLayout parent, View view) => view.Measure(parent.Width, parent.Height).Request.Height;
            }
            else
            {
                Content = _refreshView;
            }
        }

        protected override void HandleDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
        {
            base.HandleDisplayInfoChanged(sender, e);

            //On iOS, UIModalPresentationStyle.FormSheet only requires a close button when in Landscape
            if (Device.RuntimePlatform is Device.iOS)
            {
                var layout = (Layout<View>)Content;
                var backButton = layout.Children.OfType<Button>().First();

                backButton.IsVisible = e.DisplayInfo.Orientation is DisplayOrientation.Landscape;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            Disappearing += HandleDisappearing;

            if (_refreshView.Content is CollectionView collectionView && IsNullOrEmpty(collectionView.ItemsSource))
                _refreshView.IsRefreshing = true;

            static bool IsNullOrEmpty(in IEnumerable? enumerable) => !enumerable?.GetEnumerator().MoveNext() ?? true;
        }

        async void HandleCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var collectionView = (CollectionView)sender;
            collectionView.SelectedItem = null;

            if (e?.CurrentSelection.FirstOrDefault() is ReferringSiteModel referingSite
                && referingSite.IsReferrerUriValid
                && referingSite.ReferrerUri != null)
            {
                Disappearing -= HandleDisappearing;

                await OpenBrowser(referingSite.ReferrerUri);
            }
        }

        //Workaround for https://github.com/xamarin/Xamarin.Forms/issues/7878
        async void HandleDisappearing(object sender, EventArgs e)
        {
            if (Navigation.ModalStack.Any())
                await Navigation.PopModalAsync();
        }

        async void HandleCloseButtonClicked(object sender, EventArgs e) => await Navigation.PopModalAsync();
    }
}
