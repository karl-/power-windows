using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Parabox.WindowMover
{
	static class WindowUtility
	{
		[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
		static extern IntPtr SetWindowPosInternal(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint wFlags);

		public static bool SetWindowPos(IntPtr hWnd, Rectangle rect, SetWindowPositionFlags flags)
		{
			return SetWindowPosInternal(hWnd, (int)InsertWindowOrder.Top, rect.Left, rect.Top, rect.Width, rect.Height, (uint)flags) != IntPtr.Zero;
		}

		[DllImport("user32.dll", EntryPoint = "GetWindowRect")]
		static extern bool GetWindowRectInternal(IntPtr hWnd, ref Rect rect);

		public static bool GetWindowRect(IntPtr hWnd, ref Rectangle rect)
		{
			Rect r = new Rect();
			bool ret = GetWindowRectInternal(hWnd, ref r);
			rect = (Rectangle)r;
			return ret;			
		}

		[DllImport("user32.dll", EntryPoint = "WindowFromPoint")]
		public static extern IntPtr WindowFromPoint(Point point);

		[DllImport("Kernel32.dll", EntryPoint = "GetLastError")]
		public static extern uint GetLastError();

		[StructLayout(LayoutKind.Sequential)]
		struct Rect
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public static explicit operator Rectangle(Rect rect)
			{
				return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct WindowPlacement
		{
			uint length;
			uint flags;
			uint showCmd;
			Point ptMinPosition;
			Point ptMaxPosition;
			Rect rcNormalPosition;
		}

	}
}
