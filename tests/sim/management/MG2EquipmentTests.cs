using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class MG2EquipmentTests
{
    private CampaignState campaign;
    private List<ItemEquippedEvent> equippedEvents;
    private List<ItemUnequippedEvent> unequippedEvents;

    [BeforeTest]
    public void Setup()
    {
        ItemRegistry.Reset();
        campaign = CampaignState.CreateNew();
        
        equippedEvents = new List<ItemEquippedEvent>();
        unequippedEvents = new List<ItemUnequippedEvent>();
        
        campaign.EventBus = new EventBus();
        campaign.EventBus.Subscribe<ItemEquippedEvent>(e => equippedEvents.Add(e));
        campaign.EventBus.Subscribe<ItemUnequippedEvent>(e => unequippedEvents.Add(e));
    }

    // ========================================================================
    // CREWMEMBER EQUIPMENT METHODS
    // ========================================================================

    [TestCase]
    public void CrewMember_GetEquipped_ReturnsNull_WhenEmpty()
    {
        var crew = new CrewMember(1, "Test");

        AssertThat(crew.GetEquipped(EquipSlot.Weapon)).IsNull();
        AssertThat(crew.GetEquipped(EquipSlot.Armor)).IsNull();
        AssertThat(crew.GetEquipped(EquipSlot.Gadget)).IsNull();
    }

    [TestCase]
    public void CrewMember_SetEquipped_Works()
    {
        var crew = new CrewMember(1, "Test");

        crew.SetEquipped(EquipSlot.Weapon, "item_1");
        crew.SetEquipped(EquipSlot.Armor, "item_2");
        crew.SetEquipped(EquipSlot.Gadget, "item_3");

        AssertThat(crew.GetEquipped(EquipSlot.Weapon)).IsEqual("item_1");
        AssertThat(crew.GetEquipped(EquipSlot.Armor)).IsEqual("item_2");
        AssertThat(crew.GetEquipped(EquipSlot.Gadget)).IsEqual("item_3");
    }

    [TestCase]
    public void CrewMember_HasEquipped_Works()
    {
        var crew = new CrewMember(1, "Test");

        AssertThat(crew.HasEquipped(EquipSlot.Weapon)).IsFalse();

        crew.SetEquipped(EquipSlot.Weapon, "item_1");

        AssertThat(crew.HasEquipped(EquipSlot.Weapon)).IsTrue();
    }

    [TestCase]
    public void CrewMember_ClearEquipped_Works()
    {
        var crew = new CrewMember(1, "Test");
        crew.SetEquipped(EquipSlot.Weapon, "item_1");

        crew.ClearEquipped(EquipSlot.Weapon);

        AssertThat(crew.GetEquipped(EquipSlot.Weapon)).IsNull();
        AssertThat(crew.HasEquipped(EquipSlot.Weapon)).IsFalse();
    }

    [TestCase]
    public void CrewMember_GetAllEquippedIds_Works()
    {
        var crew = new CrewMember(1, "Test");
        crew.SetEquipped(EquipSlot.Weapon, "item_1");
        crew.SetEquipped(EquipSlot.Armor, "item_2");

        var ids = crew.GetAllEquippedIds();

        AssertThat(ids.Count).IsEqual(2);
        AssertThat(ids.Contains("item_1")).IsTrue();
        AssertThat(ids.Contains("item_2")).IsTrue();
    }

    [TestCase]
    public void CrewMember_GetAllEquippedIds_EmptyWhenNoneEquipped()
    {
        var crew = new CrewMember(1, "Test");

        var ids = crew.GetAllEquippedIds();

        AssertThat(ids.Count).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_EquipmentFields_DirectAccess()
    {
        var crew = new CrewMember(1, "Test");

        crew.EquippedWeaponId = "w1";
        crew.EquippedArmorId = "a1";
        crew.EquippedGadgetId = "g1";

        AssertThat(crew.EquippedWeaponId).IsEqual("w1");
        AssertThat(crew.EquippedArmorId).IsEqual("a1");
        AssertThat(crew.EquippedGadgetId).IsEqual("g1");
    }

    // ========================================================================
    // CAMPAIGNSTATE EQUIP ITEM
    // ========================================================================

    [TestCase]
    public void EquipItem_Works()
    {
        var crew = campaign.Crew[0];
        var item = campaign.AddItem("rifle", 1);

        var result = campaign.EquipItem(crew.Id, item.Id);

        AssertThat(result).IsTrue();
        AssertThat(crew.GetEquipped(EquipSlot.Weapon)).IsEqual(item.Id);
    }

    [TestCase]
    public void EquipItem_EmitsEvent()
    {
        var crew = campaign.Crew[0];
        var item = campaign.AddItem("rifle", 1);

        campaign.EquipItem(crew.Id, item.Id);

        AssertThat(equippedEvents.Count).IsEqual(1);
        AssertThat(equippedEvents[0].CrewId).IsEqual(crew.Id);
        AssertThat(equippedEvents[0].ItemId).IsEqual(item.Id);
        AssertThat(equippedEvents[0].DefId).IsEqual("rifle");
        AssertThat(equippedEvents[0].Slot).IsEqual("weapon");
    }

    [TestCase]
    public void EquipItem_FailsForNonExistentCrew()
    {
        var item = campaign.AddItem("rifle", 1);

        var result = campaign.EquipItem(999, item.Id);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void EquipItem_FailsForDeadCrew()
    {
        var crew = campaign.Crew[0];
        crew.IsDead = true;
        var item = campaign.AddItem("rifle", 1);

        var result = campaign.EquipItem(crew.Id, item.Id);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void EquipItem_FailsForNonExistentItem()
    {
        var crew = campaign.Crew[0];

        var result = campaign.EquipItem(crew.Id, "nonexistent");

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void EquipItem_FailsForNonEquipment()
    {
        var crew = campaign.Crew[0];
        var item = campaign.AddItem("medkit", 1);

        var result = campaign.EquipItem(crew.Id, item.Id);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void EquipItem_UnequipsPreviousItem()
    {
        var crew = campaign.Crew[0];
        var rifle = campaign.AddItem("rifle", 1);
        var shotgun = campaign.AddItem("shotgun", 1);

        campaign.EquipItem(crew.Id, rifle.Id);
        equippedEvents.Clear();
        unequippedEvents.Clear();

        campaign.EquipItem(crew.Id, shotgun.Id);

        AssertThat(crew.GetEquipped(EquipSlot.Weapon)).IsEqual(shotgun.Id);
        AssertThat(unequippedEvents.Count).IsEqual(1);
        AssertThat(equippedEvents.Count).IsEqual(1);
    }

    [TestCase]
    public void EquipItem_DifferentSlots()
    {
        var crew = campaign.Crew[0];
        var weapon = campaign.AddItem("rifle", 1);
        var armor = campaign.AddItem("light_armor", 1);
        var gadget = campaign.AddItem("scanner", 1);

        campaign.EquipItem(crew.Id, weapon.Id);
        campaign.EquipItem(crew.Id, armor.Id);
        campaign.EquipItem(crew.Id, gadget.Id);

        AssertThat(crew.GetEquipped(EquipSlot.Weapon)).IsEqual(weapon.Id);
        AssertThat(crew.GetEquipped(EquipSlot.Armor)).IsEqual(armor.Id);
        AssertThat(crew.GetEquipped(EquipSlot.Gadget)).IsEqual(gadget.Id);
    }

    // ========================================================================
    // CAMPAIGNSTATE UNEQUIP ITEM
    // ========================================================================

    [TestCase]
    public void UnequipItem_Works()
    {
        var crew = campaign.Crew[0];
        var item = campaign.AddItem("rifle", 1);
        campaign.EquipItem(crew.Id, item.Id);
        equippedEvents.Clear();

        var result = campaign.UnequipItem(crew.Id, EquipSlot.Weapon);

        AssertThat(result).IsTrue();
        AssertThat(crew.GetEquipped(EquipSlot.Weapon)).IsNull();
    }

    [TestCase]
    public void UnequipItem_EmitsEvent()
    {
        var crew = campaign.Crew[0];
        var item = campaign.AddItem("rifle", 1);
        campaign.EquipItem(crew.Id, item.Id);
        unequippedEvents.Clear();

        campaign.UnequipItem(crew.Id, EquipSlot.Weapon);

        AssertThat(unequippedEvents.Count).IsEqual(1);
        AssertThat(unequippedEvents[0].CrewId).IsEqual(crew.Id);
        AssertThat(unequippedEvents[0].Slot).IsEqual("weapon");
    }

    [TestCase]
    public void UnequipItem_FailsForNonExistentCrew()
    {
        var result = campaign.UnequipItem(999, EquipSlot.Weapon);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void UnequipItem_FailsForEmptySlot()
    {
        var crew = campaign.Crew[0];
        // Clear starting equipment first
        campaign.UnequipItem(crew.Id, EquipSlot.Gadget);

        var result = campaign.UnequipItem(crew.Id, EquipSlot.Gadget);

        AssertThat(result).IsFalse();
    }

    // ========================================================================
    // GET EQUIPPED ITEM
    // ========================================================================

    [TestCase]
    public void GetEquippedItem_ReturnsItem()
    {
        var crew = campaign.Crew[0];
        var item = campaign.AddItem("rifle", 1);
        campaign.EquipItem(crew.Id, item.Id);

        var equipped = campaign.GetEquippedItem(crew.Id, EquipSlot.Weapon);

        AssertThat(equipped).IsNotNull();
        AssertThat(equipped.Id).IsEqual(item.Id);
        AssertThat(equipped.DefId).IsEqual("rifle");
    }

    [TestCase]
    public void GetEquippedItem_ReturnsNull_WhenEmpty()
    {
        var crew = campaign.Crew[0];

        // Gadget slot is empty (crew starts with weapon and armor only)
        var equipped = campaign.GetEquippedItem(crew.Id, EquipSlot.Gadget);

        AssertThat(equipped).IsNull();
    }

    [TestCase]
    public void GetEquippedItem_ReturnsNull_ForInvalidCrew()
    {
        var equipped = campaign.GetEquippedItem(999, EquipSlot.Weapon);

        AssertThat(equipped).IsNull();
    }

    // ========================================================================
    // SERIALIZATION
    // ========================================================================

    [TestCase]
    public void CrewMember_GetState_PreservesEquipment()
    {
        var crew = new CrewMember(1, "Test");
        crew.EquippedWeaponId = "w1";
        crew.EquippedArmorId = "a1";
        crew.EquippedGadgetId = "g1";

        var data = crew.GetState();

        AssertThat(data.EquippedWeaponId).IsEqual("w1");
        AssertThat(data.EquippedArmorId).IsEqual("a1");
        AssertThat(data.EquippedGadgetId).IsEqual("g1");
    }

    [TestCase]
    public void CrewMember_FromState_RestoresEquipment()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            EquippedWeaponId = "w1",
            EquippedArmorId = "a1",
            EquippedGadgetId = "g1"
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.EquippedWeaponId).IsEqual("w1");
        AssertThat(crew.EquippedArmorId).IsEqual("a1");
        AssertThat(crew.EquippedGadgetId).IsEqual("g1");
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesEquipment()
    {
        var crew = campaign.Crew[0];
        var weapon = campaign.AddItem("rifle", 1);
        var armor = campaign.AddItem("light_armor", 1);
        campaign.EquipItem(crew.Id, weapon.Id);
        campaign.EquipItem(crew.Id, armor.Id);

        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        var restoredCrew = restored.GetCrewById(crew.Id);
        AssertThat(restoredCrew.EquippedWeaponId).IsEqual(weapon.Id);
        AssertThat(restoredCrew.EquippedArmorId).IsEqual(armor.Id);
    }

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    [TestCase]
    public void EquipItem_SameItemTwice_NoChange()
    {
        var crew = campaign.Crew[0];
        var item = campaign.AddItem("rifle", 1);
        campaign.EquipItem(crew.Id, item.Id);
        equippedEvents.Clear();
        unequippedEvents.Clear();

        // Equip same item again
        var result = campaign.EquipItem(crew.Id, item.Id);

        // Should still succeed (unequips then re-equips)
        AssertThat(result).IsTrue();
        AssertThat(crew.GetEquipped(EquipSlot.Weapon)).IsEqual(item.Id);
    }

    [TestCase]
    public void MultipleCrew_CanEquipSameItemType()
    {
        var crew1 = campaign.Crew[0];
        var crew2 = campaign.Crew[1];
        var rifle1 = campaign.AddItem("rifle", 1);
        var rifle2 = campaign.AddItem("rifle", 1);

        campaign.EquipItem(crew1.Id, rifle1.Id);
        campaign.EquipItem(crew2.Id, rifle2.Id);

        AssertThat(crew1.GetEquipped(EquipSlot.Weapon)).IsEqual(rifle1.Id);
        AssertThat(crew2.GetEquipped(EquipSlot.Weapon)).IsEqual(rifle2.Id);
    }
}
