using CommunicationContract.TypeDefinitions;

namespace DroneOsCrossPlatform.DevTools;

public static class DummiesRegisterDataGenerator
{
    private static byte _lastRegister = 0;


    public class DebugRegister : Register
    {
        public override AddonAddress FromAddon
        {
            get => AddonAddress.Debug;
            init => throw new NotImplementedException();
        }

        public override string RegisterName
        {
            get => null;
            init => throw new NotImplementedException();
        }

        //This action will never be called by the debugger
        public override Action<byte> NewValue { get; set; }

        public override int RegisterAddress
        {
            get => _lastRegister++;
            init => throw new NotImplementedException();
        }
    }


    public static Register Make()
    {
        return new DebugRegister();
    }
}