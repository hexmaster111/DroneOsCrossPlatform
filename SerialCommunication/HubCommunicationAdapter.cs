using CommunicationContract;
using CommunicationContract.TypeDefinitions;

namespace SerialCommunication;

public class HubCommunicationAdapter : IControllerInterface
{
    public Task<AddonAddress[]> GetCurrentConnectedAddons()
    {
        //Ask the controller for every connected addon, accumulate the results and return them.

        throw new NotImplementedException();
    }

    public Task<AddonData> GetAddonData(AddonAddress address)
    {
        //Ask the controller for every register on the addon and return the data.

        throw new NotImplementedException();
    }

    public Task<Register> GetRegisterData(AddonAddress address, int registerNumber)
    {
        //Build and give out the register data.


        throw new NotImplementedException();
    }

    public Addon AddressToAddon(AddonAddress address)
    {
        //Build an addon and send it out

        throw new NotImplementedException();
    }
}

public class HubControllerAdapter : IConnectionInterface
{
    public static HubComs HubComs = new();

    /// <summary>
    /// Connect to a serial port
    /// </summary>
    /// <param name="serialPortName">Name of the port (COM3) (/dev/ttyACM0)</param>
    public void Connect(string serialPortName)
    {
        if (serialPortName == null)
            throw new ArgumentNullException(nameof(serialPortName));

        HubComs.Open(serialPortName, 115200);
    }

    /// <summary>
    /// Disconnects from the port
    /// </summary>
    public void Disconnect()
    {
        if (HubComs.IsConnected)
        {
            HubComs.Close();
        }
    }

    public bool IsConnected => HubComs.IsConnected;
}