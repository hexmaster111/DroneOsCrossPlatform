using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunicationContract.TypeDefinitions;
using DroneOsCrossPlatform.ViewModels;

namespace DroneOsCrossPlatform.Views;

public partial class AddonDataViewer : UserControl
{
    public AddonDataViewer()
    {
        InitializeComponent();
    }


    public void SetDataSource(AddonData addonData)
    {
        var a = new AddonDataViewModel(addonData);

        this.DataContext = a;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}