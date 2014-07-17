using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;//wm delete ÒýÓÃSystem.Windows.Forms

namespace Imgo.MultiGet.Extensions
{
    public interface IUIExtension
    {
        Control[] CreateSettingsView();

        void PersistSettings(Control[] settingsView);
    }
}
