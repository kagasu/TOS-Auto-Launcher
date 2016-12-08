using Codeplex.Data;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ToSAutoLauncher
{
    public partial class MainWindow : MetroWindow
    {
        private class NGMModuleInfo
        {
            public string Host { get; set; }
            public string Crc { get; set; }
        }

        private class UserInfo
        {
            public string Id{ get; set; }
            public string Password{ get; set; }
            public bool UseOtp { get; set; }
        }
        
        public MainWindow()
        {
            ServicePointManager.Expect100Continue = false;
            InitializeComponent();

            LoadUserInfo();
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            if (textBoxID.Text == "")
            {
                textBoxID.Focus();
            }
            else if (textBoxPassword.Password == "")
            {
                textBoxPassword.Focus();
            }
            else if (textBoxOTP.Text == "")
            {
                textBoxOTP.Focus();
            }
        }

        private void SaveUserInfo(bool useOtp)
        {
            var userInfo = new UserInfo
            {
                Id = textBoxID.Text,
                Password = textBoxPassword.Password,
                UseOtp = useOtp
            };

            File.WriteAllText("userInfo.json", DynamicJson.Serialize(userInfo));
        }

        private void LoadUserInfo()
        {
            if (File.Exists("userInfo.json"))
            {
                var json = (UserInfo)DynamicJson.Parse(File.ReadAllText("userInfo.json"));

                textBoxID.Text = json.Id;
                textBoxPassword.Password = json.Password;

                if(!json.UseOtp)
                {
                    Login();
                }
            }
        }

        private void textBoxOTP_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxOTP.Text.Length == 6)
            {
                SaveUserInfo(true);
                progressbar1.IsIndeterminate = true;
                Task.Run( () =>
                {
                    Login();
                });
            }
        }

        private NGMModuleInfo GetNGMModuleInfo()
        {
            var str = new WebClientEx().DownloadString("http://platform.nexon.co.jp/Auth/NGM/JS/NGMModuleInfo.js");
            var reg = new Regex("host=\"(.*?)\" crc=\"(.*?)\"");

            var host = reg.Matches(str)[0].Groups[1].Value;
            var crc = reg.Matches(str)[0].Groups[2].Value;

            return new NGMModuleInfo() { Host = host, Crc = crc };
        }
        
        private string NexonLogin()
        {
            var wc = new WebClientEx();
            var str = wc.DownloadString("https://login.nexon.co.jp/login/");
            
            var idReg = new Regex("value='' id='(.*?)'");
            var passwordReg = new Regex("type=\"password\" id='(.*?)'");
            
            wc.Headers.Set("Referer", "https://login.nexon.co.jp/login/");
            wc.Headers.Set("User-Agent", "Mozilla");

            wc.UploadValues("https://login.nexon.co.jp/login/login_process1.aspx", new NameValueCollection()
            {
                { idReg.Matches(str)[0].Groups[1].Value, textBoxID.Dispatcher.Invoke(() => textBoxID.Text) },
                { passwordReg.Matches(str)[0].Groups[1].Value, textBoxPassword.Dispatcher.Invoke(() => textBoxPassword.Password) },
                { "onetimepass", textBoxOTP.Dispatcher.Invoke(() => textBoxOTP.Text) },
                { "HiddenUrl", "http://www.nexon.co.jp/" },
                { "otp", "" }
            });

            if (wc.CookieContainer.GetCookieHeader(new Uri("http://nexon.co.jp")).Count() != 0)
            {
                var npp = wc.CookieContainer.GetCookieHeader(new Uri("http://nexon.co.jp")).Split('=')[1];
                return npp;
            }

            return null;
        }

        private void Login()
        {
            var npp = NexonLogin();
            if (npp == null)
            {
                Dispatcher.Invoke(() => this.ShowMessageAsync("Error", "ログインに失敗しました"));
                textBoxOTP.Dispatcher.Invoke(() => textBoxOTP.Text = "");
                progressbar1.Dispatcher.Invoke(() => progressbar1.IsIndeterminate = false);
                return;
            }
            var moduleInfo = GetNGMModuleInfo();
            
            Process.Start($"ngmj://launch/-dll:{moduleInfo.Host}:{moduleInfo.Crc} -locale:JP -mode:launch -game:16818186:0 -token:'{npp}' -passarg:''");
            progressbar1.Dispatcher.Invoke(() => progressbar1.IsIndeterminate = false);
            textBoxOTP.Dispatcher.Invoke(() => textBoxOTP.Text = "");
        }

        private void buttonOtp_Click(object sender, RoutedEventArgs e)
        {
            SaveUserInfo(false);
            progressbar1.IsIndeterminate = true;
            Task.Run(() =>
            {
                Login();
            });
        }
    }
}
