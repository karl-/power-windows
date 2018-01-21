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
		public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, uint wFlags);

		[DllImport("user32.dll", EntryPoint = "WindowFromPoint")]
		public static extern IntPtr WindowFromPoint(Point point);

		IKeyboardMouseEvents m_GlobalHook;
		Form m_ParentForm;

		public WindowManager(Form parent)
		{
			m_GlobalHook = Hook.GlobalEvents();
			m_GlobalHook.MouseDownExt += MouseDownExt;
			m_ParentForm = parent;
		}

		~WindowManager()
		{
			m_GlobalHook.MouseDownExt -= MouseDownExt;
			m_GlobalHook.Dispose();
		}

		void MouseDownExt(object sender, MouseEventExtArgs e)
		{
			Console.WriteLine("Mouse down: " + e.Button);

			if (e.Button == MouseButtons.Left)
			{
				IntPtr hovered = WindowFromPoint(e.Location);

				if(hovered == m_ParentForm.Handle)
					SetWindowPos(m_ParentForm.Handle,
						0,
						0,
						0,
						m_ParentForm.Bounds.Width,
						m_ParentForm.Bounds.Height,
						(uint)(SetWindowPositionFlags.NoSize | SetWindowPositionFlags.NoZOrder));
			}
		}

	}
}
