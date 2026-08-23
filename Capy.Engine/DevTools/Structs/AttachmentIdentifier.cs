// ==================================================================================
// NOTICE: This file contains modified source code originally created by:
// joker-119, ExMod-Team & Exiled contributors
// Part of the EXILED project.
// Original Project URL: https://github.com/ExMod-Team/EXILED
//
// Modifications: The original structure has been adjusted and formatted to 
// integrate seamlessly with the new extension methods, and original comments 
// have been cleaned up.
//
// This specific file is licensed under the terms of the 
// Creative Commons Attribution-ShareAlike 3.0 Unported (CC BY-SA 3.0) License.
// ==================================================================================

using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using Capy.Engine.DevTools.Extensions;

namespace Capy.Engine.DevTools.Structs;

public readonly struct AttachmentIdentifier(uint code, AttachmentName name, AttachmentSlot slot) {
    public uint Code { get; } = code;
    public AttachmentName Name { get; } = name;
    public AttachmentSlot Slot { get; } = slot;

    public static bool operator ==(AttachmentIdentifier left, AttachmentIdentifier right) => (left.Name == right.Name) && (left.Code == right.Code) && (left.Slot == right.Slot);
    public static bool operator !=(AttachmentIdentifier left, AttachmentIdentifier right) => (left.Name != right.Name) && (left.Code != right.Code) && (left.Slot != right.Slot);
    public static bool operator ==(AttachmentIdentifier left, Attachment right) => (left.Name == right.Name) && (left.Slot == right.Slot);
    public static bool operator !=(AttachmentIdentifier left, Attachment right) => left.Name != right.Name || left.Slot != right.Slot;
    public static bool operator ==(Attachment left, AttachmentIdentifier right) => right == left;
    public static bool operator !=(Attachment left, AttachmentIdentifier right) => right != left;
    public static uint operator +(AttachmentIdentifier left, uint right) => left.Code + right;
    public static uint operator -(AttachmentIdentifier left, uint right) => left.Code - right;
    public static uint operator +(uint left, AttachmentIdentifier right) => right + left;
    public static uint operator -(uint left, AttachmentIdentifier right) => left - right.Code;

    public static bool TryParse(string s, out AttachmentIdentifier identifier) {
        identifier = default;

        foreach (AttachmentIdentifier attId in FirearmExtensions.AvailableAttachments.Values.SelectMany(kvp => kvp.Where(kvp2 => kvp2.Name.ToString() == s))) {
            identifier = attId;
            return true;
        }

        return false;
    }

    public static AttachmentIdentifier Get(ItemType type, AttachmentName name) {
        return FirearmExtensions.AvailableAttachments.TryGetValue(type, out AttachmentIdentifier[] identifiers)
            ? identifiers.FirstOrDefault(identifier => identifier.Name == name)
            : default;
    }

    public static IEnumerable<AttachmentIdentifier> Get(ItemType type, AttachmentSlot slot) {
        return FirearmExtensions.AvailableAttachments.TryGetValue(type, out AttachmentIdentifier[] identifiers)
            ? identifiers.Where(identifier => identifier.Slot == slot)
            : Enumerable.Empty<AttachmentIdentifier>();
    }

    public static bool TryParse(string s, out AttachmentName name) {
        name = default;

        foreach (AttachmentName attachmentNameTranslation in Enum.GetValues(typeof(AttachmentName))) {
            if (attachmentNameTranslation.ToString() != s)
                continue;

            name = attachmentNameTranslation;
            return true;
        }

        return false;
    }

    public override string ToString() => Name.ToString();

    public override int GetHashCode() => base.GetHashCode();

    public override bool Equals(object obj) => base.Equals(obj);
    public bool Equals(Attachment firearmAttachment) => this == firearmAttachment;
    public bool Equals(AttachmentIdentifier attachmentIdentifier) => this == attachmentIdentifier;
}
