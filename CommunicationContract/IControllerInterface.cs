using CommunicationContract.TypeDefinitions;

namespace CommunicationContract;

public interface IControllerInterface
{
    public Task<AddonAddress[]> GetCurrentConnectedAddons();
    public Task<AddonData> GetAddonData(AddonAddress address);
    public Task<Register> GetRegisterData(AddonAddress address, int registerNumber);

    public Addon AddressToAddon(AddonAddress address);
}

public interface IConnectionInterface
{
    public void Connect(string serialPortName);

    public void Disconnect();

    public bool IsConnected { get; }
}