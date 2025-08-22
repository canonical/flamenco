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

public class LinePositionSpanTests
{
    [Theory]
    [InlineData(5, 4)]
    public void IntConstructor_Throws_WhenEndIsLessThanStart(int start, int end)
    {
        void TestCode()
        {
            new LinePositionSpan(start, end);
        }
        
        Assert.Throws<ArgumentException>(TestCode);
    }
    
    [Theory]
    [InlineData(5, 4, 5, 3)] // startCharacter > endCharacter
    [InlineData(5, 4, 4, 5)] // startLine > endLine
    [InlineData(5, 4, 4, 4)] // both
    public void LinePositionConstructor_Throws_WhenEndIsLessThanStart(
        int startLine, 
        int startCharacter, 
        int endLine, 
        int endCharacter)
    {
        void TestCode()
        {
            new LinePositionSpan(
                start: new LinePosition(line: startLine, character: startCharacter), 
                end: new LinePosition(line: endLine, character: endCharacter));
        }
        
        Assert.Throws<ArgumentException>(TestCode);
    }
}