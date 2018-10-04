using System.Windows;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

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
            txtTopic.Text = Properties.Settings.Default.Topic;
            cBoxMinimize.IsChecked = Properties.Settings.Default.checkMinimize;

            txtStatusBar.Text = "";
            txtPort.Text = "";
            txtPassword.Text = "";
            txtUsername.Text = "";

            string BrokerAddress = "192.168.160.20";

            client = new MqttClient(BrokerAddress);
            

            // register a callback-function (we have to implement, see below) which is called by the library when a message was received
            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

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
                        nIcon.ShowBalloonTip(1000, "Minimized to Tray, Doubleclick Item to re-open window", this.Title, ToolTipIcon.None);
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
            await Task.Run(() => ExecuteLongProcedureAsync(this));
        }

        // should be called with task, tries to connect to mqtt broker
        internal void ExecuteLongProcedureAsync(MainWindow gui)
        {
            try
            {
                client.Connect(clientId);
                Dispatcher.Invoke(() =>
                {
                    gui.UpdateStatusBar("MQTT connection established");
                }); 
            }
            catch
            {
                // MessageBox.Show("Could not connect!");
                Dispatcher.Invoke(() =>
                {
                    gui.UpdateStatusBar("No connection, MQTT Server does not respond");
                });
            }
        }

        //update status bar with a text
        public void UpdateStatusBar(String text)
        {
            txtStatusBar.Text = text;
        }

        // this code runs when the main window closes (end of the app)
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                client.Disconnect();
            } catch
            {

            }

            base.OnClosed(e);
            nIcon.Dispose();
            App.Current.Shutdown();
        }

        // this code runs when a message was received
        void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);

            Dispatcher.Invoke(delegate
            {              // we need this construction because the receiving code in the library and the UI with textbox run on different threads
                String[] split = ReceivedMessage.Split(',');
                if (split.Length == 3)
                {

                    txtReceived.Text = DateTime.Now.ToString("HH:mm") + " R: " + split[0] + "B: " + split[1] + "G: " + split[2];
                    //txtReceived.Text = split[0] + " " + split[1] + " " + split[2];


                    if (int.TryParse(split[0], out int ColorR) && int.TryParse(split[1], out int ColorG) && int.TryParse(split[2], out int ColorB))
                    {
                        if ((0 <= ColorR) && (ColorR <= 255) && (0 <= ColorG) && (ColorG <= 255) && (0 <= ColorB) && (ColorB <= 255))
                        {
                            auraConnection.ChangeColors(ColorR, ColorG, ColorB);
                        } else
                        {
                            txtReceived.Text = DateTime.Now.ToString("HH:mm") + " ERROR: " + ReceivedMessage;
                        }
                        

                    } 
                }
                else
                {
                    txtReceived.Text = DateTime.Now.ToString("HH:mm") + " ERROR: " + ReceivedMessage;
                }

            
            });
        }

        // this code runs when the button "Subscribe" is clicked
        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (txtTopic.Text != "")
            {
                // subscribe to the topic with QoS 2
                client.Subscribe(new string[] { txtTopic.Text }, new byte[] { 2 });   // we need arrays as parameters because we can subscribe to different topics with one call
                txtReceived.Text = DateTime.Now.ToString("HH:mm") + " Topic subscribed";
            }
            else
            {
                System.Windows.MessageBox.Show("You have to enter a topic to subscribe!");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            
            Properties.Settings.Default.IpAdress = txtIpAdress.Text;
            Properties.Settings.Default.Topic = txtTopic.Text;
            Properties.Settings.Default.checkMinimize = (cBoxMinimize.IsChecked).Value;
            Properties.Settings.Default.Save();
            System.Windows.MessageBox.Show("Settings Saved");
        }

    }
       
}