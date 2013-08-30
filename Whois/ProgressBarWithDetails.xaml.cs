using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

namespace Whois
{
    /// <summary>
    /// ProgressBarWithDetails.xaml 的交互逻辑
    /// </summary>
    [DefaultProperty("Value")]
    public partial class ProgressBarWithDetails : UserControl
    {
        public static readonly DependencyProperty TextFormatProperty = DependencyProperty.Register("ContentStringFormat", typeof(string), typeof(ProgressBarWithDetails), (PropertyMetadata)new FrameworkPropertyMetadata((object)"Processing {0} of {1}...", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, null, null), new ValidateValueCallback(IsValidMeanfulStringValue));

        public ProgressBarWithDetails()
        {
            InitializeComponent();
            
            this.Label.Padding = new Thickness(0, 1, 0, 1);
            this.LayoutUpdated += (s, e) =>
            {
                if (!string.IsNullOrEmpty(ContentStringFormat))
                {
                    this.Label.Content = string.Format(ContentStringFormat, Value, Maximum);
                    this.Label.Foreground = SystemColors.ControlTextBrush;
                }
                else
                {
                    this.Label.Content = "Formator should not be null or empty!";
                    this.Label.Foreground = Brushes.Red;
                }
            };
        }

        [Category("Custom")]
        public string ContentStringFormat
        {
            get
            {
                return (string)this.GetValue(ProgressBarWithDetails.TextFormatProperty);
            }
            set
            {
                this.SetValue(TextFormatProperty, (object)value);
            }
        }

        [Category("Custom")]
        [DefaultValue(0)]
        public double Value
        {
            get { return ProgressBar.Value; }
            set { ProgressBar.Value = value; }
        }

        [Category("Custom")]
        [DefaultValue(10)]
        public double Maximum
        {
            get { return ProgressBar.Maximum; }
            set { ProgressBar.Maximum = value; }
        }

        [Category("Custom")]
        [DefaultValue(17)]
        public double TextHight
        {
            get { return Label.Height; }
            set { Label.Height = value; }
        }

        [Category("Custom")]
        [DefaultValue(9)]
        public double BarHight
        {
            get { return ProgressBar.Height; }
            set { ProgressBar.Height = value; }
        }

        [Category("Custom")]
        public double Hight
        {
            get { return Label.Height + ProgressBar.Height + Label.Padding.Top + Label.Padding.Bottom; }
        }


        private static bool IsValidMeanfulStringValue(object value)
        {
            return !string.IsNullOrEmpty(value as string);
        }
    }
}
