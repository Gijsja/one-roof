using System;
using System.Collections.Generic;
using OneRoof.Content;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace OneRoof.Editor.AssetLab
{
    /// <summary>Editor-only validation gate for content IDs, Resources assets, NPC rig dimensions, and anchors.</summary>
    public static class AssetLabValidator
    {
        public static IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            ValidateProps(errors, ids);
            ValidateResidents(errors, ids);
            ValidateEmotes(errors, ids);
            ValidateRig(errors);
            return errors;
        }

        [MenuItem("One Roof/AssetLab/Validate Content")]
        public static void ValidateMenu()
        {
            var errors = Validate();
            if (errors.Count == 0) Debug.Log("AssetLab validation passed.");
            else foreach (var error in errors) Debug.LogError(error);
        }

        [MenuItem("One Roof/AssetLab/Configure Addressables")]
        public static void ConfigureAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) settings = AddressableAssetSettings.Create("Assets/AddressableAssetsData", "AddressableAssetSettings", true, true);
            AddFolder(settings, "Assets/OneRoof/Runtime/Content/Resources/Architecture", "architecture");
            AddFolder(settings, "Assets/OneRoof/Runtime/Content/Resources/Props", "props");
            AddFolder(settings, "Assets/OneRoof/Runtime/Content/Resources/Residents", "residents");
            AddFolder(settings, "Assets/OneRoof/Runtime/Content/Resources/Emotes", "emotes");
            AddFolder(settings, "Assets/OneRoof/Runtime/Content/Resources/Rooms", "rooms");
            AssetDatabase.SaveAssets();
            Debug.Log("AssetLab Addressables groups configured.");
        }

        public static void BuildAddressables() { AddressableAssetSettings.BuildPlayerContent(out _); }

        public static void ConfigureAndBuildAddressables()
        {
            ConfigureAddressables();
            BuildAddressables();
        }

        private static void ValidateProps(List<string> errors, HashSet<string> ids)
        {
            foreach (var record in PropContentRegistry.GetAll())
            {
                ValidateRecord(errors, ids, record.ContentId, record.ResourcePath);
                if (record.InteractionPoints.Count == 0 && record.CollisionMask != "solid") errors.Add($"{record.ContentId} has an unsupported collision declaration.");
            }
        }

        private static void ValidateResidents(List<string> errors, HashSet<string> ids)
        {
            foreach (var record in NpcContentRegistry.AllRecords)
            {
                ValidateRecord(errors, ids, record.ContentId, record.ResourcePath);
                if (record.InteractionPoints.Count == 0) errors.Add($"{record.ContentId} has no interaction anchors.");
                if (record.WorldWidth <= 0f || record.WorldHeight <= 0f) errors.Add($"{record.ContentId} has invalid rig dimensions.");
            }
        }

        private static void ValidateEmotes(List<string> errors, HashSet<string> ids)
        {
            foreach (var record in EmoteContentRegistry.AllRecords) ValidateRecord(errors, ids, record.ContentId, record.ResourcePath);
        }

        private static void ValidateRecord(List<string> errors, HashSet<string> ids, string id, string resourcePath)
        {
            if (string.IsNullOrEmpty(id) || !ids.Add(id)) errors.Add($"Duplicate or empty content ID: {id}");
            if (Resources.Load<UnityEngine.Object>(resourcePath) == null) errors.Add($"{id} is missing Resources asset '{resourcePath}'.");
        }

        private static void ValidateRig(List<string> errors)
        {
            if (NpcRigDefinition.LayerRenderingOrder.Count != 8) errors.Add("NPC rig must provide exactly eight wardrobe layers.");
            if (NpcRigDefinition.PivotY != 0f || NpcRigDefinition.NominalWorldHeight <= 0f) errors.Add("NPC rig has invalid feet anchor or scale.");
        }

        private static void AddFolder(AddressableAssetSettings settings, string folder, string groupName)
        {
            var group = settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, false, null);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = AssetDatabase.GUIDToAssetPath(guid).Replace("Assets/OneRoof/Runtime/Content/Resources/", string.Empty).Replace(".png", string.Empty);
            }
        }
    }
}
