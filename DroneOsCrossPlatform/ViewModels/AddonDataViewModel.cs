using System.Collections.Generic;
using CommunicationContract.TypeDefinitions;

namespace DroneOsCrossPlatform.ViewModels;

public class AddonDataViewModel : ViewModelBase
{
    public AddonDataViewModel(AddonData addonData)
    {
        AddonData = addonData;
    }

    public AddonData AddonData { get; set; }
    
}