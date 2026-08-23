using System;
using System.Collections.Generic;
using System.Linq;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Capy.Engine.DevTools.Structs;
using NorthwoodLib.Pools;
using InventorySystem;
using Firearm = Exiled.API.Features.Items.Firearm;

namespace Capy.Engine.DevTools.Extensions;

public static class FirearmExtensions {
    internal static readonly Dictionary<ItemType, uint> BaseCodesValue = [];
    public static IReadOnlyDictionary<ItemType, uint> BaseCodes => BaseCodesValue;

    internal static Dictionary<ItemType, AttachmentIdentifier[]> AvailableAttachmentsValue { get; } = [];
    public static IReadOnlyDictionary<ItemType, AttachmentIdentifier[]> AvailableAttachments => AvailableAttachmentsValue;

    public static void AddAttachment(this Firearm item, AttachmentIdentifier identifier) {
        uint addedCode = identifier.Code == 0
            ? AvailableAttachments[item.Type].FirstOrDefault(attId => attId.Name == identifier.Name).Code
            : identifier.Code;

        uint conflicting = 0;
        uint current = 1;

        foreach (Attachment attachment in item.Base.Attachments) {
            if (attachment.Slot == identifier.Slot && attachment.IsEnabled) {
                conflicting = current;
                break;
            }

            current *= 2;
        }

        uint code = item.Base.ValidateAttachmentsCode((item.Base.GetCurrentAttachmentsCode() & ~conflicting) | addedCode);
        item.Base.ApplyAttachmentsCode(code, false);
        AttachmentCodeSync.ServerSetCode(item.Serial, code);
    }

    public static void AddAttachment(this Firearm item, AttachmentName attachmentName) => item.AddAttachment(AttachmentIdentifier.Get(item.Type, attachmentName));

    public static void AddAttachment(this Firearm item, IEnumerable<AttachmentIdentifier> identifiers) {
        foreach (AttachmentIdentifier identifier in identifiers)
            item.AddAttachment(identifier);
    }

    public static void AddAttachment(this Firearm item, IEnumerable<AttachmentName> attachmentNames) {
        foreach (AttachmentName attachmentName in attachmentNames)
            item.AddAttachment(attachmentName);
    }

    public static void RemoveAttachment(this Firearm item, AttachmentIdentifier identifier) {
        if (!item.Attachments.Any(attachment => (attachment.Name == identifier.Name) && attachment.IsEnabled))
            return;

        uint code = identifier.Code;

        item.Base.ApplyAttachmentsCode(item.Base.GetCurrentAttachmentsCode() & ~code, true);
    }

    public static void RemoveAttachment(this Firearm item, AttachmentName attachmentName) {
        uint code = AttachmentIdentifier.Get(item.Type, attachmentName).Code;

        item.Base.ApplyAttachmentsCode(item.Base.GetCurrentAttachmentsCode() & ~code, true);
    }

    public static void RemoveAttachment(this Firearm item, AttachmentSlot attachmentSlot) {
        Attachment firearmAttachment = item.Attachments.FirstOrDefault(att => (att.Slot == attachmentSlot) && att.IsEnabled);

        if (firearmAttachment is null)
            return;

        uint code = AvailableAttachments[item.Type].FirstOrDefault(attId => attId == firearmAttachment).Code;

        item.Base.ApplyAttachmentsCode(item.Base.GetCurrentAttachmentsCode() & ~code, true);
    }

    public static void RemoveAttachment(this Firearm item, IEnumerable<AttachmentIdentifier> identifiers) {
        foreach (AttachmentIdentifier identifier in identifiers)
            item.RemoveAttachment(identifier);
    }

    public static void RemoveAttachment(this Firearm item, IEnumerable<AttachmentName> attachmentNames) {
        foreach (AttachmentName attachmentName in attachmentNames)
            item.RemoveAttachment(attachmentName);
    }

    public static void RemoveAttachment(this Firearm item, IEnumerable<AttachmentSlot> attachmentSlots) {
        foreach (AttachmentSlot attachmentSlot in attachmentSlots)
            item.RemoveAttachment(attachmentSlot);
    }

    public static void ClearAttachments(this Firearm item) => item.Base.ApplyAttachmentsCode(BaseCodesValue[item.Type], true);

    public static Attachment GetAttachment(this Firearm item, AttachmentIdentifier identifier) => item.Attachments.FirstOrDefault(attachment => attachment == identifier);

    public static bool TryGetAttachment(this Firearm item, AttachmentIdentifier identifier, out Attachment? firearmAttachment) {
        firearmAttachment = null;

        if (!item.Attachments.Any(attachment => attachment.Name == identifier.Name))
            return false;

        firearmAttachment = item.GetAttachment(identifier);

        return true;
    }

    public static bool TryGetAttachment(this Firearm item, AttachmentName attachmentName, out Attachment? firearmAttachment) {
        firearmAttachment = null;

        if (item.Attachments.All(attachment => attachment.Name != attachmentName))
            return false;

        firearmAttachment = item.GetAttachment(AttachmentIdentifier.Get(item.Type, attachmentName));

        return true;
    }

    internal static void GenerateAttachments() {
        foreach (ItemType firearmType in EnumUtils<ItemType>.Values) {
            if (firearmType == ItemType.None)
                continue;

            if (BaseCodesValue.ContainsKey(firearmType) && AvailableAttachmentsValue.ContainsKey(firearmType))
                continue;

            if (!InventoryItemLoader.AvailableItems.TryGetValue(firearmType, out ItemBase itemBase) || itemBase is not InventorySystem.Items.Firearms.Firearm firearm)
                continue;

            List<AttachmentIdentifier>? attachmentIdentifiers = null;
            HashSet<AttachmentSlot>? attachmentsSlots = null;

            try {
                attachmentIdentifiers = ListPool<AttachmentIdentifier>.Shared.Rent();
                attachmentsSlots = HashSetPool<AttachmentSlot>.Shared.Rent();

                uint code = 1;

                foreach (Attachment attachment in firearm.Attachments) {
                    attachmentsSlots.Add(attachment.Slot);
                    attachmentIdentifiers.Add(new(code, attachment.Name, attachment.Slot));
                    code *= 2U;
                }

                uint baseCode = 0;
                foreach (AttachmentSlot slot in attachmentsSlots) {
                    baseCode += attachmentIdentifiers
                        .Where(attachment => attachment.Slot == slot)
                        .Select(attachment => attachment.Code)
                        .DefaultIfEmpty()
                        .Min();
                }

                BaseCodesValue[firearmType] = baseCode;
                AvailableAttachmentsValue[firearmType] = attachmentIdentifiers.ToArray();
            }
            catch (Exception e) {
                Log.Error($"FirearmExtensions.GenerateAttachments: Failed to generate attachments for {firearmType}: {e}");
            }
            finally {
                if (attachmentIdentifiers is not null)
                    ListPool<AttachmentIdentifier>.Shared.Return(attachmentIdentifiers);

                if (attachmentsSlots is not null)
                    HashSetPool<AttachmentSlot>.Shared.Return(attachmentsSlots);
            }
        }
    }
}
