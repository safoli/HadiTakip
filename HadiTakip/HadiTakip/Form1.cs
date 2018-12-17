using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HadiTakip
{
    public partial class Form1 : Form
    {
        private const string Tracker = "hadibilgiyarismasii";
        DataTable Source;
        Timer tmr;
        string FirstImageLink;

        public Form1()
        {
            InitializeComponent();
        }

        private void SetCaption()
        {
            this.Text = $"{Tracker}, {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StoryLoad();
            tmr = new Timer();
            tmr.Interval = 30 * 1000;
            tmr.Tick += Tmr_Tick;
            tmr.Start();
        }

        private void Tmr_Tick(object sender, EventArgs e)
        {
            StoryLoad();
        }

        void StoryLoad()
        {
            try
            {
                Source = new DataTable();
                Source.Columns.Add("link", typeof(byte[]));

                string content;
                string firstLink = "";

                using (var client = new WebClient())
                {
                    var user = Tracker;
                    content = client.DownloadString($"http://www.instagram.com/{user}/");

                    var storyLinksRaw = Regex.Matches(content, @"""display_url"":""([0-9A-Za-z:\/\-\._\?\=])*""");

                    foreach (Match storyLinkRaw in storyLinksRaw)
                    {
                        var storyLink = storyLinkRaw.Value.Replace(@"""display_url"":""", "").Replace(@"""", "");

                        if (string.IsNullOrEmpty(firstLink))
                            firstLink = storyLink;

                        var bin = client.DownloadData(storyLink);
                        Source.Rows.Add(bin);
                    }
                }

                dataGridView1.DataSource = Source;

                if (!this.dataGridView1.Columns.Contains("Image"))
                {
                    DataGridViewImageColumn img = new DataGridViewImageColumn();
                    img.Name = "Image";

                    this.dataGridView1.Columns.Add(img);
                    this.dataGridView1.CellFormatting += new DataGridViewCellFormattingEventHandler(dataGridView1_CellFormatting);
                }

                this.dataGridView1.Columns["link"].Visible = false;
                this.dataGridView1.AutoResizeRows();
                this.dataGridView1.AutoResizeColumns();

                SetCaption();

                if (!string.IsNullOrEmpty(this.FirstImageLink) && this.FirstImageLink != firstLink)
                {
                    this.notifyIcon1.Text = "Hadi";
                    this.notifyIcon1.BalloonTipText = "New!";
                    this.notifyIcon1.BalloonTipTitle = "New'";
                    this.notifyIcon1.Icon = this.Icon;
                    this.notifyIcon1.Visible = true;
                    this.notifyIcon1.ShowBalloonTip(5 * 1000);
                }

                this.FirstImageLink = firstLink;
            }
            catch (Exception)
            {
                //do nothing
            }
        }

        void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex > -1 && e.ColumnIndex == this.dataGridView1.Columns["Image"].Index)
            {
                if (this.dataGridView1["link", e.RowIndex].Value != null && e.Value == null)
                {
                    try
                    {
                        var bin = (byte[])dataGridView1["link", e.RowIndex].Value;
                        e.Value = GetImage(bin);
                    }
                    catch (Exception)
                    {
                        //do nothing
                    }
                }
            }
        }

        Image GetImage(byte[] bin)
        {
            using (var ms = new System.IO.MemoryStream(bin))
            {
                return ResizeImage(Image.FromStream(ms), 150, 150);
            }
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

    }
}
