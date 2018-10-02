using System.Windows;
using AuraSDKDotNet;
using Color = AuraSDKDotNet.Color;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace AuraMQTT
{


    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MqttClient client;
        string clientId;
        AuraConnect auraConnection = new AuraConnect();

        //variables for notification icon
        private NotifyIcon nIcon = new NotifyIcon();
        private ContextMenu contextMenu1;
        private MenuItem menuItem1;
        private MenuItem menuItem2;

        public MainWindow()
        {
            InitializeComponent();

            //Notification Icon
            nIcon.Icon = new Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/icon.ico")).Stream);
            nIcon.Visible = true;
            nIcon.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);

            //create Menu
            createMenu();

            MQTTBroker.Text = Properties.Settings.Default.IpAdress;
            txtTopic.Text = Properties.Settings.Default.Topic;

            string BrokerAddress = "192.168.160.20";

            client = new MqttClient(BrokerAddress);
            

            // register a callback-function (we have to implement, see below) which is called by the library when a message was received
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            //get client id from settings
            clientId = Properties.Settings.Default.clientId;

            //if client id is not set, create it and save it.
            if (clientId.Equals(""))
            {
                clientId = Guid.NewGuid().ToString();
                Properties.Settings.Default.clientId = clientId;
                Properties.Settings.Default.Save();
            }

            establishMQTTConnection();

        }

        public void createMenu()
        {

            this.contextMenu1 = new ContextMenu();
            this.menuItem1 = new MenuItem();
            this.menuItem2 = new MenuItem();

            this.contextMenu1.MenuItems.AddRange(new MenuItem[] { this.menuItem1, this.menuItem2 });
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "Open";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "Exit";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);

            nIcon.ContextMenu = this.contextMenu1;
        }

        private void menuItem1_Click(object Sender, EventArgs e)
        {
            // Show the MainWindow
            this.Show();
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void menuItem2_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            this.Close();
        }

        private void notifyIcon1_DoubleClick(object Sender, EventArgs e)
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
                    this.Hide();
                    this.ShowInTaskbar = false;
                    nIcon.ShowBalloonTip(1000, "Minimized to Tray", this.Title, ToolTipIcon.None);
                    break;
                case WindowState.Normal:
                    this.Show();
                    this.ShowInTaskbar = true;
                    this.Activate();
                    break;
            }

        }

        //establish mqtt connection
        public async void establishMQTTConnection()
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
                    gui.updateStatusBar("MQTT connection established");
                }); 
            }
            catch
            {
                // MessageBox.Show("Could not connect!");
                Dispatcher.Invoke(() =>
                {
                    gui.updateStatusBar("No connection, MQTT Server does not respond");
                });
            }
        }

        //update status bar with a text
        public void updateStatusBar(String text)
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
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);

            Dispatcher.Invoke(delegate
            {              // we need this construction because the receiving code in the library and the UI with textbox run on different threads
                String[] split = ReceivedMessage.Split(',');
                if (split.Length == 3)
                {
                    int ColorR;
                    int ColorG;
                    int ColorB;

                    txtReceived.Text = split[0] + " " + split[1] + " " + split[2];


                    if (int.TryParse(split[0], out ColorR) && int.TryParse(split[1], out ColorG) && int.TryParse(split[2], out ColorB))
                    {
                        auraConnection.ChangeColors(ColorR, ColorG, ColorB);

                    }
                }
                else
                {
                    txtReceived.Text = "wrong input";
                }

              /*  if (ReceivedMessage.Equals("ON"))
                {
                    auraConnection.ChangeColors();
                }
                */                
            });
        }

        // this code runs when the button "Subscribe" is clicked
        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (txtTopic.Text != "")
            {
                // whole topic
                string Topic = "/home/" + txtTopic.Text;

                // subscribe to the topic with QoS 2
                client.Subscribe(new string[] { Topic }, new byte[] { 2 });   // we need arrays as parameters because we can subscribe to different topics with one call
                txtReceived.Text = "";
            }
            else
            {
                System.Windows.MessageBox.Show("You have to enter a topic to subscribe!");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            
            Properties.Settings.Default.IpAdress = MQTTBroker.Text;
            Properties.Settings.Default.Topic = txtTopic.Text;
            Properties.Settings.Default.Save();
            System.Windows.MessageBox.Show("Settings Saved");
        }

    }
       
}