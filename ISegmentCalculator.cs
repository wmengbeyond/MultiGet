using System;
using System.Collections.Generic;
using System.Text;

namespace Imgo.MultiGet
{
    public interface ISegmentCalculator
    {
        CalculatedSegment[] GetSegments(int segmentCount, RemoteFileInfo fileSize);
    }
}
