// <copyright file="HeroDataTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using FluentAssertions;
using Xunit;

public class HeroDataTests
{
    #region Default Values Tests

    [Fact]
    public void HeroData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var hero = new HeroData();

        // Assert
        hero.Name.Should().BeEmpty();
        hero.HeroId.Should().BeEmpty();
        hero.IsAlive.Should().BeTrue();
        hero.Level.Should().Be(0);
        hero.Age.Should().Be(0);
    }

    [Fact]
    public void HeroData_Attributes_IsNotNull()
    {
        // Arrange & Act
        var hero = new HeroData();

        // Assert
        hero.Attributes.Should().NotBeNull();
    }

    [Fact]
    public void HeroData_Skills_IsNotNull()
    {
        // Arrange & Act
        var hero = new HeroData();

        // Assert
        hero.Skills.Should().NotBeNull();
    }

    [Fact]
    public void HeroData_UnlockedPerks_IsEmpty()
    {
        // Arrange & Act
        var hero = new HeroData();

        // Assert
        hero.UnlockedPerks.Should().BeEmpty();
    }

    [Fact]
    public void HeroData_Equipment_IsNotNull()
    {
        // Arrange & Act
        var hero = new HeroData();

        // Assert
        hero.BattleEquipment.Should().NotBeNull();
        hero.CivilianEquipment.Should().NotBeNull();
        hero.SpareEquipment.Should().NotBeNull();
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void HeroData_CanSetName()
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.Name = "Test Hero";

        // Assert
        hero.Name.Should().Be("Test Hero");
    }

    [Fact]
    public void HeroData_CanSetFirstName()
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.FirstName = "John";

        // Assert
        hero.FirstName.Should().Be("John");
    }

    [Theory]
    [InlineData(Gender.Male)]
    [InlineData(Gender.Female)]
    public void HeroData_CanSetGender(Gender gender)
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.Gender = gender;

        // Assert
        hero.Gender.Should().Be(gender);
    }

    [Theory]
    [InlineData(18)]
    [InlineData(30)]
    [InlineData(60)]
    public void HeroData_CanSetAge(int age)
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.Age = age;

        // Assert
        hero.Age.Should().Be(age);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public void HeroData_CanSetLevel(int level)
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.Level = level;

        // Assert
        hero.Level.Should().Be(level);
    }

    [Fact]
    public void HeroData_CanSetExperience()
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.Experience = 10000;

        // Assert
        hero.Experience.Should().Be(10000);
    }

    [Fact]
    public void HeroData_CanSetIsMainHero()
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.IsMainHero = true;

        // Assert
        hero.IsMainHero.Should().BeTrue();
    }

    [Fact]
    public void HeroData_CanSetIsAlive()
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.IsAlive = false;

        // Assert
        hero.IsAlive.Should().BeFalse();
    }

    #endregion

    #region Points Tests

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void HeroData_CanSetUnspentAttributePoints(int points)
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.UnspentAttributePoints = points;

        // Assert
        hero.UnspentAttributePoints.Should().Be(points);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(50)]
    public void HeroData_CanSetUnspentFocusPoints(int points)
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.UnspentFocusPoints = points;

        // Assert
        hero.UnspentFocusPoints.Should().Be(points);
    }

    #endregion

    #region Perks Tests

    [Fact]
    public void HeroData_CanAddPerks()
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.UnlockedPerks.Add("swift_strike");
        hero.UnlockedPerks.Add("strong_arm");

        // Assert
        hero.UnlockedPerks.Should().HaveCount(2);
        hero.UnlockedPerks.Should().Contain("swift_strike");
    }

    [Fact]
    public void HeroData_Perks_NoDuplicates()
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.UnlockedPerks.Add("swift_strike");
        hero.UnlockedPerks.Add("swift_strike");

        // Assert - HashSet prevents duplicates
        hero.UnlockedPerks.Should().HaveCount(1);
    }

    #endregion

    #region Reference Tests

    [Fact]
    public void HeroData_CanSetClanId()
    {
        // Arrange
        var hero = new HeroData();
        var clanId = MBGUID.Generate(MBGUIDType.Clan);

        // Act
        hero.ClanId = clanId;

        // Assert
        hero.ClanId.Should().Be(clanId);
    }

    [Fact]
    public void HeroData_CanSetPartyId()
    {
        // Arrange
        var hero = new HeroData();
        var partyId = MBGUID.Generate(MBGUIDType.Party);

        // Act
        hero.PartyId = partyId;

        // Assert
        hero.PartyId.Should().Be(partyId);
    }

    [Fact]
    public void HeroData_CanSetFleetId()
    {
        // Arrange
        var hero = new HeroData();
        var fleetId = MBGUID.Generate(MBGUIDType.Hero);

        // Act
        hero.FleetId = fleetId;

        // Assert
        hero.FleetId.Should().Be(fleetId);
    }

    #endregion
}
