using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Xml.Linq;

namespace GST_Finder
{
    class NetworkHandler
    {

        private String payload;
        private XDocument xd;

        public NetworkHandler(String pay_load)
        {
            this.payload = pay_load;
            this.xd = new XDocument();
        }
 

        public XDocument handle_request()
        {
            try
            {
                var http_request = WebRequest.Create("http://localhost:9000") as HttpWebRequest;
                var data_chunks = Encoding.UTF8.GetBytes(payload);
                http_request.Method = "POST";
                http_request.ContentType = "text/xml ; encoding ='UTF-8'";
                http_request.ContentLength = data_chunks.Length;
                using (var request_buffer = http_request.GetRequestStream())
                {
                    request_buffer.Write(data_chunks, 0, data_chunks.Length);
                }
                if (http_request.GetResponse() is HttpWebResponse http_resposne)
                {
                    using (var response_buffer = new StreamReader(http_resposne.GetResponseStream()))
                    {
                        using (var response_string = new StringReader(response_buffer.ReadToEnd()))
                        {
                            xd = XDocument.Load(response_string);
                            return xd;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + "Possible Reasons :" + Environment.NewLine + "* Tally.ERP9 is Not Running " + Environment.NewLine + "* There is no active company opened in Tally" + Environment.NewLine + "* Make sure tally is running on port 9000 in F12->Advaced Configuration","Exception while communicating with Tally");
                }));
               
            }

            return null;
        }

    }
}
