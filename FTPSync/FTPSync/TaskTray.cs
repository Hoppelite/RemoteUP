﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FTPSync
{
    class TaskTray : ApplicationContext
    {
        private NotifyIcon nIcon;
        public TaskTray(Icon icon)
        {
            nIcon = new NotifyIcon();
            nIcon.Icon = icon;
            MenuItem closeMenu = new MenuItem("Exit");
            closeMenu.Click += CloseMenu_Click;
            MenuItem[] items = new MenuItem[]
            {
                closeMenu
            };
            nIcon.ContextMenu = new ContextMenu(items);
            nIcon.Visible = true;
        }


        private void CloseMenu_Click(object sender, EventArgs e)
        {
            nIcon.Visible = false;
            Application.Exit();
        }
    }
}
