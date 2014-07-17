using System;
using System.Collections.Generic;
using System.Text;

namespace Imgo.MultiGet.Extensions
{
    public interface IExtension
    {
        string Name { get; }

        IUIExtension UIExtension { get; }
    }
}
