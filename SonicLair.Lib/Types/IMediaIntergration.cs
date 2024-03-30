using System.Collections.Generic;

namespace SonicLair.Lib.Types
{
    public interface IMediaIntergration
    {
        void Update(string title, string artist, string album, string imageUri);
        void Play();
        void Pause();
    }
}