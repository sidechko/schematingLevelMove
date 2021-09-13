using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace schematingLevelMove
{
    public partial class Form1 : Form
    {
        private OpenFileDialog levelOld;
        private OpenFileDialog levelNew;
        private OpenFileDialog schematic;

        private Button lOButton;
        private Button lNButton;
        private Button scButton;

        public Form1()
        {
            InitializeComponent();

            levelNew = new OpenFileDialog();
            levelOld = new OpenFileDialog();
            schematic = new OpenFileDialog();

            lOButton = customButton("Select level.dat old", 'o', 20);
            lNButton = customButton("Select level.dat new", 'l', 50);
            scButton = customButton("Select schematic", 's', 80);

            this.Controls.Add(lOButton);
            this.Controls.Add(lNButton);
            this.Controls.Add(scButton);
        }

        public Button customButton(string text, char name, int y)
        {
            var b = new Button()
            {
                Location = new Point(50, y),
                Size = new Size(200, 20),
                Text = text,
                Name = name.ToString(),
                Visible = true
            };
            b.Click += new EventHandler(selectLevelDat);
            return b;
        }

        private void selectLevelDat(object sender, EventArgs e)
        {
            if (((OpenFileDialog)sender).ShowDialog() == DialogResult.OK)
            {
                try
                {
                    BinaryReader reader = new BinaryReader(((OpenFileDialog)sender).OpenFile());
                    switch (((Control)sender).Name)
                    {
                        case "o":
                            break;
                        case "l":
                            break;
                        case "s":
                            break;
                    }
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }
    }
}
