using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
using System.Threading.Tasks;
using System.Security;

namespace Whois
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isChecking = false;
        private bool _isGeneratingDomains = false;
        private CancellationTokenSource _cancel = new CancellationTokenSource();
        private readonly StreamWriter _errorStream;
        private readonly string _filePathRegisted = Environment.CurrentDirectory + "\\result_registed.txt";
        private readonly string _filePathUnregisted = Environment.CurrentDirectory + "\\result_unregisted.txt";
        private readonly string _filePathUnknown = Environment.CurrentDirectory + "\\result_unknown.txt";
        private readonly string _errorFilePath = Environment.CurrentDirectory + "\\error.log";
        private const string STR_TITLE = "Domain Check";
        private const string STR_READY = "Ready";
        private const string STR_CBAPI_OPT_CUSTOM = "Custom";
        private const string STR_BU_GENERATE = "生成域名";
        private const string STR_BU_CHECK = "开始查询";
        private const string STR_BU_STOP = "停止";
        private const string STR_STOPPING = "正在停止...";
        private const string STR_STOPPPED = "已被用户停止";
        private const string STR_COMPLETED = "已完成";
        private const string STR_ERR_DIC_SHOUD_NOT_BE_EMPTY = "用于生成域名的字符字典不能为空！";
        private const string STR_ERR_EXT_ERROR = "域名扩展名格式不正确！";
        private const string STR_ERR_LENGTH_ERROR = "指定的域名长度必须大于 0 ！";
        private const string STR_ERR_GENERATE_DOMAIN_FIRST = "请先填写或生成符合格式的域名！";
        private readonly SynchronizationContext _sc;

        public MainWindow()
        {
            InitializeComponent();

            _errorStream = new StreamWriter(_errorFilePath);
            _sc = SynchronizationContext.Current;
            tbDomains.UndoLimit = 0;
            tbDomains.KeyDown += (s, e) =>
            {
                if (tbDomains.Tag != null)
                {
                    tbDomains.Tag = null;
                    tbDomains.Text = null;
                }
            };

            tbLength.LostFocus += (s, e) =>
            {
                int n;
                if (!int.TryParse(tbLength.Text, out n))
                {
                    MessageBox.Show("Please input a number");
                    tbLength.Text = "3";
                }
                e.Handled = true;
            };

            SetWorkState(WorkState.Inited);
        }

        private void buPreview_Click(object sender, RoutedEventArgs e)
        {
            if (!_isGeneratingDomains)
            {
                new Thread((tmp) =>
                {
                    try
                    {
                        _isGeneratingDomains = true;

                        SetWorkState(WorkState.Generating);

                        var args = (Tuple<string, string, string>)tmp;
                        var domains = new List<Tuple<string, string>>();
                        var domainsString = string.Empty;

                        GenerateDomains(args.Item1, args.Item2, int.Parse(args.Item3), (domain) => _sc.Post((x) =>
                        {
                            var d = (Tuple<string, string>)x;
                            domains.Add(d);
                            domainsString += domain.ToStringExt() + Environment.NewLine;
                        }, domain));

                        _sc.Post(delegate
                        {
                            tbDomains.Tag = domains;
                            tbDomains.Text = domainsString;
                        }, null);
                    }
                    catch (Exception ex)
                    {
                        _errorStream.WriteLine(ex.ToString());
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        _isGeneratingDomains = false;
                        SetWorkState(WorkState.GeneratingCanceled);
                    }
                }) { IsBackground = true }.Start(new Tuple<string, string, string>(tbDic.Text, tbExt.Text, tbLength.Text));
            }
            else
            {
                _isGeneratingDomains = !_isGeneratingDomains;
            }
        }

        private void buCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!_isChecking)
            {
                SetWorkState(WorkState.Checking);

                _cancel = new CancellationTokenSource();

                Task<List<Tuple<string, string>>>.Factory.StartNew(PrepareDomains,
                    new Tuple<object, string>(tbDomains.Tag, tbDomains.Text))
                    .ContinueWith((x) =>
                    {
                        _isChecking = true;
                        var checkApi = new CheckApi_RWEN();
                        Checking(checkApi, x.Result);
                        _isChecking = false;
                    }, _cancel.Token)
                    .ContinueWith((x) =>
                     {
                         // IsFaulted must before IsCompleted 
                         if (x.IsFaulted)
                         {
                             if (x.Exception != null)
                             {
                                 var err = string.Empty;
                                 var tmp = x.Exception.InnerExceptions.ToList();
                                 tmp.ForEach((ex) =>
                                 {
                                     err += ex.Message + Environment.NewLine;
                                 });

                                 _errorStream.WriteLine("Got error: {0}", err);
                                 MessageBox.Show(err);
                             }
                             // should not occor.
                             else
                             {
                                 _errorStream.WriteLine("Got unknown error.");
                                 MessageBox.Show("Got unknown error.");
                             }
                         }
                         else if (x.IsCanceled)
                         {
                             SetWorkState(WorkState.CheckCanceled);
                         }
                         else if (x.IsCompleted)
                         {
                             SetWorkState(WorkState.CheckCompleted);
                         }
                     });
            }
            else
            {
                _isChecking = false;
                _cancel.Cancel();

                SetWorkState(WorkState.CheckCanceling);
            }
        }

        private void Checking(ICheckApi checkApi, IEnumerable<Tuple<string, string>> domains)
        {
            if (_cancel.IsCancellationRequested) { return; }
            if (domains != null)
            {
                var arr = domains.ToList();
                int n = 0;
                if (File.Exists(_filePathRegisted)) File.Delete(_filePathRegisted);
                using (StreamWriter fileStreamRegisted = new StreamWriter(_filePathRegisted)
                    , fileStreamUnRegisted = new StreamWriter(_filePathUnregisted)
                    , fileStreamUnKnown = new StreamWriter(_filePathUnknown))
                {
                    for (int i = 0;i < arr.Count;i++)
                    {
                        if (_cancel.IsCancellationRequested) { break; }

                        ShowProgress(i, arr.Count, string.Format("Checking...{0}, {{0}}/{{1}}", arr[i].ToStringExt()));

                        bool? isOk = CheckDomain(checkApi, arr[i]);
                        var msg = arr[i].ToStringExt();
                        switch (isOk)
                        {
                            case false:
                                {
                                    fileStreamRegisted.WriteLine(msg);
                                    AddItem(lbResultRegisted, msg);
                                }
                                break;
                            case true:
                                {
                                    n++;
                                    fileStreamUnRegisted.WriteLine(msg);
                                    AddItem(lbResultUnRegisted, msg);
                                }
                                break;
                            default:
                                {
                                    fileStreamUnKnown.WriteLine(msg);
                                    AddItem(lbResultUnknown, msg);
                                }
                                break;
                        }

                        Thread.Sleep(100);
                    }
                }

                var state = string.Format("Done. Got {0} domains unregisted.", n);
                _sc.Post(delegate
                {
                    pb.ContentStringFormat = state;
                    Title = string.Format("{0}, {1}", STR_TITLE, state);
                }, null);
            }
            else
            {
                throw new BzException(STR_ERR_GENERATE_DOMAIN_FIRST);
            }
        }

        private List<Tuple<string, string>> PrepareDomains(object args)
        {
            List<Tuple<string, string>> domains = null;
            var tmp = (Tuple<object, string>)args;
            // use Tag first.
            domains = tmp.Item1 as List<Tuple<string, string>>;
            if (domains == null)
            {
                var lines = !string.IsNullOrWhiteSpace(tmp.Item2)
                    ? tmp.Item2.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    : null;
                if (lines != null && lines.Length > 0)
                {
                    domains = new List<Tuple<string, string>>();
                    foreach (var line in lines)
                    {
                        if (_cancel.IsCancellationRequested) { break; }

                        var idxLastDot = line.LastIndexOf('.');
                        if (idxLastDot > 0)
                        {
                            domains.Add(new Tuple<string, string>(line.Substring(0, idxLastDot),
                                line.Substring(idxLastDot, line.Length - idxLastDot)));
                        }
                    }
                }
            }
            return domains;
        }

        private bool? CheckDomain(ICheckApi checkApi, Tuple<string, string> domain)
        {
            Debug.Assert(checkApi != null, "_checkApi != null");

            bool? result = null;
            Uri uri = new Uri(string.Format(checkApi.Url, domain.ToStringExt()));

            WebRequest request = WebRequest.Create(uri);
            request.Method = "GET";
            using (var rsp = (HttpWebResponse)request.GetResponse())
            {
                if (rsp.StatusCode == HttpStatusCode.OK)
                {
                    using (var s = rsp.GetResponseStream())
                    {
                        Debug.Assert(s != null, "s != null");
                        var sr = new StreamReader(s);
                        string tmp = sr.ReadToEnd();
                        var pattern = (domain.Item1 + domain.Item2).Replace(".", "\\.");
                        var reUnavailable = new Regex(string.Format(checkApi.PatternUnavailable, pattern));
                        if (reUnavailable.IsMatch(tmp))
                        {
                            result = false;
                        }
                        else
                        {
                            var reAvailable = new Regex(string.Format(checkApi.PatternAvailable, pattern));
                            if (reAvailable.IsMatch(tmp))
                            {
                                result = true;
                            }
                            else
                            {
                                // result can not assigned to be false here, because it may be Unknown.
                            }
                        }
                    }
                }
                else
                {
                    _errorStream.WriteLine("WebRequest Error: Domain = {0}, StatusCode = {1}, Url = {2}", domain, rsp.StatusCode, uri);
                }
            }
            return result;
        }

        private void GenerateDomains(string dic, string ext, int length, Action<Tuple<string, string>> callback = null)
        {
            if (string.IsNullOrWhiteSpace(dic))
                throw new BzException(STR_ERR_DIC_SHOUD_NOT_BE_EMPTY);

            if (string.IsNullOrWhiteSpace(ext) || !ext.StartsWith(".") || ext.EndsWith("."))
                throw new BzException(STR_ERR_EXT_ERROR);

            if (length < 1)
                throw new BzException(STR_ERR_LENGTH_ERROR);

            var fromBase = dic.Length;
            var min = 0;
            var max = 0;
            if (fromBase > 1)
            {
                max = (int)Math.Pow(fromBase, length) - 1;
            }
            else { max = min; }

            Trace.WriteLine(string.Format("min = {0}, max = {1}", min, max));

            var currentIdx = 0;
            for (currentIdx = min;currentIdx <= max;currentIdx++)
            {
                var result = "";
                for (var j = length - 1;j >= 0;j--)
                {
                    var last = (currentIdx % Math.Pow(fromBase, j + 1) / Math.Pow(fromBase, j));
                    result += dic[(int)last];
                }

                ShowProgress(currentIdx, max, string.Format("Domain generated: {0}{1}, {{0}}/{{1}}", result, ext));

                if (callback != null)
                {
                    var domain = new Tuple<string, string>(result, ext);
                    callback(domain);
                }

                if (!_isGeneratingDomains) break;
                Thread.Sleep(1);
            }

            _sc.Post(delegate
            {
                if (currentIdx < max + 1)
                {
                    pb.ContentStringFormat = string.Format("Domain generated: {0}/{1}", currentIdx - min, max - min);
                }
                else
                {
                    pb.ContentStringFormat = string.Format("Done: {0} domain generated", currentIdx - min);
                }
            }, _sc);
        }

        private void AddItem(Selector lb, string item)
        {
            _sc.Post((x) =>
            {
                lb.Items.Insert(0, x);
                lb.SelectedIndex = 0;
            }, item);
        }

        private void ShowProgress(int current, int max, string formator = null)
        {
            _sc.Post(delegate
            {
                if (!string.IsNullOrEmpty(formator))
                {
                    pb.ContentStringFormat = formator;
                }

                pb.Value = current;
                pb.Maximum = max;
            }, _sc);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _errorStream.Flush();
            _errorStream.Close();
            _errorStream.Dispose();
        }

        private void textBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var f = string.Empty;
            if (Equals(sender, textBlock_reged))
            {
                f = _filePathRegisted;
            }
            else if (Equals(sender, textBlock_unreged))
            {
                f = _filePathUnregisted;
            }
            else if (Equals(sender, textBlock_unknown))
            {
                f = _filePathUnknown;
            }

            if (File.Exists(f))
                Process.Start(f);
        }

        private void SetWorkState(WorkState state)
        {
            _sc.Post(delegate
            {
                switch (state)
                {
                    case WorkState.Inited:
                    case WorkState.Generated:
                    case WorkState.GeneratingCanceled:
                        gbGenerator.IsEnabled = true;
                        gbAPI.IsEnabled = true;
                        tbDic.IsEnabled = true;
                        tbDomains.IsEnabled = true;
                        buCheck.IsEnabled = true;
                        buCheck.Content = STR_BU_CHECK;
                        buPreview.IsEnabled = true;
                        buPreview.Content = STR_BU_GENERATE;
                        Title = STR_TITLE;
                        if (state == WorkState.Inited)
                            pb.ContentStringFormat = STR_READY;
                        pb.Value = 0;
                        pb.ProgressBar.Visibility = Visibility.Collapsed;
                        break;
                    case WorkState.Generating:
                        gbGenerator.IsEnabled = true;
                        gbAPI.IsEnabled = false;
                        tbDic.IsEnabled = false;
                        tbDomains.Tag = null;
                        tbDomains.Text = "";
                        tbDomains.IsEnabled = false;
                        buCheck.IsEnabled = false;
                        buCheck.Content = STR_BU_CHECK;
                        buPreview.IsEnabled = true;
                        buPreview.Content = STR_BU_STOP;
                        Title = STR_TITLE;
                        pb.ContentStringFormat = STR_READY;
                        pb.Value = 0;
                        pb.ProgressBar.Visibility = Visibility.Visible;
                        break;
                    case WorkState.GeneratingCanceling:
                        gbGenerator.IsEnabled = false;
                        gbAPI.IsEnabled = false;
                        tbDic.IsEnabled = false;
                        tbDomains.IsEnabled = false;
                        buCheck.IsEnabled = false;
                        buPreview.IsEnabled = false;
                        Title = STR_TITLE;
                        buCheck.Content = STR_BU_CHECK;
                        pb.ContentStringFormat = STR_READY;
                        pb.Value = 0;
                        pb.ProgressBar.Visibility = Visibility.Collapsed;
                        break;
                    case WorkState.Checking:
                        lbResultUnknown.Items.Clear();
                        lbResultRegisted.Items.Clear();
                        lbResultUnRegisted.Items.Clear();

                        gbGenerator.IsEnabled = false;
                        gbAPI.IsEnabled = false;
                        tbDic.IsEnabled = false;
                        tbDomains.IsEnabled = false;
                        tbDomains.IsReadOnly = true;
                        buCheck.IsEnabled = true;
                        buPreview.IsEnabled = false;
                        Title = STR_TITLE;
                        buCheck.Content = STR_BU_STOP;
                        pb.ContentStringFormat = STR_READY;
                        pb.Value = 0;
                        pb.ProgressBar.Visibility = Visibility.Visible;
                        break;
                    case WorkState.CheckCanceling:
                        gbGenerator.IsEnabled = false;
                        gbAPI.IsEnabled = false;
                        tbDic.IsEnabled = false;
                        tbDomains.IsEnabled = true;
                        tbDomains.IsReadOnly = true;
                        buCheck.IsEnabled = false;
                        buPreview.IsEnabled = false;
                        Title = string.Format("{0}, {1}", STR_TITLE, STR_STOPPING);
                        buCheck.Content = STR_STOPPING;
                        pb.ContentStringFormat = STR_READY;
                        pb.Value = 0;
                        pb.ProgressBar.Visibility = Visibility.Visible;
                        break;
                    case WorkState.CheckCanceled:
                        gbGenerator.IsEnabled = true;
                        gbAPI.IsEnabled = true;
                        tbDic.IsEnabled = true;
                        tbDomains.IsEnabled = true;
                        tbDomains.IsReadOnly = false;
                        buCheck.IsEnabled = true;
                        buPreview.IsEnabled = true;
                        Title = string.Format("{0}, {1}", STR_TITLE, STR_STOPPPED);
                        buCheck.Content = STR_BU_CHECK;
                        //pb.ContentStringFormat = STR_READY;
                        pb.Value = 0;
                        pb.ProgressBar.Visibility = Visibility.Collapsed;
                        break;
                    case WorkState.CheckCompleted:
                        gbGenerator.IsEnabled = true;
                        gbAPI.IsEnabled = true;
                        tbDic.IsEnabled = true;
                        tbDomains.IsEnabled = true;
                        tbDomains.IsReadOnly = false;
                        buCheck.IsEnabled = true;
                        buPreview.IsEnabled = true;
                        Title = string.Format("{0}, {1}", STR_TITLE, STR_COMPLETED);
                        buCheck.Content = STR_BU_CHECK;
                        //pb.ContentStringFormat = STR_READY;
                        pb.Value = 0;
                        pb.ProgressBar.Visibility = Visibility.Collapsed;
                        break;
                        break;
                    default:
                        break;
                }

                tbAPI_url.IsEnabled =
                    tbAPI_a_partten.IsEnabled =
                        tbAPI_u_partten.IsEnabled = (cbAPI_opt.SelectedValue as string == STR_CBAPI_OPT_CUSTOM);


            }, null);
        }
    }
}
