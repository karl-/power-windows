using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parabox.WindowMover
{
	public enum ShowState
	{
		/// Hides the window and activates another window.
		Hide = 0,

		/// Maximizes the specified window.
		Maximize = 3,

		/// Minimizes the specified window and activates the next top-level window in the z-order.
		Minimize = 6,

		/// Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
		Restore = 9,

		/// Activates the window and displays it in its current size and position.
		Show = 5,

		/// Activates the window and displays it as a maximized window.
		ShowMaximized = 3,

		/// Activates the window and displays it as a minimized window.
		ShowMinimized = 2,

		/// Displays the window as a minimized window.
		/// This value is similar to SHOWMINIMIZED, except the window is not activated.
		ShowMinNoActive = 7,

		/// Displays the window in its current size and position.
		/// This value is similar to SHOW, except the window is not activated.
		ShowNA = 8,

		/// Displays a window in its most recent size and position.
		/// This value is similar to SHOWNORMAL, except the window is not activated.
		ShowNoActivate = 4,

		/// Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
		ShowNormal = 1,
	}

}
