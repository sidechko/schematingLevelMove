using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace schematingLevelMove
{
    public partial class LoadFileForm : Form
    {

        public ProgressBar progressBar;

        public static Dispatcher thisDispather = Dispatcher.CurrentDispatcher;
        public LoadFileForm(int maximumProgress, string title)
        {
            InitializeComponent();

            this.Text = title;

            progressBar = new ProgressBar()
            {
                Location = new Point(15, 15),
                Size = new Size(500, 30),
                Visible = true,

                Value = 0,
                Maximum = maximumProgress
            };

            this.Controls.Add(progressBar);

            this.Size = new Size(550, 100);
        }

        public void UdpateProgressBar()
        {
            progressBar.Refresh();
            if (progressBar.Value != progressBar.Maximum) progressBar.Value++;
            progressBar.CreateGraphics().DrawString(
                String.Format("{0}/{1}", progressBar.Value, progressBar.Maximum),
                new Font("Arial", (float)7.15, FontStyle.Regular),
                Brushes.Black,
                new PointF(progressBar.Width / 2 - 20, progressBar.Height / 2 - 7)
            );
        }
    }
}
