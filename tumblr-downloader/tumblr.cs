using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Hammock;
using Hammock.Serialization;
using Hammock.Model;
using Hammock.Streaming;
namespace tumblr_downloader
{
    class Tumblr
    {
        public string User { get; set; }
        private Uri URL ;
        private List<Uri> allImages = new List<Uri>(); 
        public Tumblr(string username)
        {
            User = username.Replace(" ", "").Replace("/", "").Replace(".", "");
            URL = new Uri("http://"+User+".tumblr.com/api/read/json");
        }
        
        public void fetchXml()
        {
           
      
            int max = 0;
            var client = new RestClient();
            client.Authority = URL.ToString();
            var request = new RestRequest();
            request.AddParameter("type", "photo");
            request.AddParameter("num", "50");
            request.AddParameter("filter", "text");
             var r1 = client.Request(request);
             var t = r1.Content.ToString().Replace("var tumblr_api_read = ", "");
             var firstResponse  = JsonParser.FromJson(t);
                max = Convert.ToInt32(firstResponse["posts-total"]);
             for (int i = 0; i < max; i+=51)
              {
                  if (i !=0)
                  {
                      request.AddParameter("start", i.ToString());
                  }
                 
                 
                  var r2 = client.Request(request);
                  var t2 = r2.Content.ToString().Replace("var tumblr_api_read = ", "");
                  var Response = JsonParser.FromJson(t2);

                  getUrls(Response.ToDictionary(x=>x.Key,x=>x.Value));

                  
               
                
              }
            DownloadFile();
            return;
        }

        private void getUrls(Dictionary<string,object> dictionary )
        {
            
            var posts = dictionary.Last().Value;

            foreach (var o in posts as List<object>)
            {
                var temp = o as Dictionary<string, object>;

                if (String.IsNullOrEmpty(temp["photo-url-1280"].ToString()) || String.IsNullOrWhiteSpace(temp["photo-url-1280"].ToString()))
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
                WebClient client = new WebClient();
              
                foreach (var image in allImages)
                {
                  
                   string FileName = image.Segments.Last().ToString();
                   if (FileName.LastIndexOf(".") == 0)
                    {
                        client.DownloadFileAsync(image, AppSettings.Default.downlaodpath + @"\" + FileName);
                    }
                   else
                   {
                       client.DownloadFileAsync(image, AppSettings.Default.downlaodpath + @"\" + FileName+".jpg");
                   }
                }   
            }
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
          
        }
    }
}
