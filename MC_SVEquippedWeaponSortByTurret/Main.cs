
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVEquippedWeaponSortByTurretSlot
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.equippedweaponsort";
        public const string pluginName = "SV Sort Equipped Weapons by Turret Slot";
        public const string pluginVersion = "1.1.0";

        private static bool equipUnequipWeaponFlag = false;

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        [HarmonyPatch(typeof(ShipInfo), nameof(ShipInfo.ChangeWeaponSlot))]
        [HarmonyPostfix]
        private static void ShipInfoChangeWeaponSlot_Post(ShipInfo __instance, SpaceShip ___ss)
        {
            DoSort(___ss, __instance);
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.EquipItem))]
        [HarmonyPrefix]
        private static void InventoryEquip_Pre(int ___selectedItem)
        {
            if (___selectedItem > -1 &&
                PlayerControl.inst.GetCargoSystem.cargo[___selectedItem].itemType == 1)
                equipUnequipWeaponFlag = true;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.EquipItem))]
        [HarmonyPostfix]
        private static void InventoryEquip_Post(SpaceShip ___ss, ShipInfo ___shipInfo)
        {
            if (!equipUnequipWeaponFlag)
                return;

            DoSort(___ss, ___shipInfo);
            equipUnequipWeaponFlag = false;
        }

        [HarmonyPatch(typeof(ShipInfo), nameof(ShipInfo.RemoveItem))]
        [HarmonyPrefix]
        private static void ShipInfoRemoveItem_Pre(int ___selItemType)
        {
            if (___selItemType == 1)
                equipUnequipWeaponFlag = true;
        }

        [HarmonyPatch(typeof(ShipInfo), nameof(ShipInfo.RemoveItem))]
        [HarmonyPostfix]
        private static void ShipInfoRemoveItem_Post(ShipInfo __instance, SpaceShip ___ss)
        {
            if (!equipUnequipWeaponFlag)
                return;

            DoSort(___ss, __instance);
            equipUnequipWeaponFlag = false;
        }

        private static void DoSort(SpaceShip ss, ShipInfo shipInfo)
        {
            EquipedWeapon selectedWeapon = null;
            if (shipInfo != null)
            {
                int selIndex = (int)typeof(ShipInfo).GetField("selItemIndex", AccessTools.all).GetValue(shipInfo);
                if (selIndex > 0 && selIndex < ss.shipData.weapons.Count)
                    selectedWeapon = ss.shipData.weapons[selIndex];
            }

            ss.shipData.weapons.Sort((EquipedWeapon x, EquipedWeapon y) =>
            {
                if (x.slotIndex > y.slotIndex)
                    return 1;
                else if (x.slotIndex < y.slotIndex)
                    return -1;
                else
                    return (GameData.data.weaponList[x.weaponIndex].name.CompareTo(
                        GameData.data.weaponList[y.weaponIndex].name));
            });
            ss.LoadAllWeapons(true);
            if (shipInfo != null)
            {
                shipInfo.LoadData(true);
                if (selectedWeapon != null)
                {
                    EquipmentSlot es = null;
                    foreach (Transform t in (Transform)typeof(ShipInfo).GetField("itemPanel", AccessTools.all).GetValue(shipInfo))
                    {
                        es = t.GetComponent<EquipmentSlot>();
                        if (es != null && ss.shipData.weapons[es.itemIndex] == selectedWeapon)
                            break;
                    }
                    if (es != null)
                    {
                        typeof(ShipInfo).GetField("selSlotIndex", AccessTools.all).SetValue(shipInfo, es.slotIndex);
                        typeof(ShipInfo).GetField("selItemIndex", AccessTools.all).SetValue(shipInfo, es.itemIndex);
                    }
                }
            }
        }
    }
}
