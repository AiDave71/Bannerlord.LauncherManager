// <copyright file="ModelEntityTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using FluentAssertions;
using Xunit;

public class ModelEntityTests
{
    #region QuestData Tests

    [Fact]
    public void QuestData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var quest = new QuestData();

        // Assert
        quest.QuestId.Should().BeEmpty();
        quest.Title.Should().BeEmpty();
        quest.State.Should().Be(QuestState.NotStarted);
        quest.Progress.Should().Be(0);
    }

    [Fact]
    public void QuestData_CanSetAllProperties()
    {
        // Arrange
        var quest = new QuestData
        {
            Id = MBGUID.Generate(MBGUIDType.Hero),
            QuestId = "quest_001",
            Title = "Test Quest",
            State = QuestState.Active,
            Progress = 50
        };

        // Assert
        quest.QuestId.Should().Be("quest_001");
        quest.Title.Should().Be("Test Quest");
        quest.State.Should().Be(QuestState.Active);
        quest.Progress.Should().Be(50);
    }

    [Theory]
    [InlineData(QuestState.NotStarted)]
    [InlineData(QuestState.Active)]
    [InlineData(QuestState.Completed)]
    [InlineData(QuestState.Failed)]
    [InlineData(QuestState.Cancelled)]
    public void QuestState_AllValues_AreValid(QuestState state)
    {
        // Arrange
        var quest = new QuestData { State = state };

        // Assert
        quest.State.Should().Be(state);
    }

    #endregion

    #region WorkshopData Tests

    [Fact]
    public void WorkshopData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var workshop = new WorkshopData();

        // Assert
        workshop.WorkshopType.Should().BeEmpty();
        workshop.Name.Should().BeEmpty();
        workshop.Capital.Should().Be(0);
        workshop.IsRunning.Should().BeFalse();
        workshop.Efficiency.Should().Be(1.0f);
    }

    [Fact]
    public void WorkshopData_CanSetAllProperties()
    {
        // Arrange
        var workshop = new WorkshopData
        {
            Id = MBGUID.Generate(MBGUIDType.Settlement),
            WorkshopType = "smithy",
            Name = "Test Smithy",
            Capital = 10000,
            LastRunProfit = 500,
            IsRunning = true,
            Efficiency = 1.5f
        };

        // Assert
        workshop.WorkshopType.Should().Be("smithy");
        workshop.Name.Should().Be("Test Smithy");
        workshop.Capital.Should().Be(10000);
        workshop.LastRunProfit.Should().Be(500);
        workshop.IsRunning.Should().BeTrue();
        workshop.Efficiency.Should().Be(1.5f);
    }

    #endregion

    #region CaravanData Tests

    [Fact]
    public void CaravanData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var caravan = new CaravanData();

        // Assert
        caravan.Name.Should().BeEmpty();
        caravan.Gold.Should().Be(0);
        caravan.Goods.Should().BeEmpty();
    }

    [Fact]
    public void CaravanData_CanSetAllProperties()
    {
        // Arrange
        var caravan = new CaravanData
        {
            Id = MBGUID.Generate(MBGUIDType.Party),
            Name = "Test Caravan",
            Gold = 5000
        };

        // Assert
        caravan.Name.Should().Be("Test Caravan");
        caravan.Gold.Should().Be(5000);
    }

    [Fact]
    public void CaravanData_Goods_CanAddItems()
    {
        // Arrange
        var caravan = new CaravanData();

        // Act
        caravan.Goods.Add(new ItemStack { ItemId = "grain", Count = 100 });

        // Assert
        caravan.Goods.Should().HaveCount(1);
    }

    #endregion

    #region SettlementData Tests

    [Fact]
    public void SettlementData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settlement = new SettlementData();

        // Assert
        settlement.SettlementId.Should().BeEmpty();
        settlement.Name.Should().BeEmpty();
        settlement.Prosperity.Should().Be(0);
        settlement.Loyalty.Should().Be(0);
        settlement.Security.Should().Be(0);
    }

    [Fact]
    public void SettlementData_CanSetAllProperties()
    {
        // Arrange
        var settlement = new SettlementData
        {
            Id = MBGUID.Generate(MBGUIDType.Settlement),
            SettlementId = "town_001",
            Name = "Test Town",
            Type = SettlementType.Town,
            Prosperity = 5000,
            Loyalty = 75,
            Security = 80,
            FoodStocks = 100,
            Militia = 200,
            Garrison = 300
        };

        // Assert
        settlement.SettlementId.Should().Be("town_001");
        settlement.Name.Should().Be("Test Town");
        settlement.Type.Should().Be(SettlementType.Town);
        settlement.Prosperity.Should().Be(5000);
        settlement.Loyalty.Should().Be(75);
        settlement.Security.Should().Be(80);
        settlement.FoodStocks.Should().Be(100);
        settlement.Militia.Should().Be(200);
        settlement.Garrison.Should().Be(300);
    }

    [Theory]
    [InlineData(SettlementType.Town)]
    [InlineData(SettlementType.Castle)]
    [InlineData(SettlementType.Village)]
    public void SettlementType_AllValues_AreValid(SettlementType type)
    {
        // Arrange
        var settlement = new SettlementData { Type = type };

        // Assert
        settlement.Type.Should().Be(type);
    }

    #endregion

    #region CampaignData Tests

    [Fact]
    public void CampaignData_DefaultValues_AreEmptyCollections()
    {
        // Arrange & Act
        var campaign = new CampaignData();

        // Assert
        campaign.Heroes.Should().BeEmpty();
        campaign.Parties.Should().BeEmpty();
        campaign.Settlements.Should().BeEmpty();
        campaign.Factions.Should().BeEmpty();
        campaign.Clans.Should().BeEmpty();
        campaign.Kingdoms.Should().BeEmpty();
        campaign.Quests.Should().BeEmpty();
        campaign.Workshops.Should().BeEmpty();
        campaign.Caravans.Should().BeEmpty();
        campaign.Fleets.Should().BeEmpty();
    }

    [Fact]
    public void CampaignData_CanAddHeroes()
    {
        // Arrange
        var campaign = new CampaignData();

        // Act
        campaign.Heroes.Add(new HeroData { Name = "Test Hero" });

        // Assert
        campaign.Heroes.Should().HaveCount(1);
    }

    [Fact]
    public void CampaignData_CanAddParties()
    {
        // Arrange
        var campaign = new CampaignData();

        // Act
        campaign.Parties.Add(new PartyData { Name = "Test Party" });

        // Assert
        campaign.Parties.Should().HaveCount(1);
    }

    #endregion

    #region ItemStack Tests

    [Fact]
    public void ItemStack_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var stack = new ItemStack();

        // Assert
        stack.ItemId.Should().BeEmpty();
        stack.Count.Should().Be(0);
    }

    [Fact]
    public void ItemStack_CanSetProperties()
    {
        // Arrange
        var stack = new ItemStack
        {
            ItemId = "sword",
            ItemName = "Iron Sword",
            Count = 5
        };

        // Assert
        stack.ItemId.Should().Be("sword");
        stack.ItemName.Should().Be("Iron Sword");
        stack.Count.Should().Be(5);
    }

    #endregion

    #region Vec2 Tests

    [Fact]
    public void Vec2_DefaultValues_AreZero()
    {
        // Arrange & Act
        var vec = new Vec2();

        // Assert
        vec.X.Should().Be(0);
        vec.Y.Should().Be(0);
    }

    [Fact]
    public void Vec2_CanSetCoordinates()
    {
        // Arrange
        var vec = new Vec2 { X = 100.5f, Y = 200.5f };

        // Assert
        vec.X.Should().Be(100.5f);
        vec.Y.Should().Be(200.5f);
    }

    #endregion
}
