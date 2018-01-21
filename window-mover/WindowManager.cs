using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using System.Drawing;
using System.Collections.Generic;
using WindowsInput;
using WindowsInput.Native;

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
		IInputSimulator m_InputSimulator;

		bool m_WindowKeyDown = false;

		HashSet<Keys> m_IgnoreNextKeyUp;

		public WindowManager(Form parent)
		{
			m_GlobalHook = Hook.GlobalEvents();

			m_GlobalHook.MouseDownExt += OnMouseDownExt;
			m_GlobalHook.MouseDragStartedExt += OnMouseDragStartedExt;
			m_GlobalHook.MouseDragFinishedExt += MouseDragFinishedExt;
			m_GlobalHook.MouseMoveExt += OnMouseMoveExt;

			m_GlobalHook.KeyDown += OnKeyDown;
			m_GlobalHook.KeyUp+= OnKeyUp;

			m_ParentForm = parent;

			m_IgnoreNextKeyUp = new HashSet<Keys>();

			m_InputSimulator = new InputSimulator();
		}

		~WindowManager()
		{
			m_GlobalHook.MouseDownExt -= OnMouseDownExt;
			m_GlobalHook.MouseDragStartedExt -= OnMouseDragStartedExt;
			m_GlobalHook.MouseDragFinishedExt -= MouseDragFinishedExt;
			m_GlobalHook.MouseMoveExt -= OnMouseMoveExt;

			m_GlobalHook.KeyDown -= OnKeyDown;
			m_GlobalHook.KeyUp -= OnKeyUp;

			m_GlobalHook.Dispose();
		}

		void OnKeyDown(object sender, KeyEventArgs args)
		{
			//Console.WriteLine("down: " + args.KeyCode + " (" + args.Modifiers + ")");

			if (args.KeyCode == Keys.LWin)
			{
				args.Handled = true;
				m_WindowKeyDown = true;
			}
		}

		void OnKeyUp(object sender, KeyEventArgs args)
		{
			if (args.KeyCode == Keys.LWin)
			{
				if (m_IgnoreNextKeyUp.Contains(args.KeyCode))
				{
					Console.WriteLine("ignore win");
					m_IgnoreNextKeyUp.Remove(args.KeyCode);
					args.Handled = true;
				}
				else
				{
					m_InputSimulator.Keyboard.KeyDown(VirtualKeyCode.LWIN);
					m_InputSimulator.Keyboard.KeyUp(VirtualKeyCode.LWIN);
				}

				m_WindowKeyDown = false;
			}
		}

		void OnMouseDownExt(object sender, MouseEventExtArgs args)
		{
			if (args.Button == MouseButtons.Left && m_WindowKeyDown)
			{
				m_IgnoreNextKeyUp.Add(Keys.LWin);

				m_DraggingWindowHandle = WindowFromPoint(args.Location);
				
				if(m_DraggingWindowHandle != IntPtr.Zero && GetWindowRect(m_DraggingWindowHandle, ref m_WindowRectOrigin))
				{
					m_CursorOffset.X = args.X - m_WindowRectOrigin.Location.X;
					m_CursorOffset.Y = args.Y - m_WindowRectOrigin.Location.Y;
				}
			}
		}

		void OnMouseDragStartedExt(object sender, MouseEventExtArgs args)
		{
			if (m_DraggingWindowHandle == IntPtr.Zero)
				return;

			m_IsDragging = true;
		}

		void MouseDragFinishedExt(object sender, MouseEventExtArgs args)
		{
			if (!m_IsDragging)
				return;

			m_IsDragging = false;
			m_DraggingWindowHandle = IntPtr.Zero;
		}

		void OnMouseMoveExt(object sender, MouseEventExtArgs args)
		{
			if (m_IsDragging)
			{
				if (SetWindowPos(m_DraggingWindowHandle,
					(int)InsertWindowOrder.Top,
					args.X - m_CursorOffset.X,
					args.Y - m_CursorOffset.Y,
					m_ParentForm.Bounds.Width,
					m_ParentForm.Bounds.Height,
					(uint)(
						SetWindowPositionFlags.NoSize |
						SetWindowPositionFlags.NoZOrder |
						SetWindowPositionFlags.ShowWindow
					)) == IntPtr.Zero)
				{
#if DEBUG
					Console.WriteLine("Failed to set window position: " + GetLastError());
#endif
				}
				else
				{
				}
			}
		}
	}
}
