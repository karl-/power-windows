using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Parabox.WindowMover
{
	struct InputMap
	{	
		Keys m_Keys;
		MouseButtons m_MouseButtons;
		WindowAction m_Action;

		public Keys keys { get { return m_Keys; } }
		public MouseButtons mouseButtons { get { return m_MouseButtons; } }
		public WindowAction action { get { return m_Action; } }

		public InputMap(Keys keys, MouseButtons buttons, WindowAction action)
		{
			m_Keys = keys;
			m_MouseButtons = buttons;
			m_Action = action;
		}
	}
}
