﻿using BepInEx.Configuration;
using LookingGlass.Base;
using MonoMod.RuntimeDetour;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RiskOfOptions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoR2.UI;
using static Rewired.InputMapper;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using LookingGlass.ResizeCommandWindow;
using System.Collections;
using UnityEngine.EventSystems;

namespace LookingGlass.AutoSortItems
{
    internal class AutoSortItemsClass : BaseThing
    {

        public static ConfigEntry<bool> SeperateScrap;
        public static ConfigEntry<bool> SortByTier;
        public static ConfigEntry<bool> DescendingTier;
        public static ConfigEntry<bool> SortByStackSize;
        public static ConfigEntry<bool> DescendingStackSize;
        public static ConfigEntry<bool> SortCommand;
        public static ConfigEntry<bool> SortScrapper;
        public static ConfigEntry<bool> SortCommandDescending;
        public static ConfigEntry<bool> SortScrapperDescending;
        public static ConfigEntry<bool> SortScrapperTier;
        public static ConfigEntry<bool> SortScrapperTierDescending;

        public static ConfigEntry<bool> SortCommandAlphabetical;
        public static ConfigEntry<bool> SortScrapperAlphabetical;
        public static ConfigEntry<bool> SortCommandAlphabeticalDescending;
        public static ConfigEntry<bool> SortScrapperAlphabeticalDescending;

        public static AutoSortItemsClass instance;
        RoR2.UI.ItemInventoryDisplay display;
        List<List<ItemIndex>> itemTierLists = new List<List<ItemIndex>>();
        List<ItemIndex> scrapList = new List<ItemIndex>();
        List<ItemIndex> noTierList = new List<ItemIndex>();
        Dictionary<ItemTier, int> tierMatcher = new Dictionary<ItemTier, int>();
        private static Hook overrideHook;
        bool initialized = false;

        public AutoSortItemsClass()
        {
            Setup();
        }
        public void Setup()
        {

            instance = this;
            SeperateScrap = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Seperate Scrap", true, "Sorts by Scrap");
            SortByTier = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Tier Sort", true, "Sorts by Tier");
            DescendingTier = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Descending Tier Sort", true, "Sorts by Tier Descending");
            SortByStackSize = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Stack Size Sort", true, "Sorts by Stack Size");
            DescendingStackSize = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Descending Stack Size Sort", true, "Sorts by Stack Size Descending");
            SortCommand = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Sort Command Menu", true, "Sorts command menu by stack count");
            SortCommandDescending = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Command Menu Descending", true, "Sorts command menu descending");
            SortScrapper = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Sort Scrapper", true, "Sorts Scrapper by stack count");
            SortScrapperDescending = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Scrapper Descending", true, "Sorts Scrapper by descending");
            SortScrapperTier = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Sort Scrapper Tier", false, "Sorts Scrapper by tier");
            SortScrapperTierDescending = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Scrapper Tier Descending", true, "Sorts Scrapper by descending");

            SortCommandAlphabetical = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Sort Command Menu Alphabetically", false, "Sorts command menu alphabetically");
            SortCommandAlphabeticalDescending = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Command Menu Alphabetically Descending", true, "Sorts command alphabetically descending");
            SortScrapperAlphabetical = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Sort Scrapper Alphabetically", false, "Sorts Scrapper alphabetically");
            SortScrapperAlphabeticalDescending = BasePlugin.instance.Config.Bind<bool>("Auto Sort Items", "Scrapper Alphabetically Descending", true, "Sorts Scrapper alphabetically descending");
            SeperateScrap.SettingChanged += SettingsChanged;
            SortByTier.SettingChanged += SettingsChanged;
            DescendingTier.SettingChanged += SettingsChanged;
            SortByStackSize.SettingChanged += SettingsChanged;
            DescendingStackSize.SettingChanged += SettingsChanged;

            InitHooks();
            SetupRiskOfOptions();
        }
        public void SetupRiskOfOptions()
        {
            ModSettingsManager.AddOption(new CheckBoxOption(SeperateScrap, new CheckBoxConfig() { restartRequired = false }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortByTier, new CheckBoxConfig() { restartRequired = false }));
            ModSettingsManager.AddOption(new CheckBoxOption(DescendingTier, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckTierSort }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortByStackSize, new CheckBoxConfig() { restartRequired = false }));
            ModSettingsManager.AddOption(new CheckBoxOption(DescendingStackSize, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckStackSort }));

