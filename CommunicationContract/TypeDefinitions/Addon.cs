namespace CommunicationContract.TypeDefinitions;

public abstract class Addon
{
    public abstract bool IsConnected { get; }
    
    public abstract AddonAddress Address { get; }
    public abstract Register[] RegisterValues { get; set; }

}