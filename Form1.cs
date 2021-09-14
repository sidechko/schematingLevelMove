using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using fNbt;
using System.Windows.Threading;
using System.Threading;

namespace schematingLevelMove
{
    public partial class Form1 : Form
    {
        public OpenFileDialog levelOld;
        public OpenFileDialog levelNew;
        public OpenFileDialog schematic;

        public Button lOButton;
        public Button lNButton;
        public Button scButton;
        public Button saveButton;

        public RichTextBox insideJson;

        public ProgressBar progressBar;

        public static NbtFile levelOldFile;
        public static NbtFile levelNewFile;
        public static NbtFile schemFile;

        public static Dictionary<int, string> oldIds = new Dictionary<int, string>();
        public static Dictionary<string, int> newIds = new Dictionary<string, int>();

        public static Dispatcher thisDispather = Dispatcher.CurrentDispatcher;

        public Form1()
        {
            InitializeComponent();

            levelNew = new OpenFileDialog();
            levelOld = new OpenFileDialog();
            schematic = new OpenFileDialog();

            lOButton = CustomButton("Select level.dat old", 'o', 20);
            lNButton = CustomButton("Select level.dat new", 'n', 60);
            scButton = CustomButton("Select schematic", 's', 100);
            saveButton = new Button()
            {
                Location = new Point(50, 160),
                Size = new Size(200, 30),
                Visible = true,

                Text = "Migrate",
                Name = "save",

                FlatStyle = FlatStyle.Flat
            };

            insideJson = new RichTextBox()
            {
                Location = new Point(300,60),
                Size = new Size(500, 700),
                Visible = true,

                BorderStyle = BorderStyle.None
            };

            progressBar = new ProgressBar()
            {
                Location = new Point(50, 250),
                Size = new Size(200, 30),
                Visible = true,

                Value = 0,
                Maximum = 1
            };

            this.Controls.Add(lOButton);
            this.Controls.Add(lNButton);
            this.Controls.Add(scButton);
            this.Controls.Add(saveButton);

            this.Controls.Add(insideJson);

            this.Controls.Add(progressBar);

            this.Size = new Size(860, 900);
        }

        public Button CustomButton(string text, char name, int y)
        {
            var b = new Button()
            {
                Location = new Point(50, y),
                Size = new Size(200, 30),
                Visible = true,

                Text = text,
                Name = name.ToString(),

                FlatStyle = FlatStyle.Flat
            };
            b.Click += new EventHandler(SelectLevelDat);
            return b;
        }

        public void SelectLevelDat(object sender, EventArgs e)
        {
            switch (((Control)sender).Name)
            {
                case "o":
                    levelOldFile = ReadFile(levelOld);
                    break;
                case "n":
                    levelNewFile = ReadFile(levelNew, true);
                    break;
                case "s":
                    schemFile = ReadFile(schematic);
                    break;
                default:
                    MessageBox.Show("Error");
                    break;
            }
        }

        public NbtFile ReadFile(OpenFileDialog opf, bool isNew = false)
        {
            if (opf.ShowDialog() == DialogResult.OK)
            {
                
                try
                {
                    insideJson.ResetText();

                    NbtFile nbtFile = new NbtFile();
                    if (!opf.FileName.Contains(".dat") && !opf.FileName.Contains(".schematic"))
                    {
                        MessageBox.Show("Все плохо, выбери другой");
                        return nbtFile;
                    }
                    nbtFile.LoadFromStream(opf.OpenFile(), NbtCompression.AutoDetect);

                    var nbt = nbtFile.RootTag
                        .Get<NbtCompound>("FML")
                        .Get<NbtCompound>("Registries")
                        .Get<NbtCompound>("minecraft:blocks")
                        .Get<NbtList>("ids")
                    .ToArray().ToList<NbtTag>();

                    progressBar.Refresh();
                    progressBar.Maximum = nbt.Count;

                    Task.Run(()=>{
                        int i = 0;
                        nbt.ForEach(delegate (NbtTag tag) {
                            if (tag is NbtCompound ntag)
                            {
                                int id = ntag.Get<NbtInt>("V").IntValue;
                                string name = ntag.Get<NbtString>("K").StringValue;

                                if (!isNew) oldIds.Add(id, name);
                                else newIds.Add(name, id);

                                thisDispather.Invoke(() => {
                                    insideJson.Text += String.Format("{0} : {1}\n", id, name);
                                    UdpateProgressBar();
                                });
                            }
                            i++;
                            if (i % 32 == 0) Thread.Sleep(2);
                        });
                        thisDispather.Invoke(() => { 
                            progressBar.Value = 0;
                            progressBar.Refresh();
                        });
                    });
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
            return new NbtFile();
        }
        public void UdpateProgressBar()
        {
            progressBar.Refresh();
            progressBar.Value++;
            progressBar.CreateGraphics().DrawString(
                String.Format("{0}/{1}", progressBar.Value, progressBar.Maximum), 
                new Font("Arial", (float)7.15, FontStyle.Regular), 
                Brushes.Black, 
                new PointF(progressBar.Width / 2 - 20, progressBar.Height / 2 - 7)
            );
        }
    }
}
