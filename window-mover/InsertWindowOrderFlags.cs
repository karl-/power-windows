using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parabox.WindowMover
{
	enum InsertWindowOrder
	{
		/// Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
		Bottom = 1,
		/// Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
		NoTopMost = -2,
		/// Places the window at the top of the Z order.
		Top = 0,
		/// Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
		TopMost = -1,
	}
}
