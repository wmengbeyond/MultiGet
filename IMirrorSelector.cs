using System;
using System.Collections.Generic;
using System.Text;

namespace Imgo.MultiGet
{
    public interface IMirrorSelector
    {
        void Init(Downloader downloader);

        ResourceLocation GetNextResourceLocation(); 
    }
}
