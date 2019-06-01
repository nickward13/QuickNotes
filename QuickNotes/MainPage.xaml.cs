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
            Size preferredSize = new Size(800, 50);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(preferredSize);
            ApplicationView.PreferredLaunchViewSize = preferredSize;
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        private void InitializeAuth()
        {
            SetAuthState(false);
            LoadOAuthSettings();
            AadLoginControl.SignInAsync();
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
                // Initialize Graph
                MicrosoftGraphService.Instance.AuthenticationModel = MicrosoftGraphEnums.AuthenticationModel.V2;
                MicrosoftGraphService.Instance.Initialize(appId,
                    MicrosoftGraphEnums.ServicesToInitialize.UserProfile,
                    scopes.Split(' '));
            }
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
            NoteTitleTextBox.PlaceholderText = "Type your thought and press enter...";
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

            GraphServiceClient graphClient = MicrosoftGraphService.Instance.GraphProvider;
            string noteTitle = GetAndClearNoteTitle();
            NoteTitleTextBox.Focus(FocusState.Keyboard);

            try
            {
                // Get a page of OneNote sections.
                IOnenoteSectionsCollectionPage sectionPage = await graphClient.Me.Onenote.Sections.Request()
                    .Filter("displayName eq 'Quick Notes'")
                    .GetAsync();

                // Get a handle to the first section.
                string sectionId = sectionPage[0].Id;

                // Get the request URL for adding a page. 
                string requestUrl = graphClient.Me.Onenote.Sections[sectionId].Pages.Request().RequestUrl;

                string htmlBody = String.Concat(
                    @"<!DOCTYPE html><html><head><title>",
                    noteTitle,
                    @"</title></head><body>Generated by QuickNotes</body></html> "
                    );

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                hrm.Content = new StringContent(htmlBody, System.Text.Encoding.UTF8, "text/html");

                // Authenticate (add access token) our HttpRequestMessage
                await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                HttpResponseMessage response = await graphClient.HttpProvider.SendAsync(hrm);

                // Get the OneNote page that we created.
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize into OneNotePage object.
                    var content = await response.Content.ReadAsStringAsync();
                    OnenotePage page = graphClient.HttpProvider.Serializer.DeserializeObject<OnenotePage>(content);
                }
                else
                    throw new ServiceException(
                        new Error
                        {
                            Code = response.StatusCode.ToString(),
                            Message = await response.Content.ReadAsStringAsync()
                        });
            }
            catch (Exception e)
            {
                throw (e);
            }
        }

        private string GetAndClearNoteTitle()
        {
            string noteTitle = NoteTitleTextBox.Text;
            NoteTitleTextBox.Text = string.Empty;
            return noteTitle;
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            AadLoginControl.SignInAsync();
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            AadLoginControl.SignOutAsync();
        }
    }
}
