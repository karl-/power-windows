using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Parabox.WindowMover
{
	public partial class WindowMoverForm : Form
	{
		NotifyIcon m_NotifyIcon;
		WindowManager m_WindowManager;
		ContextMenu m_ContextMenu;
		MenuItem m_ExitAppMenuItem;

		public WindowMoverForm()
		{
			m_Components = new Container();
			m_ContextMenu = new ContextMenu();
			m_ExitAppMenuItem = new MenuItem();

			m_ExitAppMenuItem.Text = "Exit";
			m_ExitAppMenuItem.Click += new System.EventHandler(this.m_ExitMenuItem_Click);
			m_ContextMenu.MenuItems.Add(m_ExitAppMenuItem);

			m_NotifyIcon = new NotifyIcon(m_Components);
			m_NotifyIcon.Text = "Parabox Window Mover";
			m_NotifyIcon.Icon = new Icon("Icon1.ico");
			m_NotifyIcon.Visible = true;
			m_NotifyIcon.ContextMenu = m_ContextMenu;
			m_NotifyIcon.DoubleClick += new EventHandler(OnNotifyIconDoubleClicked);

			m_WindowManager = new WindowManager();

			InitializeComponent();
		}

		void WindowMoverForm_Load(object sender, EventArgs e)
		{
		}
		
		void m_ExitMenuItem_Click(object sender, EventArgs args)
		{
			Close();
		}

		void OnNotifyIconDoubleClicked(object sender, EventArgs args)
		{
			if (WindowState == FormWindowState.Minimized)
			{
				WindowState = FormWindowState.Normal;
				Activate();
			}
			else if (WindowState == FormWindowState.Normal || WindowState == FormWindowState.Maximized)
			{
				WindowState = FormWindowState.Minimized;
			}
		}
	}
}
