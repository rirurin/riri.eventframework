using RyoTune.Persona3Reload.Types;

namespace p3rpc.eventframework;

public static class LocalTypeExtensions
{
    public static uint GetEvtHash(this FAtlEvtPreData self)
        => GetEvtPreDataHash((EAtlEvtEventCategoryType)self.EventCategoryTypeID, (uint)self.EventMajorID,
            (uint)self.EventMinorID);

    public static uint GetEvtPreDataHash(EAtlEvtEventCategoryType category, uint major, uint minor) // FUN_141097f20
    {
        uint uVar3 = major - minor ^ (uint)minor >> 0xd;
        uint uVar1 = (uint)(-0x61c88647 - uVar3) - minor ^ uVar3 << 8;
        uint uVar4 = (minor - uVar1) - uVar3 ^ uVar1 >> 0xd;
        uVar3 = (uVar3 - uVar1) - uVar4 ^ uVar4 >> 0xc;
        uVar1 = (uVar1 - uVar3) - uVar4 ^ uVar3 << 0x10;
        uVar4 = (uVar4 - uVar1) - uVar3 ^ uVar1 >> 5;
        uVar3 = (uVar3 - uVar1) - uVar4 ^ uVar4 >> 3;
        uVar1 = (uVar1 - uVar3) - uVar4 ^ uVar3 << 10;
        uVar1 = (uVar4 - uVar1) - uVar3 ^ uVar1 >> 0xf;
        uint iVar2 = uVar1 - (uint)category;
        uVar3 = 0x9e3779b9 - uVar1 ^ iVar2 * 0x100;
        uVar4 = ((uint)category - uVar3) - iVar2 ^ uVar3 >> 0xd;
        uVar1 = (iVar2 - uVar3) - uVar4 ^ uVar4 >> 0xc;
        uVar3 = (uVar3 - uVar1) - uVar4 ^ uVar1 << 0x10;
        uVar4 = (uVar4 - uVar3) - uVar1 ^ uVar3 >> 5;
        uVar1 = (uVar1 - uVar3) - uVar4 ^ uVar4 >> 3;
        uVar3 = (uVar3 - uVar1) - uVar4 ^ uVar1 << 10;
        return (uVar4 - uVar3) - uVar1 ^ uVar3 >> 0xf;
    }
}