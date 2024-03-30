using System;

using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Storage.Streams;

using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace SonicLair.Lib.Services.MediaIntergration.Windows
{
    internal class Control
    {
        private MediaPlayer _mediaPlayer;
        private IMusicPlayerService _musicPlayerService;
        private SystemMediaTransportControls _systemMediaTransportControls;
        private void sendNotification(string line1, string line2)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, "Icon.png");
            sendNotification(line1, line2, filePath);
        }

        private void sendNotification(string line1, string line2, string imageUri)
        {

            // Construct the content and show the toast!
            new ToastContentBuilder()


                // Inline image
                .AddInlineImage(new Uri(imageUri))

                // Profile (app logo override) image
                // .AddAppLogoOverride(new Uri(@"D:\UserData\Documents\GitHub\SonicLair.Cli\Icon.png"), ToastGenericAppLogoCrop.Circle)
                .AddText(line1, AdaptiveTextStyle.Title)
                .AddText(line2, AdaptiveTextStyle.Default)

                .Show();
            // var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
            // var stringElements = toastXml.GetElementsByTagName("text");
            // stringElements[0].AppendChild(toastXml.CreateTextNode(line1));
            // stringElements[1].AppendChild(toastXml.CreateTextNode(line2));

            // // add icon in visual area
            // // https://learn.microsoft.com/en-us/uwp/schemas/tiles/toastschema/element-image
            // string filePath = imageUri;
            // var imageElements = toastXml.GetElementsByTagName("image");
            // imageElements[0].Attributes.GetNamedItem("src").NodeValue = filePath;

            // var placement = toastXml.CreateAttribute("placement");
            // placement.Value = "appLogoOverride";
            // imageElements[0].Attributes.SetNamedItem(placement);

            // // debug write
            // // Set a variable to the Documents path.
            // string docPath =
            // Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // // Write the string array to a new file named "WriteLines.txt".
            // using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "Toast.xml")))
            // {
            //     outputFile.WriteLine(toastXml.GetXml());
            // }

            // // show
            // var toast = new ToastNotification(toastXml);
            // ToastNotificationManager.CreateToastNotifier("SonicLair").Show(toast);
        }

        public Control(IMusicPlayerService musicPlayerService)
        {
            _musicPlayerService = musicPlayerService;
            _mediaPlayer = new MediaPlayer();
            _systemMediaTransportControls = _mediaPlayer.SystemMediaTransportControls;
            _systemMediaTransportControls.IsEnabled = false;
            _mediaPlayer.CommandManager.IsEnabled = false;

            _systemMediaTransportControls.IsPlayEnabled = true;
            _systemMediaTransportControls.IsPauseEnabled = true;
            _systemMediaTransportControls.IsStopEnabled = true;
            _systemMediaTransportControls.IsNextEnabled = true;
            _systemMediaTransportControls.IsPreviousEnabled = true;

            _systemMediaTransportControls.ButtonPressed += systemMediaControls_ButtonPressed;
            _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;

            sendNotification("SonicLair is running", "on Windows");
        }

        public void Update(string title, string artist, string album, string imageUri)
        {
            _systemMediaTransportControls.IsEnabled = true;
            // https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/system-media-transport-controls
            // Get the updater.
            SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
            updater.ClearAll();

            // Music metadata.
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Artist = artist;
            updater.MusicProperties.AlbumTitle = album;
            updater.MusicProperties.Title = title;


            // download thumbnail
            if (imageUri.StartsWith("http"))
            {
                string remoteUri = imageUri;
                string fileName = "album.png", myStringWebResource = null;

                // Create a new WebClient instance.
                using (WebClient myWebClient = new())
                {
                    myStringWebResource = remoteUri;
                    // Download the Web resource and save it into the current filesystem folder.
                    myWebClient.DownloadFile(myStringWebResource, fileName);
                }

                imageUri = Path.Combine(Environment.CurrentDirectory, fileName);
            }
            
            // RandomAccessStreamReference is defined in Windows.Storage.Streams
            var tmp = Task.Run(async () =>
            {
                StorageFile sampleFile = await StorageFile.GetFileFromPathAsync(imageUri);
                updater.Thumbnail =
                RandomAccessStreamReference.CreateFromFile(sampleFile);
                // Update the system media transport controls.
                updater.Update();}
            );
            tmp.Wait();
        
            sendNotification(title, artist + " :: " + album, imageUri);
        }

        public void Play()
        {
            _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
        }
        public void Pause()
        {
            _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
        }

        private void systemMediaControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            // The system media transport control's ButtonPressed event may not fire on the app's UI thread.  XAML controls 
            // (including the MediaPlayerElement control in our page as well as the scenario page itself) typically can only be 
            // safely accessed and manipulated on the UI thread, so here for simplicity, we dispatch our entire event handling 
            // code to execute on the UI thread, as our code here primarily deals with updating the UI and the MediaPlayerElement.
            // 
            // Depending on how exactly you are handling the different button presses (which for your app may include buttons 
            // not used in this sample scenario), you may instead choose to only dispatch certain parts of your app's 
            // event handling code (such as those that interact with XAML) to run on UI thread.
            // Because the handling code is dispatched asynchronously, it is possible the user may have
            // navigated away from this scenario page to another scenario page by the time we are executing here.
            // Check to ensure the page is still active before proceeding.
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    _musicPlayerService.Play();
                    break;

                case SystemMediaTransportControlsButton.Pause:
                    _musicPlayerService.Pause();
                    break;

                case SystemMediaTransportControlsButton.Stop:
                    _musicPlayerService.Pause();
                    break;

                case SystemMediaTransportControlsButton.Next:
                    // range-checking will be performed in SetNewMediaItem()
                    _musicPlayerService.ToggleNext();
                    break;

                case SystemMediaTransportControlsButton.Previous:
                    // range-checking will be performed in SetNewMediaItem()
                    _musicPlayerService.Prev();
                    break;

                    // Insert additional case statements for other buttons you want to handle in your app.
                    // Remember that you also need to first enable those buttons via the corresponding
                    // Is****Enabled property on the SystemMediaTransportControls object.
            }
        }
    }
}
