using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Parabox.WindowMover
{
	public struct WindowPlacement
	{
		public ShowState showState;
		public Point ptMinPosition;
		public Point ptMaxPosition;
		public Rectangle rcNormalPosition;
	}

	static class WindowUtility
	{
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
		struct WindowPlacementInternal
		{
			public uint length;
			public uint flags;
			public uint showCmd;
			public Point ptMinPosition;
			public Point ptMaxPosition;
			public Rect rcNormalPosition;
		}

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

		[DllImport("user32.dll", EntryPoint = "GetWindowPlacement")]
		static extern bool GetWindowPlacementInternal(IntPtr hWnd, IntPtr windowPlacement);

		public static bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement windowPlacement)
		{
			var wp = new WindowPlacementInternal();
			wp.length = (uint)Marshal.SizeOf(typeof(WindowPlacementInternal));

			int size = Marshal.SizeOf(typeof(WindowPlacementInternal));
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(wp, ptr, false);
			bool ret = GetWindowPlacementInternal(hWnd, ptr);
			wp = (WindowPlacementInternal) Marshal.PtrToStructure(ptr, typeof(WindowPlacementInternal));
			Marshal.FreeHGlobal(ptr);

			windowPlacement.showState = (ShowState) wp.showCmd;
			windowPlacement.ptMinPosition = wp.ptMinPosition;
			windowPlacement.ptMaxPosition = wp.ptMaxPosition;
			windowPlacement.rcNormalPosition = (Rectangle) wp.rcNormalPosition;

			return ret;
		}
	}
}
