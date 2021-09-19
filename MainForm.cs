﻿using System;
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
        public static NbtFile levelNewFile;
        public static NbtFile schemFile;

        public static Dictionary<int, string> oldIds = new Dictionary<int, string>();
        public static Dictionary<string, int> newIds = new Dictionary<string, int>();

        public static NbtFile migratedSchem;

        public string newShemName;


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
                    if (opf.FileName.Contains(".schem"))
                    {
                        
                    }
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
            NbtList rawNbt = new NbtList();
            rawNbt = tmp.RootTag
                            .Get<NbtCompound>("FML")
                            .Get<NbtCompound>("Registries")
                            .Get<NbtCompound>("minecraft:blocks")
                            .Get<NbtList>("ids");
            //MessageBox.Show("Похоже вы загрузили не верный файл, попробуйте другой");

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
                int i = 0;
                nbt.ForEach(delegate (NbtTag tag) {
                    if (tag is NbtCompound ntag)
                    {
                        int id = ntag.Get<NbtInt>("V").IntValue;
                        string name = ntag.Get<NbtString>("K").StringValue;
                        Console.WriteLine(String.Format("{0}  {1}", id, name));
                        if (!isNew) oldIds.Add(id, name);
                        else newIds.Add(name, id);
                        i++;
                        if (i % 5 == 0)
                        {
                            Thread.Sleep(1);
                            i = 0;
                        }
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
            if (!lnfIsLoaded) { MessageBox.Show("Переходной level.dat не загружен"); return; }

            //if (schemFile == null) { MessageBox.Show("Схематик не загружен"); return; }

            toSave.SaveToFile(newShemName, NbtCompression.None);
        }

        public NbtFile createSchemFile()
        {
            var nbt = schemFile.RootTag;

            var usedBlocks = getUsedBlockStringId(nbt);


            byte[] addBlocks;
            byte[] blocks = convertToOtherLevelDat(usedBlocks, out addBlocks);

            NbtCompound nbtToSave = new NbtCompound("Schematic")
            {
                new NbtByteArray("Blocks", blocks),
                new NbtString("Materials",nbt.Get<NbtString>("Materials").Value),
                new NbtByteArray("AddBlocks", addBlocks),
                new NbtByteArray("Data",nbt.Get<NbtByteArray>("Data").Value),
                new NbtShort("Length",nbt.Get<NbtShort>("Length").Value),
                new NbtInt("WEOffsetX",nbt.Get<NbtInt>("WEOffsetX").Value),
                new NbtInt("WEOffsetY",nbt.Get<NbtInt>("WEOffsetY").Value),
                new NbtInt("WEOriginZ",nbt.Get<NbtInt>("WEOriginZ").Value),
                new NbtInt("WEOffsetZ",nbt.Get<NbtInt>("WEOffsetZ").Value),
                new NbtShort("Height",nbt.Get<NbtShort>("Height").Value),
                new NbtInt("WEOriginY",nbt.Get<NbtInt>("WEOriginY").Value),
                new NbtInt("WEOriginX",nbt.Get<NbtInt>("WEOriginX").Value),
                new NbtShort("Width",nbt.Get<NbtShort>("Width").Value)
            };

            return new NbtFile(nbtToSave);
        }

        public List<int> getUsedBlockStringId(NbtCompound nbt)
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
                usedBlocksIds.Add(newIds[a]);
            }
            return usedBlocksIds;
        }

        public byte[] convertToOtherLevelDat(List<int> list, out byte[] addBlocks)
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
