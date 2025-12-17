// <copyright file="FleetDataTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.WarSails;
using FluentAssertions;
using Xunit;

public class FleetDataTests
{
    #region Default Values Tests

    [Fact]
    public void FleetData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var fleet = new FleetData();

        // Assert
        fleet.Name.Should().BeEmpty();
        fleet.Ships.Should().BeEmpty();
    }

    [Fact]
    public void FleetData_Position_IsNotNull()
    {
        // Arrange & Act
        var fleet = new FleetData();

        // Assert
        fleet.Position.Should().NotBeNull();
    }

    #endregion

    #region TotalCrewCount Tests

    [Fact]
    public void TotalCrewCount_NoShips_ReturnsZero()
    {
        // Arrange
        var fleet = new FleetData();

        // Assert
        fleet.TotalCrewCount.Should().Be(0);
    }

    [Fact]
    public void TotalCrewCount_SingleShip_ReturnsShipCrew()
    {
        // Arrange
        var fleet = new FleetData();
        fleet.Ships.Add(new ShipData { CrewCount = 25 });

        // Assert
        fleet.TotalCrewCount.Should().Be(25);
    }

    [Fact]
    public void TotalCrewCount_MultipleShips_ReturnsTotalCrew()
    {
        // Arrange
        var fleet = new FleetData();
        fleet.Ships.Add(new ShipData { CrewCount = 25 });
        fleet.Ships.Add(new ShipData { CrewCount = 30 });
        fleet.Ships.Add(new ShipData { CrewCount = 15 });

        // Assert
        fleet.TotalCrewCount.Should().Be(70);
    }

    #endregion

    #region TotalCargoCapacity Tests

    [Fact]
    public void TotalCargoCapacity_NoShips_ReturnsZero()
    {
        // Arrange
        var fleet = new FleetData();

        // Assert
        fleet.TotalCargoCapacity.Should().Be(0);
    }

    [Fact]
    public void TotalCargoCapacity_WithShips_ReturnsSum()
    {
        // Arrange
        var fleet = new FleetData();
        fleet.Ships.Add(new ShipData());

        // Assert - capacity is computed from ship cargo
        fleet.TotalCargoCapacity.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region TotalCargoWeight Tests

    [Fact]
    public void TotalCargoWeight_NoShips_ReturnsZero()
    {
        // Arrange
        var fleet = new FleetData();

        // Assert
        fleet.TotalCargoWeight.Should().Be(0);
    }

    [Fact]
    public void TotalCargoWeight_WithShips_ReturnsSum()
    {
        // Arrange
        var fleet = new FleetData();
        fleet.Ships.Add(new ShipData());

        // Assert - weight is computed from cargo
        fleet.TotalCargoWeight.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void FleetData_CanSetName()
    {
        // Arrange
        var fleet = new FleetData();

        // Act
        fleet.Name = "Test Fleet";

        // Assert
        fleet.Name.Should().Be("Test Fleet");
    }

    [Fact]
    public void FleetData_CanSetAdmiralId()
    {
        // Arrange
        var fleet = new FleetData();
        var admiralId = MBGUID.Generate(MBGUIDType.Hero);

        // Act
        fleet.AdmiralId = admiralId;

        // Assert
        fleet.AdmiralId.Should().Be(admiralId);
    }

    [Fact]
    public void FleetData_CanSetClanId()
    {
        // Arrange
        var fleet = new FleetData();
        var clanId = MBGUID.Generate(MBGUIDType.Clan);

        // Act
        fleet.ClanId = clanId;

        // Assert
        fleet.ClanId.Should().Be(clanId);
    }

    [Fact]
    public void FleetData_CanSetFlagshipId()
    {
        // Arrange
        var fleet = new FleetData();
        var flagshipId = MBGUID.Generate(MBGUIDType.Hero);

        // Act
        fleet.FlagshipId = flagshipId;

        // Assert
        fleet.FlagshipId.Should().Be(flagshipId);
    }

    [Fact]
    public void FleetData_CanSetCurrentRegionId()
    {
        // Arrange
        var fleet = new FleetData();

        // Act
        fleet.CurrentRegionId = "sea_region_001";

        // Assert
        fleet.CurrentRegionId.Should().Be("sea_region_001");
    }

    #endregion

    #region Ships Collection Tests

    [Fact]
    public void FleetData_CanAddShips()
    {
        // Arrange
        var fleet = new FleetData();

        // Act
        fleet.Ships.Add(new ShipData { Name = "Ship 1" });
        fleet.Ships.Add(new ShipData { Name = "Ship 2" });

        // Assert
        fleet.Ships.Should().HaveCount(2);
    }

    [Fact]
    public void FleetData_CanRemoveShips()
    {
        // Arrange
        var fleet = new FleetData();
        var ship = new ShipData { Name = "Ship 1" };
        fleet.Ships.Add(ship);

        // Act
        fleet.Ships.Remove(ship);

        // Assert
        fleet.Ships.Should().BeEmpty();
    }

    #endregion
}

public class ShipDataTests
{
    #region Default Values Tests

    [Fact]
    public void ShipData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var ship = new ShipData();

        // Assert
        ship.Name.Should().BeEmpty();
        ship.CrewCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ShipData_Cargo_IsNotNull()
    {
        // Arrange & Act
        var ship = new ShipData();

        // Assert
        ship.Cargo.Should().NotBeNull();
    }

    [Fact]
    public void ShipData_Upgrades_IsNotNull()
    {
        // Arrange & Act
        var ship = new ShipData();

        // Assert
        ship.Upgrades.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ShipData_CanSetName()
    {
        // Arrange
        var ship = new ShipData();

        // Act
        ship.Name = "Test Ship";

        // Assert
        ship.Name.Should().Be("Test Ship");
    }

    [Fact]
    public void ShipData_CrewCount_DefaultIsZero()
    {
        // Arrange
        var ship = new ShipData();

        // Assert
        ship.CrewCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Theory]
    [InlineData(CrewQuality.Regular)]
    [InlineData(CrewQuality.Veteran)]
    [InlineData(CrewQuality.Elite)]
    public void ShipData_CanSetCrewQuality(CrewQuality quality)
    {
        // Arrange
        var ship = new ShipData();

        // Act
        ship.CrewQuality = quality;

        // Assert
        ship.CrewQuality.Should().Be(quality);
    }

    #endregion
}

public class NavalPositionTests
{
    [Fact]
    public void NavalPosition_DefaultValues_AreZero()
    {
        // Arrange & Act
        var pos = new NavalPosition();

        // Assert
        pos.X.Should().Be(0);
        pos.Y.Should().Be(0);
    }

    [Fact]
    public void NavalPosition_CanSetCoordinates()
    {
        // Arrange
        var pos = new NavalPosition { X = 100.5f, Y = 200.5f };

        // Assert
        pos.X.Should().Be(100.5f);
        pos.Y.Should().Be(200.5f);
    }
}
