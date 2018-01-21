using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using System.Drawing;

namespace Parabox.WindowMover
{
	class WindowManager
	{
		[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
		public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint wFlags);

		[DllImport("user32.dll", EntryPoint = "GetWindowRect")]
		public static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle rect);

		[DllImport("user32.dll", EntryPoint = "WindowFromPoint")]
		public static extern IntPtr WindowFromPoint(Point point);

		[DllImport("Kernel32.dll", EntryPoint = "GetLastError")]
		public static extern uint GetLastError();
		
		IKeyboardMouseEvents m_GlobalHook;
		Form m_ParentForm;
		bool m_IsDragging;
		IntPtr m_DraggingWindowHandle;
		Point m_CursorOffset;
		Rectangle m_WindowRectOrigin;

		public WindowManager(Form parent)
		{
			m_GlobalHook = Hook.GlobalEvents();
			m_GlobalHook.MouseDownExt += OnMouseDownExt;
			m_GlobalHook.MouseDragStartedExt += OnMouseDragStartedExt;
			m_GlobalHook.MouseDragFinishedExt += MouseDragFinishedExt;
			m_GlobalHook.MouseMoveExt += OnMouseMoveExt;
			m_ParentForm = parent;
		}

		~WindowManager()
		{
			m_GlobalHook.MouseDownExt -= OnMouseDownExt;
			m_GlobalHook.MouseDragStartedExt -= OnMouseDragStartedExt;
			m_GlobalHook.MouseDragFinishedExt -= MouseDragFinishedExt;
			m_GlobalHook.MouseMoveExt -= OnMouseMoveExt;

			m_GlobalHook.Dispose();
		}

		void OnMouseDownExt(object sender, MouseEventExtArgs args)
		{
			if (args.Button == MouseButtons.Left)
			{
				m_DraggingWindowHandle = WindowFromPoint(args.Location);
				
				if(m_DraggingWindowHandle != IntPtr.Zero && GetWindowRect(m_DraggingWindowHandle, ref m_WindowRectOrigin))
				{
					m_CursorOffset.X = args.X - m_WindowRectOrigin.Location.X;
					m_CursorOffset.Y = args.Y - m_WindowRectOrigin.Location.Y;
					args.Handled = true;
				}
			}
		}

		void OnMouseDragStartedExt(object sender, MouseEventExtArgs args)
		{
			if (m_DraggingWindowHandle == IntPtr.Zero)
				return;

			args.Handled = true;
			m_IsDragging = true;
		}

		void MouseDragFinishedExt(object sender, MouseEventExtArgs args)
		{
			if (!m_IsDragging)
				return;

			args.Handled = true;
			m_IsDragging = false;
			m_DraggingWindowHandle = IntPtr.Zero;
		}

		void OnMouseMoveExt(object sender, MouseEventExtArgs args)
		{
			if (m_IsDragging)
			{
				if (SetWindowPos(m_DraggingWindowHandle,
					(int) InsertWindowOrder.Top,
					args.X - m_CursorOffset.X,
					args.Y - m_CursorOffset.Y,
					m_ParentForm.Bounds.Width,
					m_ParentForm.Bounds.Height,
					(uint) (
						SetWindowPositionFlags.NoSize |
						SetWindowPositionFlags.NoZOrder |
						SetWindowPositionFlags.ShowWindow
					)) != IntPtr.Zero)
				{
					args.Handled = true;
				}
				else
				{
#if DEBUG
					Console.WriteLine("Failed to set window position: " + GetLastError());
#endif
				}
			}
		}
	}
}
