using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
// Client::Graphics::Scene::Human
//   Client::Graphics::Scene::CharacterBase
//     Client::Graphics::Scene::DrawObject
//       Client::Graphics::Scene::Object

// size = 0xA80
// ctor E8 ?? ?? ?? ?? 48 8B F8 48 85 C0 74 28 48 8D 55 D7
[StructLayout(LayoutKind.Explicit, Size = 0xA80)]
public unsafe partial struct Human
{
    [FieldOffset(0x0)] public CharacterBase CharacterBase;
    [FieldOffset(0x8F0)] public fixed byte CustomizeData[0x1A];
    [FieldOffset(0x8F0)] public CustomizeData Customize;
    [Obsolete("Use Customize instead")] [FieldOffset(0x8F0)] public byte Race;
    [Obsolete("Use Customize instead")] [FieldOffset(0x8F1)] public byte Sex;
    [Obsolete("Use Customize instead")] [FieldOffset(0x8F2)] public byte BodyType;
    [Obsolete("Use Customize instead")] [FieldOffset(0x8F4)] public byte Clan;
    [Obsolete("Use Customize instead")] [FieldOffset(0x904)] public byte LipColorFurPattern;
    [Obsolete("Use Customize instead")] [FieldOffset(0x90C)] public uint SlotNeedsUpdateBitfield;
    [FieldOffset(0x910)] public fixed byte EquipSlotData[4 * 0xA];
    [FieldOffset(0x910)] public EquipmentModelId Head;
    [Obsolete("Use Head instead")] [FieldOffset(0x910)] public short HeadSetID;
    [Obsolete("Use Head instead")] [FieldOffset(0x912)] public byte HeadVariantID;
    [Obsolete("Use Head instead")] [FieldOffset(0x913)] public byte HeadDyeID;
    [FieldOffset(0x914)] public EquipmentModelId Top;
    [Obsolete("Use Top instead")] [FieldOffset(0x914)] public short TopSetID;
    [Obsolete("Use Top instead")] [FieldOffset(0x916)] public byte TopVariantID;
    [Obsolete("Use Top instead")] [FieldOffset(0x917)] public byte TopDyeID;
    [FieldOffset(0x918)] public EquipmentModelId Arms;
    [Obsolete("Use Arms instead")] [FieldOffset(0x918)] public short ArmsSetID;
    [Obsolete("Use Arms instead")] [FieldOffset(0x91A)] public byte ArmsVariantID;
    [Obsolete("Use Arms instead")] [FieldOffset(0x91B)] public byte ArmsDyeID;
    [FieldOffset(0x91C)] public EquipmentModelId Legs;
    [Obsolete("Use Legs instead")] [FieldOffset(0x91C)] public short LegsSetID;
    [Obsolete("Use Legs instead")] [FieldOffset(0x91E)] public byte LegsVariantID;
    [Obsolete("Use Legs instead")] [FieldOffset(0x91F)] public byte LegsDyeID;
    [FieldOffset(0x920)] public EquipmentModelId Feet;
    [Obsolete("Use Feet instead")] [FieldOffset(0x920)] public short FeetSetID;
    [Obsolete("Use Feet instead")] [FieldOffset(0x922)] public byte FeetVariantID;
    [Obsolete("Use Feet instead")] [FieldOffset(0x923)] public byte FeetDyeID;
    [FieldOffset(0x924)] public EquipmentModelId Ear;
    [Obsolete("Use Ear instead")] [FieldOffset(0x924)] public short EarSetID;
    [Obsolete("Use Ear instead")] [FieldOffset(0x926)] public byte EarVariantID;
    [FieldOffset(0x928)] public EquipmentModelId Neck;
    [Obsolete("Use Neck instead")] [FieldOffset(0x928)] public short NeckSetID;
    [Obsolete("Use Neck instead")] [FieldOffset(0x92A)] public byte NeckVariantID;
    [FieldOffset(0x92C)] public EquipmentModelId Wrist;
    [Obsolete("Use Wrist instead")] [FieldOffset(0x92C)] public short WristSetID;
    [Obsolete("Use Wrist instead")] [FieldOffset(0x92E)] public byte WristVariantID;
    [FieldOffset(0x930)] public EquipmentModelId RFinger;
    [Obsolete("Use RFinger instead")] [FieldOffset(0x930)] public short RFingerSetID;
    [Obsolete("Use RFinger instead")] [FieldOffset(0x932)] public byte RFingerVariantID;
    [FieldOffset(0x934)] public EquipmentModelId LFinger;
    [Obsolete("Use LFinger instead")] [FieldOffset(0x934)] public short LFingerSetID;
    [Obsolete("Use LFinger instead")] [FieldOffset(0x936)] public byte LFingerVariantID;
    [FieldOffset(0x938)] public ushort RaceSexId; // cXXXX ID (0101, 0201, etc)
    [FieldOffset(0x93A)] public ushort HairId; // hXXXX 
    [FieldOffset(0x93C)] public ushort FaceId; // fXXXX ID
    [FieldOffset(0x93E)] public ushort TailEarId; // tXXXX/zXXXX(viera)
    [FieldOffset(0x940)] public ushort FurId;

    [FieldOffset(0xA38)] public byte* ChangedEquipData;

    [MemberFunction("48 8B ?? 53 55 57 48 83 ?? ?? 48 8B")]
    public partial byte SetupVisor(ushort modelId, byte visorState);

    // Updates the customize array and, if not skipEquipment the equip array.
    // data needs to be 26 bytes if not skipEquipment and 66 bytes otherwise.
    // Returns false and does nothing if the given race, gender or body type is not equal to the current one, 
    // or if the race is Hyur and one clan is Highlander and the other Midlander.
    [MemberFunction("E8 ?? ?? ?? ?? 41 0F B6 C5 66 41 89 86")]
    public partial bool UpdateDrawData(byte* data, bool skipEquipment);

    [MemberFunction("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B F9 48 8B EA 48 81 C1")]
    public partial bool SetupFromCharacterData(byte* data);
}