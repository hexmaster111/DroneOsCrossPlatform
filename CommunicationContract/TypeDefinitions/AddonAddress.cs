namespace CommunicationContract.TypeDefinitions;

public enum AddonAddress
{
    None = 0,
    DebugAddon0 = 0x01,
    DebugAddon1 = 0x02,
    DebugAddon2 = 0x03,
    DebugAddon3 = 0x04,

    VapeController0 = 0x05,
    VapeController1 = 0x06,
    VapeController2 = 0x07,
    VapeController3 = 0x08,

    //MCP Input card reserved | Switch States |
    Mcp23017InputCard0 = 0x20, //000
    Mcp23017InputCard1 = 0x21, //001
    Mcp23017InputCard2 = 0x22, //010
    Mcp23017InputCard3 = 0x23, //011
    Mcp23017InputCard4 = 0x24, //100
    Mcp23017InputCard5 = 0x25, //101
    Mcp23017InputCard6 = 0x26, //110
    Mcp23017InputCard7 = 0x27, //111
    
    Debug = 0xff,
}