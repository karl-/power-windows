using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WindowsInput.Native;

namespace Parabox.WindowMover
{
	static class KeysUtility
	{
		/// <summary>
		/// Convert a key and modifiers from Forms.Keys to a collection of VirtualKeyCode.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		internal static List<VirtualKeyCode> VirtualKeyFromFormsKey(Keys key)
		{
			List<VirtualKeyCode> virtualKeys = new List<VirtualKeyCode>();

			if (key.HasFlag(Keys.Alt))
				virtualKeys.Add((VirtualKeyCode) Keys.Alt);
			if (key.HasFlag(Keys.Control))
				virtualKeys.Add((VirtualKeyCode) Keys.Control);
			if (key.HasFlag(Keys.Shift))
				virtualKeys.Add((VirtualKeyCode) Keys.Shift);
			//if (key.HasFlag(Keys.Windows))
			//	virtualKeys.Add((VirtualKeyCode)Keys.LWin);

			virtualKeys.Add((VirtualKeyCode)((int)key & 0xFFFF));

			return virtualKeys;
		}

		internal static VirtualKeyCode VirtualKeyFromKeys(Keys keys)
		{
			// remove modifiers, then cast to VirtualKeyCode
			return (VirtualKeyCode)(((int)keys) & 0xFFFF);
		}

		internal static void VirtualKeyAndModifiersFromKey(Keys keyModifiers, Keys keyCode, out List<VirtualKeyCode> modifiers, out List<VirtualKeyCode> keys)
		{
			modifiers = new List<VirtualKeyCode>();

			if (keyModifiers.HasFlag(Keys.Alt))
				modifiers.Add((VirtualKeyCode)Keys.Alt);
			if (keyModifiers.HasFlag(Keys.Control))
				modifiers.Add((VirtualKeyCode)Keys.Control);
			if (keyModifiers.HasFlag(Keys.Shift))
				modifiers.Add((VirtualKeyCode)Keys.Shift);

			keys = new List<VirtualKeyCode>()
			{
				(VirtualKeyCode)((int)keyCode & 0xFFFF)
			};
		}
	}
}
