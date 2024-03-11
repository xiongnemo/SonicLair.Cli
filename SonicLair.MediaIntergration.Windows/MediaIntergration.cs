namespace SonicLair.MediaIntergration.Windows
{
    public class MediaIntergration
    {
        private SystemMediaTransportControls _smtc;

        public MediaIntergration()
        {
            _smtc = new SystemMediaTransportControls();

        }

        public void Update()
        {
            _smtc.Update();
        }

    }
}
