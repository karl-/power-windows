using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using System.Drawing;
using System.Collections.Generic;
using WindowsInput;
using WindowsInput.Native;
using System.Windows.Input;
using System.Linq;

namespace Parabox.WindowMover
{
	class WindowManager
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

		[Flags]
		enum KeyPressState
		{
			Down = 0x1,
			Up = 0x2
		}

		[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
		static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint wFlags);

		[DllImport("user32.dll", EntryPoint = "GetWindowRect")]
		static extern bool GetWindowRect(IntPtr hWnd, ref Rect rect);

		[DllImport("user32.dll", EntryPoint = "WindowFromPoint")]
		static extern IntPtr WindowFromPoint(Point point);

		[DllImport("Kernel32.dll", EntryPoint = "GetLastError")]
		static extern uint GetLastError();
						
		IKeyboardMouseEvents m_GlobalHook;
		bool m_IsDragging;
		IntPtr m_DraggingWindowHandle;
		Point m_CursorOffset;
		Rectangle m_WindowRectOrigin;
		IInputSimulator m_InputSimulator;
		Rectangle m_WindowRectCurrent;

		InputMap m_InputMap;

		Keys m_QueuedKeyInput;
		Keys m_IgnoreNextInput;
		bool m_DidUseKeyInput;

		HashSet<Keys> m_PressedKeys;

