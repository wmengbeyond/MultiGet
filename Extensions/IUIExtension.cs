using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;//wm delete ����System.Windows.Forms

namespace Imgo.MultiGet.Extensions
{
    public interface IUIExtension
    {
        Control[] CreateSettingsView();

        void PersistSettings(Control[] settingsView);
    }
}
