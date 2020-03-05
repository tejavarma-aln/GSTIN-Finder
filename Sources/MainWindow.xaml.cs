using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace GST_Finder
{
    

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private Dictionary<String, String> PGSTIN = new Dictionary<string, string>();

        private const int WAIT_TIME = 5;   // increase wait time if you face poor network connection or frequent errors..

        private List<String> GST_INFO = new List<string>();



        public int find_gst(String gst)
        {
            try
            {
                var phantom_serverice = PhantomJSDriverService.CreateDefaultService();
                phantom_serverice.HideCommandPromptWindow = true;
                phantom_serverice.LoadImages = false;
                phantom_serverice.SuppressInitialDiagnosticInformation = true;
                phantom_serverice.IgnoreSslErrors = true;
                using (var phantom_instance = new PhantomJSDriver(phantom_serverice))
                {
                    phantom_instance.Navigate().GoToUrl("https://www.knowyourgst.com/gst-number-search/");
                    IWebElement gst_serach_box = phantom_instance.FindElement(By.Id("gstnumber"));
                    gst_serach_box.SendKeys(gst);
                    phantom_instance.FindElement(By.ClassName("btn")).Click();
                    phantom_instance.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(WAIT_TIME));

                    if (phantom_instance.FindElements(By.TagName("td")).Count != 0)
                    {

                        ReadOnlyCollection<IWebElement> data_elements = phantom_instance.FindElements(By.TagName("td"));
                        GST_INFO.Add(data_elements[1].Text);
                        GST_INFO.Add(data_elements[3].Text);
                        GST_INFO.Add(data_elements[5].Text);
                        GST_INFO.Add(data_elements[9].Text);
                        GST_INFO.Add(data_elements[11].Text);
                        GST_INFO.Add(data_elements[13].Text);
                        GST_INFO.Add(data_elements[17].Text);

                        return 0;
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show("Exception while Scrapping",ex.Message);
                }));
            }

            return 1;
        }
        


        private async void InintScrapper(String gstin,Grid gd)
        {
            if (!String.IsNullOrEmpty(gstin) && gstin.Length == 15)
            {
                GST_INFO.Clear();
                EllipseLoader.Visibility = Visibility.Visible;
                MainPanel.IsEnabled = false;
                int res = await Task.Factory.StartNew(() => find_gst(gstin));
                EllipseLoader.Visibility = Visibility.Hidden;
                MainPanel.IsEnabled = true;
               if(res == 0)
                {
                    ProcessResponse(gd);
                }
                else
                {
                    HandleErrors("Scrapping failed due to errors");
                }
            }
            else
            {
                HandleErrors("Invalid GSTIN,It must be 15 characters");
            }

        }


        private void ProcessResponse(Grid gd)
        {

            if (GST_INFO.Count() > 0)
            {
                gd.Visibility = Visibility.Hidden;
                responsePanel.Visibility = Visibility.Visible;
                cmpName.Text = GST_INFO[0];
                Pan.Text = GST_INFO[1];
                LegalName.Text = GST_INFO[2];
                Entity.Text = GST_INFO[3];
                RegType.Text = GST_INFO[4];
                RegDate.Text = GST_INFO[5];
                DepCode.Text = GST_INFO[6];
            }
            else
            {
                HandleErrors("No data found for this GSTIN");
            }
        }



        private String PgstPayLoad()
        {
            StringBuilder PayloadBuilder = new StringBuilder();
            PayloadBuilder.Append("<ENVELOPE><HEADER><VERSION>1</VERSION><TALLYREQUEST>Export</TALLYREQUEST><TYPE>Collection</TYPE><ID>HostPartyGstColl</ID></HEADER><BODY>");
            PayloadBuilder.Append("<DESC><STATICVARIABLES><SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT></STATICVARIABLES><TDL><TDLMESSAGE><COLLECTION NAME = 'HostPartyGstColl'><TYPE>Ledger</TYPE>");
            PayloadBuilder.Append("<METHOD>GSTIN:$PartyGSTIN</METHOD><METHOD>Name:$Name</METHOD><FILTER>OnlyRequiredGroup</FILTER></COLLECTION><SYSTEM Type = 'Formulae' Name = 'OnlyRequiredGroup'>$$IsGroupSundryDebtors:$_PrimaryGroup or $$IsGroupSundryCreditors:$_PrimaryGroup</SYSTEM>");
            PayloadBuilder.Append("</TDLMESSAGE></TDL></DESC></BODY></ENVELOPE>");

            return PayloadBuilder.ToString();
        }


        private void HandleErrors(String errMsg)
        {
            error_grid.Visibility = Visibility.Visible;
            error_content.Text = errMsg;
            DoubleAnimation da = new DoubleAnimation(3, 0, new Duration(TimeSpan.FromSeconds(5)));
            da.AutoReverse = false;
            error_grid.BeginAnimation(OpacityProperty, da);
        }

        private void ButtonClickHandle(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "Tsearchbutton":
                    InintScrapper(Pgst.Text.Trim(),TallySearch);
                    break;
                case "searchbutton":
                    InintScrapper(gst.Text.Trim(),NormalSearch);
                    break;
                case "Backbutton":
                    responsePanel.Visibility = Visibility.Hidden;
                    NormalSearch.Visibility = Visibility.Visible;
                    break;
                case "Treloadbutton":
                    GetTallyData();
                    break;
                default:
                    break;
            }
          
        }


        private void ParseGSTIN(XDocument x)
        {

            foreach (var node in x.Root.Elements("BODY").Elements("DATA").Elements("COLLECTION").Elements("LEDGER"))
            {
                if (!PGSTIN.ContainsKey(node.Element("NAME").Value.ToUpper().Trim())) PGSTIN.Add(node.Element("NAME").Value.ToUpper().Trim(), String.IsNullOrEmpty(node.Element("GSTIN").Value.ToUpper().Trim())?"0": node.Element("GSTIN").Value.ToUpper().Trim());
            }

           
        }




        private void HyperLinkHandle(object sender, RoutedEventArgs e)
        {
           
            switch (((Hyperlink)sender).Name)
            {
                case "tsearchlink":
                    GetTallyData();
                    break;
                case "nsearchlick":
                    TallySearch.Visibility = Visibility.Hidden;
                    NormalSearch.Visibility = Visibility.Visible;
                    gst.Clear();
                    break;
                default:
                    break;

            }
        }


        private async void GetTallyData()
        {

            if (!TallySearch.IsVisible) tsearchlink.IsEnabled = false; 
            EllipseLoader.Visibility = Visibility.Visible;
            Task<XDocument> tk = new Task<XDocument>(() => new NetworkHandler(PgstPayLoad()).handle_request());
            tk.Start();
            XDocument xdc = await tk;
            if (xdc != null)
            {
                TpartyCombo.ItemsSource = null;
                PGSTIN.Clear();

                await Task.Factory.StartNew(() => ParseGSTIN(xdc));

               if(NormalSearch.IsVisible) NormalSearch.Visibility = Visibility.Hidden;
               if(!TallySearch.IsVisible) TallySearch.Visibility = Visibility.Visible;
                if (PGSTIN.Count() > 0) TpartyCombo.ItemsSource = PGSTIN.Keys ; else HandleErrors("Sorry we didn't found any records");
               Pgst.Text = "PARTY GSTIN";

            }
            else
            {
                HandleErrors("Error Occurred while fetching data");
            }
            if(!tsearchlink.IsEnabled) tsearchlink.IsEnabled = true; 
            EllipseLoader.Visibility = Visibility.Hidden;
        }

        private void TpartyCombo_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (!(TpartyCombo.SelectedIndex < 0))
            {
                String value = PGSTIN[PGSTIN.Keys.ElementAt(TpartyCombo.SelectedIndex)];
                Pgst.Text = value;
            }
        }
    }
}
