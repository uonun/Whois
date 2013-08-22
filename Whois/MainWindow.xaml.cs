using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Whois
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> _domains = new List<string>();
        private StreamWriter _fileStream;
        private StreamWriter _errorStream;
        private string _dic;
        private string _ext;
        private int _length;
        private bool _needGenerateDomains = true;

        private SynchronizationContext _sc;

        public MainWindow()
        {
            InitializeComponent();

            string errorFilePath = Environment.CurrentDirectory + "\\error.log";
            _errorStream = new StreamWriter(errorFilePath);
            _sc = SynchronizationContext.Current;
            _dic = tbDic.Text;
            _ext = tbExt.Text;
            _length = int.Parse(tbLength.Text);

            tbDic.TextChanged += (s, e) => { _dic = tbDic.Text; _needGenerateDomains = true; };
            tbExt.TextChanged += (s, e) => { _ext = tbExt.Text; _needGenerateDomains = true; };
            tbLength.TextChanged += (s, e) => { _length = int.Parse(tbLength.Text); _needGenerateDomains = true; };
        }

        private void buCheck_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                _sc.Post(delegate { lbResult.Items.Clear(); }, null);


                string filePath = Environment.CurrentDirectory + "\\result.txt";
                if (File.Exists(filePath)) File.Delete(filePath);

                int n = 0;
                using (_fileStream = new StreamWriter(filePath) { AutoFlush = true })
                {
                    GenericDomains(_length, (d) =>
                    {
                        bool? isOk = CheckDomain(d, _ext);
                        var msg = string.Empty;
                        switch (isOk)
                        {
                            case false:
                                {
                                    n++;
                                    msg = string.Format("{0}{1} Unregisted.", d, _ext);
                                }
                                break;
                            case true:
                                msg = string.Format("{0}{1} Registed.", d, _ext);
                                break;
                            default:
                                msg = string.Format("{0}{1} Unknown.", d, _ext);
                                break;
                        }

                        _fileStream.WriteLine(msg);
                        AddItem(msg);
                        Thread.Sleep(200);
                    });
                }

                MessageBox.Show(string.Format("Got {0} domains unregisted.", n));
                Process.Start(filePath);
            }).Start();
        }

        private void buPreview_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                _sc.Post(delegate { lbResult.Items.Clear(); }, null);

                GenericDomains(_length, (d) => AddItem(d + _ext));
            }).Start();
        }

        private bool? CheckDomain(string domainName, string ext)
        {
            ShowInfo(string.Format("Checking...{0}{1}", domainName, ext));

            bool? result = null;
            Uri uri = new Uri("http://sys.rwen.com/style/info/newxhccl.asp?domain=" + domainName + ext);
            WebRequest request = WebRequest.Create(uri);
            request.Method = "GET";
            using (HttpWebResponse rsp = (HttpWebResponse)request.GetResponse())
            {
                if (rsp.StatusCode == HttpStatusCode.OK)
                {
                    using (var s = rsp.GetResponseStream())
                    {
                        StreamReader sr = new StreamReader(s);
                        string tmp = sr.ReadToEnd();
                        var pattern = domainName + ext.Replace(".", "\\.");
                        Regex re = new Regex("value%3D" + pattern + "%20disabled%3D%22disabled%22");
                        result = re.IsMatch(tmp);
                    }
                }
                else
                {
                    _errorStream.WriteLine(string.Format("Unknown: {0}{1}", domainName, ext));
                }
            }
            return result;
        }

        private void GenericDomains(int length, Action<string> callback = null)
        {
            if (_needGenerateDomains)
            {
                _domains = new List<string>();
                var mi = _dic.Length;
                int min = (int)Math.Pow(mi, length - 1);
                int max = 0;
                for (var i = 0;i < length;i++)
                {
                    max += (int)((mi - 1) * Math.Pow(mi, i));
                }

                Trace.WriteLine(string.Format("min = {0}, max = {1}", min, max));

                for (var i = min;i <= max;i++)
                {
                    var result = "";
                    for (var j = length - 1;j >= 0;j--)
                    {
                        var last = (i % Math.Pow(mi, j + 1) / Math.Pow(mi, j));
                        result += _dic[(int)last];
                    }
                    _domains.Add(result);
                    ShowInfo(string.Format("Domain generated: {0}{1}", result, _ext));

                    if (callback != null)
                    {
                        callback(result);
                    }

                    Thread.Sleep(1);
                }

                _needGenerateDomains = false;
            }

            if (callback != null)
            {
                foreach (var d in _domains)
                {
                    callback(d);
                }
            }
        }

        private void AddItem(string item)
        {
            _sc.Post((x) =>
            {
                lbResult.Items.Insert(0, x);
                lbResult.SelectedIndex = 0;
            }, item);
        }

        private void ShowInfo(string msg)
        {
            _sc.Post(delegate
            {
                lbInfo.Content = msg;
            }, _sc);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _errorStream.Flush();
            _errorStream.Close();
            _errorStream.Dispose();
        }


    }
}
