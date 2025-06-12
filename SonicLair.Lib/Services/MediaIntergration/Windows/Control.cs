using System;

using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Storage.Streams;

using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

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
            #if DEBUG
            // Construct the content and show the toast!
            new ToastContentBuilder()
                // Inline image
                .AddInlineImage(new Uri(imageUri))
                // .AddAppLogoOverride(new Uri(@"D:\UserData\Documents\GitHub\SonicLair.Cli\Icon.png"), ToastGenericAppLogoCrop.Circle)
                .AddText(line1, AdaptiveTextStyle.Title)
                .AddText(line2, AdaptiveTextStyle.Default)
                .Show();
            #endif
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

        public async Task Update(string title, string artist, string album, string imageUri)
        {
            _systemMediaTransportControls.IsEnabled = true;
            // https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/system-media-transport-controls
            SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
            updater.ClearAll();

            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Artist = artist;
            updater.MusicProperties.AlbumTitle = album;
            updater.MusicProperties.Title = title;


            // download thumbnail
            if (imageUri.StartsWith("http"))
            {
                string remoteUri = imageUri;
                string fileName = "album.png";

                using (HttpClient httpClient = new())
                {
                    byte[] imageBytes = await httpClient.GetByteArrayAsync(remoteUri);
                    await File.WriteAllBytesAsync(fileName, imageBytes);
                }

                imageUri = Path.Combine(Environment.CurrentDirectory, fileName);
            }

            var tmp = Task.Run(async () =>
            {
                StorageFile sampleFile = await StorageFile.GetFileFromPathAsync(imageUri);
                updater.Thumbnail =
                RandomAccessStreamReference.CreateFromFile(sampleFile);
                updater.Update();
            }
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
                    _musicPlayerService.ToggleNext();
                    break;

                case SystemMediaTransportControlsButton.Previous:
                    _musicPlayerService.Prev();
                    break;
            }
        }
    }
}
