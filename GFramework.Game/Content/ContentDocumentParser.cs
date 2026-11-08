// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace GFramework.Game.Content;

/// <summary>Normalizes the data-only YAML subset into JSON values without resolving CLR types or aliases.</summary>
internal static class ContentDocumentParser
{
    /// <summary>Reads exactly one bounded YAML sequence of operation mappings.</summary>
    /// <param name="text">Operation file contents.</param>
    /// <returns>Normalized operations.</returns>
    internal static JArray Parse(string text)
    {
        var stream = new YamlStream();
        using var reader = new StringReader(text);
        stream.Load(reader);
        if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlSequenceNode sequence)
            throw new InvalidOperationException("A content file must contain one YAML sequence of operations.");
        var count = 0;
        return (JArray)ConvertNode(sequence, 0, ref count);
    }

    private static JToken ConvertNode(YamlNode node, int depth, ref int count)
    {
        if (depth > 64 || ++count > 100000)
            throw new InvalidOperationException("Content YAML exceeds the supported depth or node count.");
        if (!node.Anchor.IsEmpty)
            throw new InvalidOperationException("Content YAML anchors and aliases are not supported.");
        var tag = node.Tag.ToString();
        if (!string.IsNullOrEmpty(tag) && tag is not "?" and not "!" and not
            "tag:yaml.org,2002:str" and not "tag:yaml.org,2002:int" and not "tag:yaml.org,2002:float" and not
            "tag:yaml.org,2002:bool" and not "tag:yaml.org,2002:null" and not
            "tag:yaml.org,2002:map" and not "tag:yaml.org,2002:seq")
            throw new InvalidOperationException("Content YAML cannot declare custom tags or runtime types.");
        if (node is YamlMappingNode mapping)
        {
            var result = new JObject();
            foreach (var pair in mapping.Children)
            {
                if (pair.Key is not YamlScalarNode key || string.IsNullOrEmpty(key.Value) || result.ContainsKey(key.Value))
                    throw new InvalidOperationException("Content objects require unique nonempty string keys.");
                result.Add(key.Value, ConvertNode(pair.Value, depth + 1, ref count));
            }
            return result;
        }
        if (node is YamlSequenceNode sequence)
        {
            var result = new JArray();
            foreach (var item in sequence.Children)
                result.Add(ConvertNode(item, depth + 1, ref count));
            return result;
        }
        if (node is not YamlScalarNode scalar)
            throw new InvalidOperationException("Unsupported content YAML node.");
        var value = scalar.Value;
        if (scalar.Style is ScalarStyle.DoubleQuoted or ScalarStyle.SingleQuoted or ScalarStyle.Literal or ScalarStyle.Folded ||
            string.Equals(tag, "tag:yaml.org,2002:str", StringComparison.Ordinal))
            return new JValue(value ?? string.Empty);
        if (value is null or "" or "null" or "Null" or "NULL" or "~")
            return JValue.CreateNull();
        if (bool.TryParse(value, out var boolean))
            return new JValue(boolean);
        if (long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
            return new JValue(integer);
        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return new JValue(number);
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floating))
        {
            if (!double.IsFinite(floating))
                throw new InvalidOperationException("Non-finite content numbers are not allowed.");
            return new JValue(floating);
        }
        return new JValue(value);
    }
}
