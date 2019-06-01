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
using Microsoft.Graph;
using Windows.UI.Notifications;
using System.Threading.Tasks;
using System.Net.Http;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace QuickNotes
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();

            if ((App.Current as App).IsAuthenticated)
            {
                HomePageMessage.Text = "Welcome! Please use the menu to the left to select a view.";
            }
        }

        private void CreateQuickNoteButton_Click(object sender, RoutedEventArgs e)
        {

            OneNoteAddPageHtml();
        }

        public async Task OneNoteAddPageHtml()
        {
            GraphServiceClient graphClient = MicrosoftGraphService.Instance.GraphProvider;

            // Get a page of OneNote sections.
            IOnenoteSectionsCollectionPage sectionPage = await graphClient.Me.Onenote.Sections.Request().GetAsync();

            // Get a handle to the first section.
            string sectionId = sectionPage[0].Id;

            // Get the request URL for adding a page. 
            string requestUrl = graphClient.Me.Onenote.Sections[sectionId].Pages.Request().RequestUrl;

            string htmlBody = @"<!DOCTYPE html><html><head><title>OneNoteAddPageHtml created this</title></head>
                        <body>Generated with love</body></html> ";

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
    }
}
