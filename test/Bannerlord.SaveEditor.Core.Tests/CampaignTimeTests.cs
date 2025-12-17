// <copyright file="CampaignTimeTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Models;
using FluentAssertions;
using Xunit;

public class CampaignTimeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithTicks_SetsTicksCorrectly()
    {
        // Arrange & Act
        var time = new CampaignTime(1000000L);

        // Assert
        time.Ticks.Should().Be(1000000L);
    }

    [Fact]
    public void Constructor_WithZeroTicks_CreatesValidTime()
    {
        // Arrange & Act
        var time = new CampaignTime(0L);

        // Assert
        time.Ticks.Should().Be(0L);
    }

    #endregion

    #region FromComponents Tests

    [Fact]
    public void FromComponents_ValidValues_CreatesCorrectTime()
    {
        // Act
        var time = CampaignTime.FromComponents(1084, 0, 1, 12);

        // Assert
        time.Year.Should().Be(1084);
        time.Season.Should().Be(0);
        time.DayOfSeason.Should().Be(1);
    }

    [Theory]
    [InlineData(1084, 0, 1)]
    [InlineData(1085, 1, 10)]
    [InlineData(1090, 2, 15)]
    [InlineData(1100, 3, 21)]
    public void FromComponents_VariousValues_CreatesCorrectTime(int year, int season, int day)
    {
        // Act
        var time = CampaignTime.FromComponents(year, season, day);

        // Assert
        time.Year.Should().Be(year);
        time.Season.Should().Be(season);
        time.DayOfSeason.Should().Be(day);
    }

    #endregion

    #region Year Tests

    [Fact]
    public void Year_ZeroTicks_Returns1084()
    {
        // Arrange
        var time = new CampaignTime(0L);

        // Assert
        time.Year.Should().Be(1084);
    }

    [Fact]
    public void Year_OneYearTicks_Returns1085()
    {
        // Arrange
        var time = new CampaignTime(CampaignTime.TicksPerYear);

        // Assert
        time.Year.Should().Be(1085);
    }

    #endregion

    #region Season Tests

    [Theory]
    [InlineData(0, "Spring")]
    [InlineData(1, "Summer")]
    [InlineData(2, "Autumn")]
    [InlineData(3, "Winter")]
    public void SeasonName_AllSeasons_ReturnsCorrectName(int season, string expectedName)
    {
        // Arrange
        var time = CampaignTime.FromComponents(1084, season, 1);

        // Assert
        time.SeasonName.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Season_ValidSeasons_ReturnsCorrectSeason(int season)
    {
        // Arrange
        var time = CampaignTime.FromComponents(1084, season, 1);

        // Assert
        time.Season.Should().Be(season);
    }

    #endregion

    #region DayOfSeason Tests

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(21)]
    public void DayOfSeason_ValidDays_ReturnsCorrectDay(int day)
    {
        // Arrange
        var time = CampaignTime.FromComponents(1084, 0, day);

        // Assert
        time.DayOfSeason.Should().Be(day);
    }

    #endregion

    #region HourOfDay Tests

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(23)]
    public void HourOfDay_ValidHours_ReturnsCorrectHour(int hour)
    {
        // Arrange
        var time = CampaignTime.FromComponents(1084, 0, 1, hour);

        // Assert
        time.HourOfDay.Should().Be(hour);
    }

    #endregion

    #region TotalDays Tests

    [Fact]
    public void TotalDays_ZeroTicks_ReturnsZero()
    {
        // Arrange
        var time = new CampaignTime(0L);

        // Assert
        time.TotalDays.Should().Be(0);
    }

    [Fact]
    public void TotalDays_OneDayTicks_ReturnsOne()
    {
        // Arrange
        var time = new CampaignTime(CampaignTime.TicksPerDay);

        // Assert
        time.TotalDays.Should().Be(1);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameTicks_ReturnsTrue()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(1000L);

        // Assert
        time1.Equals(time2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTicks_ReturnsFalse()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(2000L);

        // Assert
        time1.Equals(time2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectSameTicks_ReturnsTrue()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        object time2 = new CampaignTime(1000L);

        // Assert
        time1.Equals(time2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectNull_ReturnsFalse()
    {
        // Arrange
        var time = new CampaignTime(1000L);

        // Assert
        time.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectDifferentType_ReturnsFalse()
    {
        // Arrange
        var time = new CampaignTime(1000L);

        // Assert
        time.Equals("not a time").Should().BeFalse();
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void EqualityOperator_SameTicks_ReturnsTrue()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(1000L);

        // Assert
        (time1 == time2).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_DifferentTicks_ReturnsTrue()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(2000L);

        // Assert
        (time1 != time2).Should().BeTrue();
    }

    [Fact]
    public void LessThanOperator_SmallerTicks_ReturnsTrue()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(2000L);

        // Assert
        (time1 < time2).Should().BeTrue();
    }

    [Fact]
    public void GreaterThanOperator_LargerTicks_ReturnsTrue()
    {
        // Arrange
        var time1 = new CampaignTime(2000L);
        var time2 = new CampaignTime(1000L);

        // Assert
        (time1 > time2).Should().BeTrue();
    }

    [Fact]
    public void LessThanOrEqualOperator_EqualTicks_ReturnsTrue()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(1000L);

        // Assert
        (time1 <= time2).Should().BeTrue();
    }

    [Fact]
    public void GreaterThanOrEqualOperator_EqualTicks_ReturnsTrue()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(1000L);

        // Assert
        (time1 >= time2).Should().BeTrue();
    }

    #endregion

    #region CompareTo Tests

    [Fact]
    public void CompareTo_SameTicks_ReturnsZero()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(1000L);

        // Assert
        time1.CompareTo(time2).Should().Be(0);
    }

    [Fact]
    public void CompareTo_SmallerTicks_ReturnsNegative()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(2000L);

        // Assert
        time1.CompareTo(time2).Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_LargerTicks_ReturnsPositive()
    {
        // Arrange
        var time1 = new CampaignTime(2000L);
        var time2 = new CampaignTime(1000L);

        // Assert
        time1.CompareTo(time2).Should().BeGreaterThan(0);
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameTicks_ReturnsSameHash()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(1000L);

        // Assert
        time1.GetHashCode().Should().Be(time2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentTicks_ReturnsDifferentHash()
    {
        // Arrange
        var time1 = new CampaignTime(1000L);
        var time2 = new CampaignTime(2000L);

        // Assert
        time1.GetHashCode().Should().NotBe(time2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ValidTime_ReturnsFormattedString()
    {
        // Arrange
        var time = CampaignTime.FromComponents(1084, 0, 1);

        // Act
        var result = time.ToString();

        // Assert
        result.Should().Contain("1084");
        result.Should().Contain("Spring");
    }

    [Fact]
    public void ToString_WinterSeason_ContainsWinter()
    {
        // Arrange
        var time = CampaignTime.FromComponents(1084, 3, 10);

        // Act
        var result = time.ToString();

        // Assert
        result.Should().Contain("Winter");
    }

    #endregion

    #region Constants Tests

    [Fact]
    public void TicksPerHour_HasCorrectValue()
    {
        // Assert
        CampaignTime.TicksPerHour.Should().Be(2500);
    }

    [Fact]
    public void TicksPerDay_IsMultipleOfTicksPerHour()
    {
        // Assert
        CampaignTime.TicksPerDay.Should().Be(CampaignTime.TicksPerHour * 24);
    }

    [Fact]
    public void TicksPerSeason_IsMultipleOfTicksPerDay()
    {
        // Assert
        CampaignTime.TicksPerSeason.Should().Be(CampaignTime.TicksPerDay * 21);
    }

    [Fact]
    public void TicksPerYear_IsMultipleOfTicksPerSeason()
    {
        // Assert
        CampaignTime.TicksPerYear.Should().Be(CampaignTime.TicksPerSeason * 4);
    }

    #endregion
}
