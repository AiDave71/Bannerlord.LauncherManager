# Testing Documentation

> Comprehensive test coverage for Bannerlord Save Editor

![Test Coverage Dashboard](diagrams/testing.svg)

---

## Overview

| Metric | Value |
|--------|-------|
| **Total Tests** | 949 |
| **Pass Rate** | 100% |
| **Core Tests** | 881 |
| **Backup Tests** | 68 |
| **Framework** | xUnit + FluentAssertions |

---

## Test Structure

```
test/
├── Bannerlord.SaveEditor.Core.Tests/
│   ├── SaveParserTests.cs         (~120 tests)
│   ├── SaveWriterTests.cs         (~60 tests)
│   ├── CharacterEditorTests.cs    (~160 tests)
│   ├── PartyEditorTests.cs        (~140 tests)
│   ├── FleetEditorTests.cs        (~150 tests)
│   ├── ValidationServiceTests.cs  (~50 tests)
│   ├── SaveServiceTests.cs        (~30 tests)
│   ├── ZlibHandlerTests.cs        (~40 tests)
│   ├── MBGUIDTests.cs             (~80 tests)
│   ├── CampaignTimeTests.cs       (~47 tests)
│   ├── HeroDataTests.cs           (~29 tests)
│   ├── FleetDataTests.cs          (~26 tests)
│   ├── ModelEntityTests.cs        (~24 tests)
│   └── EdgeCaseTests.cs           (~27 tests)
└── Bannerlord.SaveEditor.Backup.Tests/
    └── BackupServiceTests.cs      (68 tests)
```

---

## Test Categories

### SaveParser Tests (~120 tests)

Tests for loading and parsing save files:

| Category | Tests | Coverage |
|----------|-------|----------|
| Header parsing | 15 | File headers, metadata |
| Decompression | 20 | ZLIB decompression |
| JSON parsing | 25 | Entity deserialization |
| Entity loading | 30 | Heroes, parties, settlements |
| Error handling | 15 | Invalid files, corruption |
| Async operations | 15 | Cancellation, timeouts |

```csharp
[Fact]
public async Task LoadAsync_ValidSaveFile_ReturnsPopulatedSaveFile()
{
    var save = await _parser.LoadAsync(_validSavePath);
    
    save.Should().NotBeNull();
    save.Header.Should().NotBeNull();
    save.Campaign.Heroes.Should().NotBeEmpty();
}
```

### SaveWriter Tests (~60 tests)

Tests for writing and saving:

| Category | Tests | Coverage |
|----------|-------|----------|
| Compression | 15 | ZLIB compression levels |
| Header writing | 10 | Metadata preservation |
| Serialization | 15 | Entity serialization |
| Integrity | 10 | Checksum verification |
| Error handling | 10 | Invalid paths, permissions |

```csharp
[Fact]
public async Task SaveAsync_ModifiedSave_PreservesAllData()
{
    var original = await _parser.LoadAsync(_savePath);
    await _writer.SaveAsync(original, _outputPath);
    var reloaded = await _parser.LoadAsync(_outputPath);
    
    reloaded.Header.Should().BeEquivalentTo(original.Header);
}
```

### CharacterEditor Tests (~160 tests)

Comprehensive character editing tests:

| Category | Tests | Coverage |
|----------|-------|----------|
| Level/XP | 20 | Level changes, XP calc |
| Attributes | 30 | All 6 attributes |
| Skills | 40 | All 18 skills, focus |
| Perks | 25 | Add/remove perks |
| Gold | 15 | Currency operations |
| State | 20 | Health, alive, state |
| Equipment | 10 | Gear management |

```csharp
[Theory]
[InlineData(1)]
[InlineData(30)]
[InlineData(62)]
public async Task SetLevel_ValidRange_UpdatesLevel(int level)
{
    await _editor.SetLevelAsync(_hero, level);
    
    _hero.Level.Should().Be(level);
}
```

### PartyEditor Tests (~140 tests)

Party management tests:

| Category | Tests | Coverage |
|----------|-------|----------|
| Troops | 40 | Add/remove/heal |
| Prisoners | 25 | Capture/release |
| Resources | 25 | Food, gold, morale |
| Size limits | 20 | Capacity validation |
| State | 15 | Party state changes |
| Merge | 15 | Stack merging |

```csharp
[Fact]
public async Task AddTroops_ValidCount_IncreasesPartySize()
{
    var initialSize = _party.TotalTroops;
    
    await _editor.AddTroopsAsync(_party, "imperial_infantry", 10);
    
    _party.TotalTroops.Should().Be(initialSize + 10);
}
```

### FleetEditor Tests (~150 tests)

Naval/War Sails tests:

| Category | Tests | Coverage |
|----------|-------|----------|
| Fleet ops | 30 | Create, admiral, flagship |
| Ship ops | 35 | Repair, crew, upgrades |
| Cargo | 25 | Add/remove cargo |
| Position | 15 | Naval coordinates |
| Computed | 20 | Total crew, capacity |
| Validation | 25 | Invalid operations |

