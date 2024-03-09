namespace SonicLair.Lib.Types.SonicLair
{
    public class CurrentState
    {
        public Song CurrentTrack { get; set; }
        public decimal Position { get; set; }
        public bool IsPlaying { get; set; }
        public bool Stopped { get; set; }
        public Playlist CurrentPlaylist { get; set; }
        public RepeatStatus RepeatStatus { get; set; }
        public bool IsShuffled { get; set; }
    }
}
