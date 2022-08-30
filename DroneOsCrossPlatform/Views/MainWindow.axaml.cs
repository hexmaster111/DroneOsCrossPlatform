using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunicationContract;
using CommunicationContract.TypeDefinitions;
using DroneOsCrossPlatform.DevTools;
using SerialCommunication;

namespace DroneOsCrossPlatform.Views
{
    public partial class MainWindow : Window
    {

        public static IConnectionInterface ConnectionInterface { get; set; } = new HubControllerAdapter();
        
        public MainWindow()
        {
            InitializeComponent();
            
            List<Register> registerData = new List<Register>();

            for (var i = 0; i < 16; i++)
            {
                registerData.Add(DummiesRegisterDataGenerator.Make());
            }

            var a = new AddonData
            {
                Address = (AddonAddress)0xFF,
                RegisterData = registerData
            };
            
            ActiveAddonDataView.SetDataSource(a);
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            //TODO: Dont hard code this...
            ConnectionInterface.Connect("COM3");
        }
    }
}