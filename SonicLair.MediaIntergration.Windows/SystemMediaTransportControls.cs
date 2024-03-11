using System;

using Windows.Media;
using Windows.Media.Playback;
using Windows.Media.Core;

namespace SonicLair.MediaIntergration.Windows
{
    internal class SystemMediaTransportControls
        {
            private MediaPlaybackItem _item;
            public SystemMediaTransportControls()
            {
                _item = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri("")));
            }

            public void Update()
            {
                // https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/integrate-with-systemmediatransportcontrols
                var props = _item.GetDisplayProperties();
                props.Type = MediaPlaybackType.Music;
                props.MusicProperties.Title = "Song title";
                props.MusicProperties.Artist = "Song artist";
                props.MusicProperties.Genres.Add("Polka");
                _item.ApplyDisplayProperties(props);
            }
        }
}
