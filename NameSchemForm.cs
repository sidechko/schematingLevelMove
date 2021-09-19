using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace schematingLevelMove
{
    public partial class NameSchemForm : Form
    {
        public TextBox nameOfSchematic;
        public Button saveName;
        public NameSchemForm()
        {
            InitializeComponent();

            nameOfSchematic = new TextBox()
            {
                Size = new Size(200, 50),
                Location = new Point(15,15),
                Visible = true
            };

            saveName = new Button()
            {
                Size = new Size(200, 50),
                Location = new Point(15, 15),
                Visible = true,
                Text = "Save"
            };

            this.Controls.Add(nameOfSchematic);
            this.Controls.Add(saveName);

            saveName.Click += new EventHandler(getName);

            this.Text = "Select schematic name";

            this.Size = new Size(300, 160);
        }

        public void getName(object sender, EventArgs eventArgs)
        {
            this.Close();
        }
    }
}
