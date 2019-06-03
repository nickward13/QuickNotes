using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Services.MicrosoftGraph;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Net.Http;
using Windows.UI.ViewManagement;
using Microsoft.Toolkit.Uwp.UI.Controls.Graph;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace QuickNotes
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            InitializeAuth();
            SetDefaultPageSize();
        }

        private void SetDefaultPageSize()
        {
            Size preferredSize = new Size(500, 50);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(preferredSize);
            ApplicationView.PreferredLaunchViewSize = preferredSize;
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        private void InitializeAuth()
        {
            LoadOAuthSettings();
            SignIn();
        }

        private void SignIn()
        {
            SetAuthState(false);
            SignInToAad();
        }

        private void SignOut()
        {
            SignOutFromAad();
            SetAuthState(false);
        }

        private async void SignOutFromAad()
        {
            SetSignInButtonToSignIn();
            await AadLoginControl.SignOutAsync();
        }

        private void SignInToAad()
        {
            SetSignInButtonToSigningIn();
            AadLoginControl.SignInAsync();
        }

        private void SetSignInButtonToSigningIn()
        {
            SignInButton.Content = "Signing in";
            SignInButton.IsEnabled = false;
        }

        private void SetSignInButtonToSignIn()
        {
            SignInButton.Content = "Sign in";
            SignInButton.IsEnabled = true;
        }

        private void LoadOAuthSettings()
        {
            var oauthSettings = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("OAuth");
            var appId = oauthSettings.GetString("AppId");
            var scopes = oauthSettings.GetString("Scopes");

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(scopes))
            {
                Notification.Show("Could not load OAuth Settings from resource file.");
            }
            else
            {
                InitializeGraph(appId, scopes);
            }
        }

        private static void InitializeGraph(string appId, string scopes)
        {
            MicrosoftGraphService.Instance.AuthenticationModel = MicrosoftGraphEnums.AuthenticationModel.V2;
            MicrosoftGraphService.Instance.Initialize(appId,
                MicrosoftGraphEnums.ServicesToInitialize.UserProfile,
                scopes.Split(' '));
        }

        private void SetAuthState(bool isAuthenticated)
        {
            if (isAuthenticated)
                ShowControlsForSignedInState();
            else
                ShowControlsForSignedOutState();
        }

        private void ShowControlsForSignedInState()
        {
            NoteTitleTextBox.IsEnabled = true;
            CreateQuickNoteButton.IsEnabled = true;
            SignInButton.Visibility = Visibility.Collapsed;
            SignOutButton.Visibility = Visibility.Visible;
            NoteTitleTextBox.PlaceholderText = "Type a thought and press enter...";
            NoteTitleTextBox.Focus(FocusState.Keyboard);
        }

        private void ShowControlsForSignedOutState()
        {
            NoteTitleTextBox.IsEnabled = false;
            CreateQuickNoteButton.IsEnabled = false;
            SignInButton.Visibility = Visibility.Visible;
            SignOutButton.Visibility = Visibility.Collapsed;
            NoteTitleTextBox.PlaceholderText = "Please sign in first";
        }

        private void Login_SignInCompleted(object sender, Microsoft.Toolkit.Uwp.UI.Controls.Graph.SignInEventArgs e)
        {
            SetAuthState(true);
        }

        private void Login_SignOutCompleted(object sender, EventArgs e)
        {
            SetAuthState(false);
        }

        private void CreateQuickNoteButton_Click(object sender, RoutedEventArgs e)
        {
            CreatePageInOneNote();
        }

        private void NoteTitleTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                CreatePageInOneNote();
            }
        }

        public async Task CreatePageInOneNote()
        {
            if (String.IsNullOrWhiteSpace(NoteTitleTextBox.Text))
                return;

            QuickNote.SaveToOneNoteAsync(NoteTitleTextBox.Text);
            ResetNoteTitleTextBox();
        }

        private void ResetNoteTitleTextBox()
        {
            NoteTitleTextBox.Text = string.Empty;
            NoteTitleTextBox.Focus(FocusState.Keyboard);
        }

        private string GetAndClearNoteTitle()
        {
            string noteTitle = NoteTitleTextBox.Text;
            NoteTitleTextBox.Text = string.Empty;
            return noteTitle;
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            SignIn();
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            SignOut();
        }
    }
}