            ModSettingsManager.AddOption(new CheckBoxOption(SortCommand, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckNotCommandSortAlphabetical }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortCommandDescending, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckCommandSort }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortScrapper, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckNotScrapperSortTierAlphabetical }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortScrapperDescending, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckScrapperSort }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortScrapperTier, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckNotScrapperSortTierAlphabetical }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortScrapperTierDescending, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckScrapperSortTier }));

            ModSettingsManager.AddOption(new CheckBoxOption(SortCommandAlphabetical, new CheckBoxConfig() { restartRequired = false }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortCommandAlphabeticalDescending, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckCommandSortAlphabetical }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortScrapperAlphabetical, new CheckBoxConfig() { restartRequired = false }));
            ModSettingsManager.AddOption(new CheckBoxOption(SortScrapperAlphabeticalDescending, new CheckBoxConfig() { restartRequired = false, checkIfDisabled = CheckScrapperSortTierAlphabetical }));
        }
        //I'm going cross-eyed looking at all these
        private static bool CheckTierSort()
        {
            return !SortByTier.Value;
        }
        private static bool CheckStackSort()
        {
            return !SortByStackSize.Value;
        }
        private static bool CheckCommandSort()
        {
            return !SortCommand.Value || SortCommandAlphabetical.Value;
        }
        private static bool CheckScrapperSort()
        {
            return !SortScrapper.Value || SortScrapperAlphabetical.Value;
        }
        private static bool CheckScrapperSortTier()
        {
            return !SortScrapperTier.Value || SortScrapperAlphabetical.Value;
        }
        private static bool CheckCommandSortAlphabetical()
        {
            return !SortCommandAlphabetical.Value;
        }
        private static bool CheckScrapperSortTierAlphabetical()
        {
            return !SortScrapperAlphabetical.Value;
        }
        private static bool CheckNotCommandSortAlphabetical()
        {
            return SortCommandAlphabetical.Value;
        }
        private static bool CheckNotScrapperSortTierAlphabetical()
        {
            return SortScrapperAlphabetical.Value;
        }
        internal void SortPickupPicker(PickupPickerController.Option[] options, ReadOnlyCollection<MPButton> elements, Inventory inventory, bool isCommand, PickupPickerPanel panel)
        {
            Dictionary<ItemIndex, GameObject> stuff = new Dictionary<ItemIndex, GameObject>();
            List<ItemIndex> items = new List<ItemIndex>();
            for (int i = 0; i < options.Length; i++)
            {
                ItemIndex itemIndex = PickupCatalog.GetPickupDef(options[i].pickupIndex).itemIndex;
                if (itemIndex == ItemIndex.None)
                {
                    return;
                }
                stuff.Add(itemIndex, elements[i].gameObject);
                items.Add(itemIndex);
            }
            if ((isCommand && SortCommandAlphabetical.Value) || (!isCommand && SortScrapperAlphabetical.Value))
            {
                items.Sort(delegate (ItemIndex index, ItemIndex index2)
                {
                    return Language.GetString(ItemCatalog.GetItemDef(index).nameToken).CompareTo(Language.GetString(ItemCatalog.GetItemDef(index2).nameToken));
                });
                if ((isCommand && !SortCommandAlphabeticalDescending.Value) || (!isCommand && !SortScrapperAlphabeticalDescending.Value))
                {
                    items.Reverse();
                }
            }
            else
            {
                items = new List<ItemIndex>(SortItems(items.ToArray(), items.Count, display, false, isCommand ? false : SortScrapperTier.Value, isCommand ? false : SortScrapperTierDescending.Value, isCommand ? SortCommand.Value : SortScrapper.Value, isCommand ? SortCommandDescending.Value : SortScrapperDescending.Value));
            }
            foreach (var item in items)
            {
                stuff[item].transform.SetAsLastSibling();
            }
            Run.instance.StartCoroutine(ReOrganizeItems(options, panel));
        }
        IEnumerator ReOrganizeItems(PickupPickerController.Option[] options, PickupPickerPanel self)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            List<MPButton> buttons = new List<MPButton>();
            foreach (var item in self.GetComponentInChildren<GridLayoutGroup>().GetComponentsInChildren<MPButton>())
            {
                if (item.gameObject.activeSelf)
                {
                    buttons.Add(item);
                }
            }
            for (int j = 0; j < options.Length; j++)
            {
                MPButton mpbutton = buttons[j];
                //Log.Debug($"{Language.GetString(mpbutton.GetComponent<TooltipProvider>().titleToken)}");
                int num = j - j % self.maxColumnCount;
                int num2 = j % self.maxColumnCount;
                int num3 = num2 - self.maxColumnCount;
                int num4 = num2 - 1;
                int num5 = num2 + 1;
                int num6 = num2 + self.maxColumnCount;
                //Log.Debug($"num[{num}] num2[{num2}] num3[{num3}] num4[{num4}] num5[{num5}] num6[{num6}] ");
                Navigation navigation = mpbutton.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnRight = null;
                navigation.selectOnLeft = null;
                navigation.selectOnUp = null;
                navigation.selectOnDown = null;
                if (num4 >= 0)
                {
                    MPButton mpbutton2 = buttons[num + num4];
                    //Log.Debug($"selectOnLeft :{Language.GetString(mpbutton2.GetComponent<TooltipProvider>().titleToken)}");
                    navigation.selectOnLeft = mpbutton2;
                }
                if (num5 < self.maxColumnCount && num + num5 < options.Length)
                {
                    MPButton mpbutton3 = buttons[num + num5];
                    //Log.Debug($"selectOnRight :{Language.GetString(mpbutton3.GetComponent<TooltipProvider>().titleToken)}");
                    navigation.selectOnRight = mpbutton3;
                }
                if (num + num3 >= 0)
                {
                    MPButton mpbutton4 = buttons[num + num3];
                    //Log.Debug($"selectOnUp :{Language.GetString(mpbutton4.GetComponent<TooltipProvider>().titleToken)}");
                    navigation.selectOnUp = mpbutton4;
                }
                if (num + num6 < options.Length)
                {
                    MPButton mpbutton5 = buttons[num + num6];
                    //Log.Debug($"selectOnDown :{Language.GetString(mpbutton5.GetComponent<TooltipProvider>().titleToken)}");
                    navigation.selectOnDown = mpbutton5;
                }
                mpbutton.navigation = navigation;
            }
            EventSystem.current.SetSelectedGameObject(buttons.First().gameObject);
        }
        void InitHooks()
        {
            var targetMethod = typeof(RoR2.UI.ItemInventoryDisplay).GetMethod(nameof(RoR2.UI.ItemInventoryDisplay.UpdateDisplay), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var destMethod = typeof(AutoSortItemsClass).GetMethod(nameof(UpdateDisplayOverride), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            overrideHook = new Hook(targetMethod, destMethod, this);
        }
        private void UpdateDisplayOverride(Action<RoR2.UI.ItemInventoryDisplay> orig, RoR2.UI.ItemInventoryDisplay self)
        {
            display = self;
            var temp = self.itemOrder;
            try
            {
                if (!initialized)
                {
                    initialized = true;
                    foreach (var tierList in RoR2.ContentManagement.ContentManager.itemTierDefs)
                    {
                        if (tierList.tier.ToString() == "NoTier")
                        {
                            noTierNum = itemTierLists.Count;
                        }
                        tierMatcher.Add(tierList.tier, itemTierLists.Count);
                        itemTierLists.Add(new List<ItemIndex>());
                    }
                }
                self.itemOrder = SortItems(self.itemOrder, self.itemOrderCount, self, SeperateScrap.Value, SortByTier.Value, DescendingTier.Value, SortByStackSize.Value, DescendingStackSize.Value);
            }
            catch (Exception e)
            {
                Log.Debug($"Had issue when sorting items: {e}");
            }
            orig(self);
            self.itemOrder = temp;
        }

        int noTierNum;

        private void SettingsChanged(object sender, EventArgs e)
        {
            try
            {
                if (display)
                {
                    display.UpdateDisplay();
                }
            }
            catch (Exception)
            {
            }
        }

        ItemIndex[] SortItems(ItemIndex[] items, int count, RoR2.UI.ItemInventoryDisplay display, bool seperateScrap, bool sortByTier, bool descendingTier, bool sortByStackSize, bool descendingStackSize) //This really should be refactored but it works so...
        {
            foreach (var tierList in itemTierLists)
            {
                tierList.Clear();
            }
            scrapList.Clear();
            noTierList.Clear();
            ItemIndex[] newArray = new ItemIndex[count];
            List<ItemIndex> allItems = new List<ItemIndex>();
            for (int i = 0; i < count; i++)
            {
                if (seperateScrap && (ItemCatalog.GetItemDef(items[i]).ContainsTag(ItemTag.Scrap) || ItemCatalog.GetItemDef(items[i]).ContainsTag(ItemTag.PriorityScrap) || ItemCatalog.GetItemDef(items[i]).nameToken == "ITEM_REGENERATINGSCRAPCONSUMED_NAME"))
                {
                    scrapList.Add(items[i]);
                }
                else if (sortByTier)
                {
                    if (ItemCatalog.GetItemDef(items[i]).tier == ItemTier.NoTier)
                    {
                        noTierList.Add(items[i]);
                    }
                    else
                    {
                        itemTierLists[tierMatcher[ItemCatalog.GetItemDef(items[i]).tier]].Add(items[i]);
                    }
                }
                else
                {
                    allItems.Add(items[i]);
                    newArray[i] = items[i];
                }
            }
            items = newArray;

            if (sortByTier)
            {
                for (int i = 0; i < itemTierLists.Count; i++)
                {
                    itemTierLists[i] = new List<ItemIndex>(itemTierLists[i].OrderBy((item) => (
                    (int)item)
                    + ((descendingStackSize ? -1 : 1) * (sortByStackSize ? 1 : 0) * display.itemStacks[(int)item] * 20000)).ToArray());
                }
                int num = 0;
                if (seperateScrap)
                {
                    for (int i = 0; i < scrapList.Count; i++)
                    {
                        items[num] = scrapList[i];
                        num++;
                    }
                }
                if (descendingTier)
                {
                    for (int i = itemTierLists.Count - 1; i > -1; i--)
                    {
                        for (int x = 0; x < itemTierLists[i].Count; x++)
                        {
                            items[num] = itemTierLists[i][x];
                            num++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < itemTierLists.Count; i++)
                    {
                        for (int x = 0; x < itemTierLists[i].Count; x++)
                        {
                            items[num] = itemTierLists[i][x];
                            num++;
                        }
                    }
                }
                for (int i = 0; i < noTierList.Count; i++)
                {
                    items[num] = noTierList[i];
                    num++;
                }
            }
            else
            {
                allItems = new List<ItemIndex>(allItems.ToArray().OrderBy((item) =>
                +((descendingStackSize ? -1 : 1) * (sortByStackSize ? 1 : 0) * display.itemStacks[(int)item] * 20000)).ToArray());
                foreach (var item in scrapList)
                {
                    allItems.Insert(0, item);
                }
                items = allItems.ToArray();
            }
            return items;
        }
    }
}
