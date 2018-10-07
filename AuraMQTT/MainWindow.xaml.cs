using System.Windows;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using IWshRuntimeLibrary;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace AuraMQTT
{


    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MqttClient client;
        string clientId;
        AuraConnect auraConnection;

        //variables for notification icon
        private NotifyIcon nIcon = new NotifyIcon();


        public MainWindow()
        {
            InitializeComponent();
            auraConnection = new AuraConnect();

            //Notification Icon
            nIcon.Icon = new Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/icon.ico")).Stream);
            nIcon.Visible = true;
            nIcon.DoubleClick += new System.EventHandler(this.NotifyIcon1_DoubleClick);

            //create Menu
            CreateMenu();

            txtIpAdress.Text = Properties.Settings.Default.IpAdress;
            txtPort.Text = Properties.Settings.Default.Port;
            txtTopic.Text = Properties.Settings.Default.Topic;

            cBoxMinimize.IsChecked = Properties.Settings.Default.checkMinimize;
            cBoxStartWithWindows.IsChecked = Properties.Settings.Default.checkStartWithWindows;
            cBoxAutoSubscribe.IsChecked = Properties.Settings.Default.checkAutoSubscribe;

            if (cBoxMinimize.IsChecked == true)
            {
                this.WindowState = WindowState.Minimized;
                this.Hide();
                this.ShowInTaskbar = false;
            }

            /**
             * TEST
             

            byte[] plaintext;

            // Generate additional entropy (will be used as the Initialization vector)
            byte[] entropy = new byte[20];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(entropy);
            }

            byte[] ciphertext = ProtectedData.Protect(Encoding.ASCII.GetBytes("test"), entropy,
                DataProtectionScope.CurrentUser);
            Properties.Settings.Default.password = ciphertext;

            */


            //get client id from settings
            clientId = Properties.Settings.Default.clientId;

            //if client id is not set, create it and save it.
            if (clientId.Equals(""))
            {
                clientId = Guid.NewGuid().ToString();
                Properties.Settings.Default.clientId = clientId;
                Properties.Settings.Default.Save();
            }

            EstablishMQTTConnection();

        }

        public void CreateMenu()
        {
            ContextMenu contextMenu1 = new ContextMenu();
            MenuItem menuItem1 = new MenuItem();
            MenuItem menuItem2 = new MenuItem();

            contextMenu1.MenuItems.AddRange(new MenuItem[] { menuItem1, menuItem2 });
            menuItem1.Index = 0;
            menuItem1.Text = "Open";
            menuItem1.Click += new System.EventHandler(this.MenuItem1_Click);
            menuItem2.Index = 1;
            menuItem2.Text = "Exit";
            menuItem2.Click += new System.EventHandler(this.MenuItem2_Click);

            nIcon.ContextMenu = contextMenu1;
        }

        private void MenuItem1_Click(object Sender, EventArgs e)
        {
            // Show the MainWindow
            this.Show();
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void MenuItem2_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            this.Close();
        }

        private void NotifyIcon1_DoubleClick(object Sender, EventArgs e)
        {
            // Show the window when the user double clicks on the notify icon.
            if (this.WindowState == WindowState.Minimized)
            {
                this.Show();
                this.ShowInTaskbar = true;
                this.WindowState = WindowState.Normal;
                this.Activate();
            }
        }


        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            switch (this.WindowState)
            {
                case WindowState.Maximized:
                    this.Show();
                    this.ShowInTaskbar = true;
                    this.Activate();
                    break;
                case WindowState.Minimized:
                    if (cBoxMinimize.IsChecked == true)
                    {
                        this.Hide();
                        this.ShowInTaskbar = false;
                        nIcon.ShowBalloonTip(1000, "Minimized to tray, Doubleclick or use menu to re-open window", this.Title, ToolTipIcon.None);
                    }
                    break;
                case WindowState.Normal:
                    this.Show();
                    this.ShowInTaskbar = true;
                    this.Activate();
                    break;
            }

        }

        //establish mqtt connection
        public async void EstablishMQTTConnection()
        {

            if (txtPort.Text.Equals(""))
            {
                client = new MqttClient(txtIpAdress.Text);
            }
            else
            {
                client = new MqttClient(txtIpAdress.Text, int.Parse(txtPort.Text), false, MqttSslProtocols.None, null, null);
            }

            // register a callback-function (we have to implement, see below) which is called by the library when a message was received
            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            string user = txtUsername.Text;
            string pass = txtPassword.Text;

            await Task.Run(() => EstablishMQTTConnectionAsync(this, user, pass));

            if (client.IsConnected && cBoxAutoSubscribe.IsChecked == true)
            {
                SubscribeToTopic();
            }
        }

        // should be called with task, tries to connect to mqtt broker
        internal void EstablishMQTTConnectionAsync(MainWindow gui, string user, string pass)
        {
            try
            {
                if (user.Equals(""))
                {
                    client.Connect(clientId);
                }
                else
                {
                    client.Connect(clientId, user, pass);
                }
                Dispatcher.Invoke(() =>
                {
                    gui.UpdateStatusBar("MQTT connection established", 2);
                });
            }
            catch
            {
                // MessageBox.Show("Could not connect!");
                Dispatcher.Invoke(() =>
                {
                    gui.UpdateStatusBar("MQTT Broker not reachable", 1);
                });
            }
        }

        public void StartWithWindows(bool isChecked)
        {
            /*
             * C:\Users\Tobias\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
             */
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (isChecked)
            {
                WshShell shell = new WshShell();
                string shortcutAddress = startupFolder + @"\AuraMQTT.lnk";
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "A startup shortcut. If you delete this shortcut from your computer, AuraMQTT.exe will not launch on Windows Startup"; // set the description of the shortcut
                shortcut.WorkingDirectory = System.Windows.Forms.Application.StartupPath; /* working directory */
                shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath; /* path of the executable */
                shortcut.Save(); // save the shortcut 
            }
            else
            {
                if (System.IO.File.Exists(Path.Combine(startupFolder, "AuraMQTT.lnk")))
                {
                    System.IO.File.Delete(Path.Combine(startupFolder, "AuraMQTT.lnk"));
                }
            }



        }

        //update status bar with a text
        public void UpdateStatusBar(String text, int color)
        {
            txtStatusBar.Text = text;
            switch (color)
            {
                case 0:
                    txtStatusBar.Foreground = System.Windows.Media.Brushes.Black;
                    break;
                case 1:
                    txtStatusBar.Foreground = System.Windows.Media.Brushes.Red;
                    break;
                case 2:
                    txtStatusBar.Foreground = System.Windows.Media.Brushes.Green;
                    break;
                default:
                    txtStatusBar.Foreground = System.Windows.Media.Brushes.Black;
                    break;
            }

        }

        // this code runs when the main window closes (end of the app)
        protected override void OnClosed(EventArgs e)
        {
            if (client.IsConnected)
            {
                try
                {
                    client.Disconnect();
                }
                catch
                {

                }
            }
            nIcon.Dispose();
            base.OnClosed(e);

            //App.Current.Shutdown();
        }

        // this code runs when a message was received
        void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            int auraReturn = 0;

            Dispatcher.Invoke(delegate
            {              
                String[] split = ReceivedMessage.Split(',');
                if (split.Length == 3)
                {

                    txtReceived.Text = DateTime.Now.ToString("HH:mm") + " R: " + split[0] + " B:" + split[1] + " G:" + split[2];

                    if (int.TryParse(split[0], out int ColorR) && int.TryParse(split[1], out int ColorG) && int.TryParse(split[2], out int ColorB))
                    {
                        if ((0 <= ColorR) && (ColorR <= 255) && (0 <= ColorG) && (ColorG <= 255) && (0 <= ColorB) && (ColorB <= 255))
                        {
                            auraReturn = auraConnection.ChangeColors(ColorR, ColorG, ColorB);
                        }
                        else
                        {
                            txtReceived.Text = DateTime.Now.ToString("HH:mm") + " ERROR: " + ReceivedMessage;
                        }
                    }
                }
                else
                {
                    txtReceived.Text = DateTime.Now.ToString("HH:mm") + " ERROR: " + ReceivedMessage;
                }

                if (auraReturn < 0)
                {
                    txtReceived.Text = "ERROR: AURA_SDK.dll is missing";

                }
                else if (auraReturn == 0)
                {
                    txtReceived.Text = "ERROR: No Moardsboards found";
                }

            });
        }

        public void SubscribeToTopic()
        {
            if (txtTopic.Text != "")
            {
                client.Subscribe(new string[] { txtTopic.Text }, new byte[] { 2 });
                txtReceived.Text = DateTime.Now.ToString("HH:mm") + " Topic subscribed";
            }
            else
            {
                txtReceived.Text = "Topic Missing!!";
            }
        }

        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            SubscribeToTopic();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {

            Properties.Settings.Default.IpAdress = txtIpAdress.Text;
            Properties.Settings.Default.Topic = txtTopic.Text;
            Properties.Settings.Default.checkMinimize = (cBoxMinimize.IsChecked).Value;
            Properties.Settings.Default.checkStartWithWindows = (cBoxStartWithWindows.IsChecked).Value;
            Properties.Settings.Default.checkAutoSubscribe = (cBoxAutoSubscribe.IsChecked).Value;

            if ((cBoxStartWithWindows.IsChecked).Value)
            {
                StartWithWindows(true);
            }
            else
            {
                StartWithWindows(false);
            }
            Properties.Settings.Default.Save();
            System.Windows.MessageBox.Show("Settings Saved");
        }

        private void BtnReconnect_Click(object sender, RoutedEventArgs e)
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
            UpdateStatusBar("Reconnecting...", 0);
            EstablishMQTTConnection();

        }

        /*
         * Checks the port textfield so only numbers will be input
         */
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

    }


}