		public WindowManager()
		{
			m_GlobalHook = Hook.GlobalEvents();

			m_GlobalHook.MouseDownExt += OnMouseDownExt;
			m_GlobalHook.MouseDragStartedExt += OnMouseDragStartedExt;
			m_GlobalHook.MouseDragFinishedExt += MouseDragFinishedExt;
			m_GlobalHook.MouseMoveExt += OnMouseMoveExt;

			m_GlobalHook.KeyDown += OnKeyDown;
			m_GlobalHook.KeyUp+= OnKeyUp;

			m_InputMap = new InputMap(Keys.LMenu, MouseButtons.Left, WindowAction.Move);
			m_InputSimulator = new InputSimulator();

			m_QueuedKeyInput = Keys.None;
			m_IgnoreNextInput = Keys.None;
			m_DidUseKeyInput = false;

			m_PressedKeys = new HashSet<Keys>();
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

		[System.Diagnostics.Conditional("DEBUG_KEY_PRESS")]
		static void LogKeyPress(string txt)
		{
			Console.WriteLine(txt);
		}

		void OnKeyDown(object sender, KeyEventArgs args)
		{
			bool firstPress = m_PressedKeys.Add(args.KeyCode);

			if (m_IgnoreNextInput == args.KeyData)
			{
				if (firstPress)
				{
					LogKeyPress("Ignore Simulated: " + args.KeyData + " [" + args.KeyCode + ", " + args.Modifiers + "]");
					LogKeyPress("    Passed: " + args.KeyCode);
				}

				m_IgnoreNextInput = Keys.None;

				return;
			}

			if (firstPress)
				LogKeyPress("OnKeyDown: " + args.KeyData + " [" + args.KeyCode + ", " + args.Modifiers + "]");
			
			if (args.KeyData == m_InputMap.keys)
			{
				if (firstPress)
					LogKeyPress("    Supressed: " + args.KeyData);
				args.SuppressKeyPress = true;
				args.Handled = true;
				m_QueuedKeyInput = m_InputMap.keys;
			}
			else
			{
				if(m_QueuedKeyInput != Keys.None)
					SendQueuedInput(KeyPressState.Down);

				if (firstPress)
					LogKeyPress("    Passed: " + args.KeyData);
			}
		}

		void SendQueuedInput(KeyPressState state)
		{
			if (m_QueuedKeyInput == Keys.None)
				return;

			//if (m_QueuedKeyInput.Modifiers > 0)
			//{
			//	List<VirtualKeyCode> modifiers, keys;
			//	KeysUtility.VirtualKeyAndModifiersFromKey(args.Modifiers, args.KeyCode, out modifiers, out keys);
			//	LogKeyPress("    sending virtual keys: [" + string.Join(",", modifiers.ToArray()) + "] " + string.Join(",", keys.ToArray()));
			//	m_InputSimulator.Keyboard.ModifiedKeyStroke(modifiers, keys);
			//	Console.WriteLine("    sent virtual keys: [" + string.Join(",", modifiers.ToArray()) + "] " + string.Join(",", keys.ToArray()));
			//}
			//else
			//{
			VirtualKeyCode vk = KeysUtility.VirtualKeyFromKeys(m_QueuedKeyInput);
			m_IgnoreNextInput = m_QueuedKeyInput;
			m_QueuedKeyInput = Keys.None;
			LogKeyPress("    sending virtual key: " + vk);
			if((state & KeyPressState.Down) > 0)
				m_InputSimulator.Keyboard.KeyDown(vk);
			if((state & KeyPressState.Up) > 0)
				m_InputSimulator.Keyboard.KeyUp(vk);
			LogKeyPress("    sent virtual key: " + vk);
			//}
		}

		void OnKeyUp(object sender, KeyEventArgs args)
		{
			LogKeyPress("OnKeyUp: " + args.KeyData + " [" + args.KeyCode + ", " + args.Modifiers + "]");

			m_PressedKeys.Remove(args.KeyCode);

			if (m_QueuedKeyInput == args.KeyCode)
			{
				LogKeyPress("    Suppressed: " + args.KeyData);
				args.Handled = true;
				args.SuppressKeyPress = true;

				// If the shortcut was used, discard the key event
				if (m_DidUseKeyInput)
				{
					LogKeyPress("    was ignored: " + args.KeyData);
				}
				// if it was not used, simulate it
				else
				{
					SendQueuedInput(KeyPressState.Down | KeyPressState.Up);
				}

				m_QueuedKeyInput = Keys.None;
				m_DidUseKeyInput = false;
			}
			else
			{
				LogKeyPress("    Passed: " + args.KeyData);
			}
		}

		void OnMouseDownExt(object sender, MouseEventExtArgs args)
		{
			LogKeyPress("OnMouseDown (keys):");

			foreach (var v in m_PressedKeys)
				LogKeyPress("    " + v.ToString());

			if (args.Button == m_InputMap.mouseButtons && m_PressedKeys.Count == 1 && m_PressedKeys.First().Equals(m_InputMap.keys))
			{
				m_DraggingWindowHandle = WindowFromPoint(args.Location);
				Rect windowRect = new Rect();

				if(m_DraggingWindowHandle != IntPtr.Zero && GetWindowRect(m_DraggingWindowHandle, ref windowRect))
				{
					m_DidUseKeyInput = true;
					m_WindowRectOrigin = (Rectangle) windowRect;

					m_CursorOffset.X = args.X - m_WindowRectOrigin.Location.X;
					m_CursorOffset.Y = args.Y - m_WindowRectOrigin.Location.Y;
					m_WindowRectCurrent.Width = m_WindowRectOrigin.Width;
					m_WindowRectCurrent.Height = m_WindowRectOrigin.Height;
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
				m_WindowRectCurrent.X = args.X - m_CursorOffset.X;
				m_WindowRectCurrent.Y = args.Y - m_CursorOffset.Y;

				if (args.X <= 0)
				{
					m_WindowRectCurrent.X = 0;
					m_WindowRectCurrent.Y = 0;
					Rectangle screenBounds = Screen.FromPoint(args.Location).Bounds;
					m_WindowRectCurrent.Width = (int)(screenBounds.Width * .5);
					m_WindowRectCurrent.Height = screenBounds.Height;
				}
				else
					m_WindowRectCurrent.Width = m_WindowRectOrigin.Width;

				if (args.Y <= 0)
				{
					m_WindowRectCurrent.X = 0;
					m_WindowRectCurrent.Y = 0;
					Rectangle screenBounds = Screen.FromPoint(args.Location).Bounds;
					m_WindowRectCurrent.Width = screenBounds.Width;
					m_WindowRectCurrent.Height = screenBounds.Height;
				}
				else
					m_WindowRectCurrent.Height = m_WindowRectOrigin.Height;
					
				if (SetWindowPos(m_DraggingWindowHandle,
					(int) InsertWindowOrder.Top,
					m_WindowRectCurrent.X,
					m_WindowRectCurrent.Y,
					m_WindowRectCurrent.Width,
					m_WindowRectCurrent.Height,
					(uint)(
						//SetWindowPositionFlags.NoSize |
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
