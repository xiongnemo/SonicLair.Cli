using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonicLair.Lib.Types
{
    public interface IMediaIntergration
    {
        Task Update(string title, string artist, string album, string imageUri);
        void Play();
        void Pause();
    }
}