```csharp
[Fact]
public void TotalCrewCount_MultipleShips_ReturnsSumOfCrew()
{
    _fleet.Ships.Add(new ShipData { CrewCount = 25 });
    _fleet.Ships.Add(new ShipData { CrewCount = 30 });
    
    _fleet.TotalCrewCount.Should().Be(55);
}
```

### MBGUID Tests (~80 tests)

Unique identifier tests:

| Category | Tests | Coverage |
|----------|-------|----------|
| Generation | 15 | All entity types |
| Equality | 20 | Equals, operators |
| Comparison | 15 | CompareTo, ordering |
| Collections | 15 | HashSet, Dictionary |
| Parsing | 15 | ToString, Parse |

```csharp
[Fact]
public void Generate_DifferentTypes_CreatesDifferentGuids()
{
    var hero = MBGUID.Generate(MBGUIDType.Hero);
    var party = MBGUID.Generate(MBGUIDType.Party);
    
    hero.Should().NotBe(party);
}
```

### CampaignTime Tests (~47 tests)

Time system tests:

| Category | Tests | Coverage |
|----------|-------|----------|
| Construction | 5 | From ticks, components |
| Properties | 15 | Year, season, day, hour |
| Operators | 12 | ==, !=, <, >, <=, >= |
| Comparison | 5 | CompareTo |
| Formatting | 5 | ToString |
| Constants | 5 | Ticks calculations |

```csharp
[Theory]
[InlineData(0, "Spring")]
[InlineData(1, "Summer")]
[InlineData(2, "Autumn")]
[InlineData(3, "Winter")]
public void SeasonName_AllSeasons_ReturnsCorrectName(int season, string name)
{
    var time = CampaignTime.FromComponents(1084, season, 1);
    
    time.SeasonName.Should().Be(name);
}
```

### Edge Case Tests (~27 tests)

Boundary and edge case coverage:

| Category | Tests | Coverage |
|----------|-------|----------|
| Null refs | 6 | Null property handling |
| Empty collections | 4 | Empty lists/sets |
| Numeric bounds | 5 | Min/max values |
| String edge cases | 4 | Empty, unicode |
| Enum values | 4 | All enum options |
| Position coords | 4 | Negative coordinates |

```csharp
[Fact]
public void HeroData_UnicodeNameCharacters_IsValid()
{
    var hero = new HeroData { Name = "日本語テスト" };
    
    hero.Name.Should().Be("日本語テスト");
}
```

### Backup Tests (68 tests)

Backup system tests:

| Category | Tests | Coverage |
|----------|-------|----------|
| Create | 15 | Backup creation |
| Restore | 15 | Backup restoration |
| Compression | 10 | GZip/LZ4 |
| Retention | 10 | Max backups |
| Integrity | 10 | SHA256 verification |
| Errors | 8 | Error handling |

---

## Running Tests

### All Tests

```bash
dotnet test Bannerlord.SaveEditor.sln -c Debug
```

### With Verbosity

```bash
dotnet test Bannerlord.SaveEditor.sln -c Debug --verbosity normal
```

### Specific Project

```bash
dotnet test test/Bannerlord.SaveEditor.Core.Tests -c Debug
```

### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Filter by Category

```bash
dotnet test --filter "FullyQualifiedName~CharacterEditor"
```

---

## Test Patterns

### Arrange-Act-Assert

All tests follow AAA pattern:

```csharp
[Fact]
public void Example_Scenario_ExpectedResult()
{
    // Arrange
    var sut = new SystemUnderTest();
    var input = CreateTestInput();
    
    // Act
    var result = sut.MethodUnderTest(input);
    
    // Assert
    result.Should().BeExpectedValue();
}
```

### Theory Tests

Parameterized tests for multiple inputs:

```csharp
[Theory]
[InlineData(0, 0)]
[InlineData(5, 5)]
[InlineData(10, 10)]
public void SetAttribute_ValidRange_SetsCorrectly(int input, int expected)
{
    // Test implementation
}
```

### Async Tests

Async operations tested properly:

```csharp
[Fact]
public async Task LoadAsync_ValidPath_ReturnsData()
{
    var result = await _service.LoadAsync(_path);
    
    result.Should().NotBeNull();
}
```

---

## Best Practices

1. **Isolation**: Each test is independent
2. **Naming**: `MethodName_Scenario_ExpectedBehavior`
3. **Assertions**: Use FluentAssertions for readability
4. **Coverage**: Target all code paths
5. **Edge Cases**: Test boundaries and nulls
6. **Performance**: Tests run quickly (<1 second)

---

## Continuous Integration

Tests run automatically on:
- Pull request creation
- Push to main/feature branches
- Nightly builds

```yaml
# Example CI configuration
test:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - name: Test
      run: dotnet test --verbosity normal
```
