// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

namespace Flamenco.Shared;

public class LinePositionTests
{
    [Fact]
    public void Constructor1_Throws_When_LineIsNegative()
    {
        void TestCode()
        {
            new LinePosition(line: -1);
        }
        
        Assert.Throws<ArgumentOutOfRangeException>(TestCode);
    }
    
    [Fact]
    public void Constructor2_Throws_When_LineIsNegative()
    {
        void TestCode()
        {
            new LinePosition(line: -1, character: 0);
        }
        
        Assert.Throws<ArgumentOutOfRangeException>(TestCode);
    }
    
    [Fact]
    public void Constructor_Throws_When_CharacterIsNegative()
    {
        void TestCode()
        {
            new LinePosition(line: 0, character: -1);
        }
        
        Assert.Throws<ArgumentOutOfRangeException>(TestCode);
    }
}