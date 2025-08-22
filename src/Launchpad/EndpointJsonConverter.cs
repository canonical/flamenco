// This file is part of Flamenco
// Copyright 2024 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Canonical.Launchpad.Endpoints;

namespace Canonical.Launchpad;

/*
public class EndpointJsonConverterFactory : JsonConverterFactory
{
    private const Type EndpointInterfaceType = typeof(ILaunchpadEndpoint<>); 
    
    public override bool CanConvert(Type typeToConvert)
    {
        foreach (var VARIABLE in ty)
        {
            
        }
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
*/
public class EndpointJsonConverter<TEndpoint> : JsonConverter<TEndpoint> where TEndpoint : ILaunchpadEndpoint<TEndpoint>
{
    public override TEndpoint Read(
        ref Utf8JsonReader reader, 
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (typeToConvert != typeof(TEndpoint))
        {
            throw new JsonException(message: $"Can not convert type {typeToConvert.FullName}.");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            const int maxPreviewLength = 128;
            Span<char> value = stackalloc char[Math.Min(reader.ValueSpan.Length, maxPreviewLength)];

            if (Encoding.UTF8.TryGetChars(reader.ValueSpan, value, out int charsWritten))
            {
                value = value.Slice(start: 0, length: charsWritten);
            }
            else
            {
                value[^1] = '.';
                value[^2] = '.';
                value[^3] = '.';
            }
            
            throw new JsonException($"Expected token type String, but got {reader.TokenType} " +
                                    $"while trying to parse type {typeToConvert.Name} (value: '{value}').");
        }
        
        return TEndpoint.ParseEndpointRoot(endpointRoot: reader.GetString().AsSpan());
    }

    public override void Write(Utf8JsonWriter writer, TEndpoint? value, JsonSerializerOptions options)
    {
        if (value is not null)
            writer.WriteStringValue(value.EndpointRoot.ToString());
        else
            writer.WriteNullValue();
    }
}