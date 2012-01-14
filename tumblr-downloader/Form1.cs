using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using tumblr_downloader;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Hammock;
using Hammock.Serialization;
using Hammock.Model;
using Hammock.Streaming;
using Timer = System.Threading.Timer;


namespace tumblr_downloader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void selectFolder_Click(object sender, EventArgs e)
        {
            SelectFolderDialog.ShowDialog();

            pathBox.Text = SelectFolderDialog.SelectedPath.ToString();
            AppSettings.Default.downlaodpath = SelectFolderDialog.SelectedPath.ToString();
            AppSettings.Default.Save();
        }

        private void downloadBtn_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
            timer1.Start();

        }

        public string User { get; set; }
        private Uri URL;
        private SynchronizedCollection<Uri> allImages = new SynchronizedCollection<Uri>();


        public void fetchXml()
        {
            User = usernamBox.Text.Replace(" ", "").Replace("/", "").Replace(".", "");
            URL = new Uri("http://" + User + ".tumblr.com/api/read/json");

            int max = 0;
            var client = new RestClient();
            client.Authority = URL.ToString();
            var request = new RestRequest();
            request.AddParameter("type", "photo");
            request.AddParameter("num", "50");
            request.AddParameter("filter", "text");
            var r1 = client.Request(request);
            var t = r1.Content.ToString().Replace("var tumblr_api_read = ", "");
            var firstResponse = JsonParser.FromJson(t);
            max = Convert.ToInt32(firstResponse["posts-total"]);
            // to eventually make each fetch a separate request
            for (int i = 0; i < max; i += 51)
            {
                if (i != 0)
                {
                    request.AddParameter("start", i.ToString());
                }
                var r2 = client.Request(request);
                var t2 = r2.Content.ToString().Replace("var tumblr_api_read = ", "");
                var Response = JsonParser.FromJson(t2);

                getUrls(Response.ToDictionary(x => x.Key, x => x.Value));
            }
          

        }

        private void getUrls(Dictionary<string, object> dictionary)
        {
            var posts = dictionary.Last().Value;

            foreach (var o in posts as List<object>)
            {
                var temp = o as Dictionary<string, object>;

                if (String.IsNullOrEmpty(temp["photo-url-1280"].ToString()) ||
                    String.IsNullOrWhiteSpace(temp["photo-url-1280"].ToString()))
                {
                    allImages.Add(new Uri(temp["photo-url-500"].ToString()));
                }
                else
                {
                    allImages.Add(new Uri(temp["photo-url-1280"].ToString()));
                }
            }
           
        }

        private void DownloadFile()
        {
            if (allImages.Any())
            {
                for (int i = 0; i < allImages.Count; i++)
                {
                    WebClient client = new WebClient();
                    client.DownloadFileCompleted += Completed;
                    string FileName = allImages[i].Segments.Last().ToString();
                    if (FileName.EndsWith(".jpg") || FileName.EndsWith(".png") || FileName.EndsWith(".gif") )
                    {
                        
                        client.DownloadFileAsync(allImages[i],
                                              AppSettings.Default.downlaodpath + @"\" + FileName );
                    }
                    else
                    {
                        client.DownloadFileAsync(allImages[i], AppSettings.Default.downlaodpath + @"\" + FileName+".jpg");
                    }
                }
            }
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            progressBar.Value++;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(AppSettings.Default.downlaodpath))
            {
                pathBox.Text = AppSettings.Default.downlaodpath;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            fetchXml();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            totalImagesLbl.Text = allImages.Count.ToString();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Stop();
            progressBar.Maximum = allImages.Count;
            totalImagesLbl.Text = allImages.Count.ToString();
            DownloadFile();
        }
    }
}