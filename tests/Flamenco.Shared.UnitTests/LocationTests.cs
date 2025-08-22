// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Collections;

namespace Flamenco.Shared;

public class LocationTests
{
    [Fact]
    public void UnspecifiedLocation_Has_ExpectedValues()
    {
        Assert.Equal(actual: Location.Unspecified.ResourceLocator, expected: null);
        Assert.Equal(actual: Location.Unspecified.TextSpan, expected: null);
    }
    
    [Fact]
    public void FromPosition_Returns_ExpectedValue()
    {
        var location = Location.FromPosition(5);
        
        Assert.Equal(
            actual: location, 
            expected: new Location
            {
                ResourceLocator = null,
                TextSpan = new LinePositionSpan(
                    start: new LinePosition(line: 0, character: 5),
                    end: new LinePosition(line: 0, character: 5))
            });
    }
    
    [Fact]
    public void FromPosition_Throws_ForNegativeValues()
    {
        void TestCode()
        {
            Location.FromPosition(-1);
        }
        
        Assert.Throws<ArgumentOutOfRangeException>(TestCode);
    }
    
    [Theory]
    [ClassData(typeof(LocationOffsetTestDataGenerator))]
    public void Offset_Returns_ExpectedValues(Location parentLocation, Location childLocation, Location expectedLocation)
    {   
        var location = childLocation.Offset(parentLocation);
        Assert.Equal(actual: location, expected: expectedLocation);
    }
}

public class LocationOffsetTestDataGenerator : IEnumerable<object[]>
{
    private readonly List<object[]> _data =
    [
        new object[]
        {
            // Location parentLocation:
            Location.Unspecified,
            // Location childLocation:
            Location.Unspecified,
            // Location expectedLocation:
            Location.Unspecified,
        },
        new object[]
        {
            // Location parentLocation:
            new Location { ResourceLocator = "/tmp/location" },
            // Location childLocation:
            Location.Unspecified,
            // Location expectedLocation:
            new Location { ResourceLocator = "/tmp/location" },
        },
        new object[]
        {
            // Location parentLocation:
            Location.Unspecified,
            // Location childLocation:
            new Location { ResourceLocator = "/tmp/location" },
            // Location expectedLocation:
            new Location { ResourceLocator = "/tmp/location" },
        },
        new object[]
        {
            // Location parentLocation:
            new Location { ResourceLocator = "/tmp/location", TextSpan = new LinePositionSpan(start: 0, end: 5) },
            // Location childLocation:
            Location.Unspecified,
            // Location expectedLocation:
            new Location { ResourceLocator = "/tmp/location", TextSpan = new LinePositionSpan(start: 0, end: 5) },
        },
        new object[]
        {
            // Location parentLocation:
            Location.Unspecified,
            // Location childLocation:
            new Location { ResourceLocator = "/tmp/location", TextSpan = new LinePositionSpan(start: 0, end: 5) },
            // Location expectedLocation:
            new Location { ResourceLocator = "/tmp/location", TextSpan = new LinePositionSpan(start: 0, end: 5) },
        },
        new object[]
        {
            // Location parentLocation:
            new Location
            {
                ResourceLocator = "/tmp/location", 
                TextSpan = new LinePositionSpan(
                    start: new LinePosition(line: 1, character: 1),
                    end: new LinePosition(line: 2, character: 5))
            },
            // Location childLocation:
            new Location { TextSpan = new LinePositionSpan(start: 0, end: 2) },
            // Location expectedLocation:
            new Location
            {
                ResourceLocator = "/tmp/location", 
                TextSpan = new LinePositionSpan(
                    start: new LinePosition(line: 1, character: 1),
                    end: new LinePosition(line: 1, character: 3))
            },
        },
        new object[]
        {
            // Location parentLocation:
            new Location
            {
                TextSpan = new LinePositionSpan(
                    start: new LinePosition(line: 1, character: 1),
                    end: new LinePosition(line: 2, character: 5))
            },
            // Location childLocation:
            new Location
            {
                ResourceLocator = "/tmp/location",
                TextSpan = new LinePositionSpan(start: 0, end: 2)
            },
            // Location expectedLocation:
            new Location
            {
                ResourceLocator = "/tmp/location", 
                TextSpan = new LinePositionSpan(
                    start: new LinePosition(line: 1, character: 1),
                    end: new LinePosition(line: 1, character: 3))
            },
        },
    ];

    public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
