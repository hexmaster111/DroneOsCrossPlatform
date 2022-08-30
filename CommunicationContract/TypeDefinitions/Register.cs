namespace CommunicationContract.TypeDefinitions;

public abstract class Register
{
    public abstract AddonAddress FromAddon { get; init; }
    public abstract string RegisterName { get; init; }
    public abstract Action<byte> NewValue { get; set; }


    /// <summary>
    /// Register Array ID
    /// </summary>
    public abstract int RegisterAddress { get; init; }
}