using Fiona.Core.Services;
using Fiona.Helpers;
using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Fiona.Views
{
    public sealed partial class FirstRunDialog : ContentDialog
    {
        public FirstRunDialog()
        {
            // TODO WTS: Update the contents of this dialog with any important information you want to show when the app is used for the first time.
            RequestedTheme = (Window.Current.Content as FrameworkElement).RequestedTheme;
            InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (!string.IsNullOrEmpty(Settings_LMS_ServerIP_TextBox.Text) &&
                !string.IsNullOrEmpty(Settings_LMS_ServerPort_TextBox.Text))
            {
                if (FionaDataService.ContactServer(Settings_LMS_ServerIP_TextBox.Text, int.Parse(Settings_LMS_ServerPort_TextBox.Text)))
                { // all good, let's move
                    FionaDataService.ServerIP = Settings_LMS_ServerIP_TextBox.Text;
                    FionaDataService.ServerPort = int.Parse(Settings_LMS_ServerPort_TextBox.Text);
                    await Windows.Storage.ApplicationData.Current.LocalSettings.SaveAsync<string>("ServerIP", FionaDataService.ServerIP);
                    await Windows.Storage.ApplicationData.Current.LocalSettings.SaveAsync<int>("ServerPort", FionaDataService.ServerPort);

                }
                else
                { // can't contact the server
                    //args.GetDeferral();
                    args.Cancel = true;
                }
            }
            else
            { // empty values
                //args.GetDeferral();
                args.Cancel = true;
            }
        }
    }
}
