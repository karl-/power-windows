//#define DEBUG_KEY_PRESS

using System;
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

		[Flags]
		enum KeyPressState
		{
			Down = 0x1,
			Up = 0x2
		}

		class WindowState
		{
			public IntPtr handle;
			public Point cursorOffset;
			public Rectangle origin;
			public Rectangle position;
			public bool isDragging;

			public WindowState()
			{
				Clear();
			}

			public void Clear()
			{
				handle = IntPtr.Zero;
				cursorOffset = Point.Empty;
				origin = Rectangle.Empty;
				position = Rectangle.Empty;
				isDragging = false;
			}
		}

		WindowState m_WindowState;

		IKeyboardMouseEvents m_GlobalHook;
		IInputSimulator m_InputSimulator;
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

			m_InputMap = new InputMap(Keys.LWin, MouseButtons.Left, WindowAction.Move);
			m_InputSimulator = new InputSimulator();

			m_QueuedKeyInput = Keys.None;
			m_IgnoreNextInput = Keys.None;
			m_DidUseKeyInput = false;

			m_PressedKeys = new HashSet<Keys>();

			m_WindowState = new WindowState();
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
			LogKeyPress("    sending virtual key " + state + " : " + vk);
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
					SendQueuedInput(KeyPressState.Up);
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
				m_WindowState.handle = WindowUtility.WindowFromPoint(args.Location);
				Rectangle windowRect = new Rectangle();

				if(m_WindowState.handle != IntPtr.Zero && WindowUtility.GetWindowRect(m_WindowState.handle, ref windowRect))
				{
					m_DidUseKeyInput = true;
					m_WindowState.origin = windowRect;

					m_WindowState.cursorOffset.X = args.X - m_WindowState.origin.Location.X;
					m_WindowState.cursorOffset.Y = args.Y - m_WindowState.origin.Location.Y;
					m_WindowState.position.Width = m_WindowState.origin.Width;
					m_WindowState.position.Height = m_WindowState.origin.Height;
				}
			}
		}

		void OnMouseDragStartedExt(object sender, MouseEventExtArgs args)
		{
			if (m_WindowState.handle == IntPtr.Zero)
				return;

			m_WindowState.isDragging = true;
		}

		void MouseDragFinishedExt(object sender, MouseEventExtArgs args)
		{
			if (!m_WindowState.isDragging)
				return;

			m_WindowState.isDragging = false;
			m_WindowState.handle = IntPtr.Zero;
		}

		void OnMouseMoveExt(object sender, MouseEventExtArgs args)
		{
			if (m_WindowState.isDragging)
			{
				m_WindowState.position.X = args.X - m_WindowState.cursorOffset.X;
				m_WindowState.position.Y = args.Y - m_WindowState.cursorOffset.Y;

				if (args.X <= 0)
				{
					m_WindowState.position.X = 0;
					m_WindowState.position.Y = 0;
					Rectangle screenBounds = Screen.FromPoint(args.Location).Bounds;
					m_WindowState.position.Width = (int)(screenBounds.Width * .5);
					m_WindowState.position.Height = screenBounds.Height;
				}
				else
					m_WindowState.position.Width = m_WindowState.origin.Width;

				if (args.Y <= 0)
				{
					m_WindowState.position.X = 0;
					m_WindowState.position.Y = 0;
					Rectangle screenBounds = Screen.FromPoint(args.Location).Bounds;
					m_WindowState.position.Width = screenBounds.Width;
					m_WindowState.position.Height = screenBounds.Height;
				}
				else
					m_WindowState.position.Height = m_WindowState.origin.Height;

				if(!WindowUtility.SetWindowPos(m_WindowState.handle, m_WindowState.position, SetWindowPositionFlags.NoZOrder | SetWindowPositionFlags.ShowWindow))
				{
#if DEBUG
					Console.WriteLine("Failed to set window position: " + WindowUtility.GetLastError());
#endif
				}
				else
				{
				}
			}
		}
	}
}
