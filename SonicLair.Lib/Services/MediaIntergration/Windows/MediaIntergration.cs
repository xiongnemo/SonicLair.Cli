using SonicLair.Lib.Types;

namespace SonicLair.Lib.Services.MediaIntergration.Windows
{
    public class MediaIntergration : IMediaIntergration
    {
        private Control _control;

        public MediaIntergration(IMusicPlayerService musicPlayerService)
        {
            _control = new Control(musicPlayerService);
        }

        public void Update(string title, string artist, string album, string imageUri)
        {
            _control.Update(title, artist, album, imageUri);
        }

        public void Play()
        {
            _control.Play();
        }

        public void Pause()
        {
            _control.Pause();
        }
    }
}
