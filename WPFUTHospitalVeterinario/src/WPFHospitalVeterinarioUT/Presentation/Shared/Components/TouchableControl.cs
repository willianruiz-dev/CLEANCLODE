using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Components;

public class TouchableControl : UserControl
{
    public event EventHandler TouchOrMouseDown;

    public TouchableControl()
    {
        // Attach event handlers for both MouseDown and TouchDown events
        PreviewMouseDown += TouchableControl_MouseDown;
        PreviewTouchDown += TouchableControl_TouchDown;
    }

    private void TouchableControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
        OnTouchOrMouseDown();
    }

    private void TouchableControl_TouchDown(object sender, TouchEventArgs e)
    {
        OnTouchOrMouseDown();
    }

    protected virtual void OnTouchOrMouseDown()
    {
        TouchOrMouseDown?.Invoke(this, EventArgs.Empty);
    }
}
