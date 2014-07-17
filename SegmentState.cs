using System;
using System.Collections.Generic;
using System.Text;

namespace Imgo.MultiGet
{
    public enum SegmentState
    {
        Idle,
        Connecting,
        Downloading,
        Paused,
        Finished,
        Error,
    }
}
