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
    public partial class MainForm : Form
    {
        public OpenFileDialog levelOld;
        public OpenFileDialog levelNew;
        public OpenFileDialog schematic;


        public Button lOButton;
        public Button lNButton;
        public Button scButton;
        public Button saveButton;

        public static NbtFile levelOldFile;
        public static bool lofIsLoaded = false;
        public static bool lnfIsLoaded = false;
        public static bool schemIsLoaded = false;
        public static NbtFile levelNewFile;
        public static NbtFile schemFile;

        public static Dictionary<int, string> oldIds = new Dictionary<int, string>();
        public static Dictionary<string, int> newIds = new Dictionary<string, int>();

        public static NbtFile migratedSchem;

        public static string newShemName;


        public MainForm()
        {
            InitializeComponent();
            Console.WriteLine(newShemName);
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

            this.Controls.Add(lOButton);
            this.Controls.Add(lNButton);
            this.Controls.Add(scButton);
            this.Controls.Add(saveButton);

            this.Size = new Size(320, 280);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            this.Text = "Shematic Migrate App";

            saveButton.Click += new EventHandler(migrate);
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
            bool iDo = false;
            switch (((Control)sender).Name)
            {
                case "o":
                    levelOldFile = ReadFile(levelOld, ref iDo);
                    if(iDo) loadLevelDat(false, "Load old level file");
                    break;
                case "n":
                    levelNewFile = ReadFile(levelNew, ref iDo);
                    if (iDo) loadLevelDat(true, "Load new level file");
                    break;
                case "s":
                    schemFile = ReadFile(schematic, ref iDo);
                    if (iDo) schemIsLoaded = true;
                    break;
                default:
                    MessageBox.Show("Error");
                    break;
            }
        }

        public NbtFile ReadFile(OpenFileDialog opf, ref bool iDo)
        {
            if (opf.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    NbtFile nbtFile = new NbtFile();
                    if (!opf.FileName.Contains(".dat") && !opf.FileName.Contains(".schem"))
                    {
                        MessageBox.Show("Все плохо, выбери другой");
                        return nbtFile;
                    }
                    nbtFile.LoadFromStream(opf.OpenFile(), NbtCompression.AutoDetect);
                    iDo = true;
                    return nbtFile;
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
            return new NbtFile();
        }

        public void loadLevelDat(bool isNew, string title)
        {
            var tmp = isNew ? levelNewFile : levelOldFile;

            if (tmp.RootTag.Get<NbtCompound>("FML") == null) {
                MessageBox.Show("Файл не коректен, попробуйте другой"); 
                return; 
            }

            NbtList rawNbt = tmp.RootTag.Get<NbtCompound>("FML")
                            .Get<NbtCompound>("Registries")
                            .Get<NbtCompound>("minecraft:blocks")
                            .Get<NbtList>("ids");
            
            if (rawNbt == null) {
                MessageBox.Show("Похоже вы загрузили не верный файл, попробуйте другой"); 
            }

            var nbt = rawNbt.ToArray().ToList<NbtTag>();
            
            if (!isNew) oldIds.Clear();
            else newIds.Clear();

            Task.Run(() => {
                if (isNew) lnfIsLoaded = false;
                else lofIsLoaded = false;
                LoadFileForm loadForm = new LoadFileForm(nbt.Count, title);
                loadForm.Show();

                nbt.ForEach(delegate (NbtTag tag) {
                    if (tag is NbtCompound ntag)
                    {
                        int id = ntag.Get<NbtInt>("V").IntValue;
                        string name = ntag.Get<NbtString>("K").StringValue;
                        Console.WriteLine(String.Format("{0}  {1}", id, name));
                        if (!isNew) oldIds.Add(id, name);
                        else newIds.Add(name, id);
                        loadForm.UdpateProgressBar();

                    }
                });

                loadForm.Close();

                if (isNew) lnfIsLoaded = true;
                else lofIsLoaded = true;
            });
        }

        public void migrate(object sender, EventArgs e)
        {
            if (!lofIsLoaded) { MessageBox.Show("Изначальный level.dat не загружен"); return; }
            if (!lnfIsLoaded) { MessageBox.Show("Конечный level.dat не загружен"); return; }
            if (!schemIsLoaded) { MessageBox.Show("Схематик не загружен"); return; }
            newShemName = "sch" + DateTime.Now.Ticks + ".schematic";
            new NameSchemForm().Show();
        }

        public static void saveFile()
        {
            NbtFile toSave = createSchemFile();
            toSave.SaveToFile(newShemName, NbtCompression.GZip);
        }

        public static NbtFile createSchemFile()
        {
            var nbt = schemFile.RootTag;
            Console.WriteLine(nbt);

            var usedBlocks = getUsedBlockStringId(nbt);


            byte[] addBlocks;
            byte[] blocks = convertToOtherLevelDat(usedBlocks, out addBlocks);

            NbtCompound nbtToSave = new NbtCompound("Schematic")
            {
                new NbtByteArray("Blocks", blocks),
                new NbtString(nbt.Get<NbtString>("Materials")),
                new NbtByteArray(nbt.Get<NbtByteArray>("Data")),
                new NbtByteArray("AddBlocks", addBlocks),
                new NbtList(nbt.Get<NbtList>("TileEntities")),
                new NbtList(nbt.Get<NbtList>("Entities")),
                new NbtShort(nbt.Get<NbtShort>("Length")),
                new NbtInt(nbt.Get<NbtInt>("WEOffsetX")),
                new NbtInt(nbt.Get<NbtInt>("WEOffsetY")),
                new NbtInt(nbt.Get<NbtInt>("WEOriginZ")),
                new NbtInt(nbt.Get<NbtInt>("WEOffsetZ")),
                new NbtShort(nbt.Get<NbtShort>("Height")),
                new NbtInt(nbt.Get<NbtInt>("WEOriginY")),
                new NbtInt(nbt.Get<NbtInt>("WEOriginX")),
                new NbtShort(nbt.Get<NbtShort>("Width"))
            };

            return new NbtFile(nbtToSave);
        }

        public static List<int> getUsedBlockStringId(NbtCompound nbt)
        {
            var blocks = nbt.Get<NbtByteArray>("Blocks");
            var addBlocks = nbt.Get<NbtByteArray>("AddBlocks");

            byte[] blockId = blocks.Value;
            byte[] addId = addBlocks.Value;

            List<string> usedBlocks = new List<string>();

            for (int index = 0; index < blockId.Length; index++)
            {
                if ((index >> 1) >= addId.Length)
                {
                    var i = (short)(blockId[index] & 0xFF);
                    usedBlocks.Add(oldIds[i]);
                }
                else
                {
                    if ((index & 1) == 0)
                    {
                        var i = (short)(((addId[index >> 1] & 0x0F) << 8) + (blockId[index] & 0xFF));
                        usedBlocks.Add(oldIds[i]);
                    }
                    else
                    {
                        var i = (short)(((addId[index >> 1] & 0xF0) << 4) + (blockId[index] & 0xFF));
                        usedBlocks.Add(oldIds[i]);
                    }
                }
            }

            List<int> usedBlocksIds = new List<int>();
            foreach(string a in usedBlocks)
            {
                try { usedBlocksIds.Add(newIds[a]); }
                catch { usedBlocksIds.Add(0); }
            }
            return usedBlocksIds;
        }

        public static byte[] convertToOtherLevelDat(List<int> list, out byte[] addBlocks)
        {
            int i = 0;
            byte[] blocks = new byte[list.Count];
            addBlocks = new byte[(blocks.Length >> 1) + 1];
            foreach (int block in list)
            {
                if (block > 255)
                {
                    addBlocks[i >> 1] = (byte)(((i & 1) == 0) ?
                            addBlocks[i >> 1] & 0xF0 | (block >> 8) & 0xF
                            : addBlocks[i >> 1] & 0xF | ((block >> 8) & 0xF) << 4);
                }

                blocks[i] = (byte) block;
                i++;
            }

            return blocks;
        }
    }
}
