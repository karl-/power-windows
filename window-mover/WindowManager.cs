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
		HashSet<Keys> m_QueuedKeyInput;
		HashSet<Keys> m_UsedKeyInput;
		HashSet<Keys> m_IgnoreNextInput;
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

			m_InputMap = new InputMap(Keys.LWin, MouseButtons.Left, WindowAction.Move);
			m_InputSimulator = new InputSimulator();

			m_QueuedKeyInput = new HashSet<Keys>();
			m_UsedKeyInput = new HashSet<Keys>();
			m_IgnoreNextInput = new HashSet<Keys>();

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

		void OnKeyDown(object sender, KeyEventArgs args)
		{
			m_PressedKeys.Add(args.KeyCode);

			if (m_IgnoreNextInput.Contains(args.KeyData))
			{
				Console.WriteLine("Ignore Simulated: " + args.KeyData + " [" + args.KeyCode + ", " + args.Modifiers + "]");
				Console.WriteLine("    Passed: " + args.KeyCode);
				m_IgnoreNextInput.Remove(args.KeyData);
				return;
			}

			Console.WriteLine("OnKeyDown: " + args.KeyData + " [" + args.KeyCode + ", " + args.Modifiers + "]");
			
			if (args.KeyData == m_InputMap.keys)
			{
				Console.WriteLine("    Supressed: " + args.KeyData);
				args.SuppressKeyPress = true;
				args.Handled = true;
				m_QueuedKeyInput.Add(args.KeyData);
			}
			else
			{
				Console.WriteLine("    Passed: " + args.KeyData);
			}
		}

		void OnKeyUp(object sender, KeyEventArgs args)
		{
			Console.WriteLine("OnKeyUp: " + args.KeyData + " [" + args.KeyCode + ", " + args.Modifiers + "]");

			m_PressedKeys.Remove(args.KeyCode);

			if (m_QueuedKeyInput.Contains(args.KeyData))
			{
				Console.WriteLine("    Suppressed: " + args.KeyData);
				args.Handled = true;
				args.SuppressKeyPress = true;

				// If the shortcut was used, discard the key event
				if (m_UsedKeyInput.Contains(args.KeyData))
				{
					m_QueuedKeyInput.Remove(args.KeyData);
					m_UsedKeyInput.Remove(args.KeyData);
					Console.WriteLine("    was ignored: " + args.KeyData);
				}
				// if it was not used, simulate it
				else
				{
					m_IgnoreNextInput.Add(args.KeyData);
					m_QueuedKeyInput.Remove(args.KeyData);

					if (args.Modifiers > 0)
					{
						List<VirtualKeyCode> modifiers, keys;
						KeysUtility.VirtualKeyAndModifiersFromKey(args.Modifiers, args.KeyCode, out modifiers, out keys);
						Console.WriteLine("    sending virtual keys: [" + string.Join(",", modifiers.ToArray()) + "] " + string.Join(",", keys.ToArray()));
						m_InputSimulator.Keyboard.ModifiedKeyStroke(modifiers, keys);
						Console.WriteLine("    sent virtual keys: [" + string.Join(",", modifiers.ToArray()) + "] " + string.Join(",", keys.ToArray()));
					}
					else
					{
						VirtualKeyCode vk = KeysUtility.VirtualKeyFromKeys(args.KeyCode);
						Console.WriteLine("    sending virtual key: " + vk);
						m_InputSimulator.Keyboard.KeyDown(vk);
						m_InputSimulator.Keyboard.KeyUp(vk);
						Console.WriteLine("    sent virtual key: " + vk);
					}
				}
			}
			else
			{
				Console.WriteLine("    Passed: " + args.KeyData);
			}
		}

		void OnMouseDownExt(object sender, MouseEventExtArgs args)
		{
			Console.WriteLine("buttons:");
			foreach (var v in m_PressedKeys)
				Console.WriteLine(v.ToString());

			if (args.Button == m_InputMap.mouseButtons && m_PressedKeys.Count == 1 && m_PressedKeys.First().Equals(m_InputMap.keys))
			{
				m_DraggingWindowHandle = WindowFromPoint(args.Location);
				Rect windowRect = new Rect();

				if(m_DraggingWindowHandle != IntPtr.Zero && GetWindowRect(m_DraggingWindowHandle, ref windowRect))
				{
					m_UsedKeyInput.Add(m_InputMap.keys);
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
