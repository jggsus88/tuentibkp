using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace tuentibkp
{
    public partial class Browser : Form
    {
        public Browser()
        {
            InitializeComponent();
        }

        public WebBrowser GetBrowser()
        {
            return this.webBrowser1;
        }
    }
}
