using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace io.github.ykysnk.Localization.Editor
{
    /// <summary>
    ///     Converts a <see cref="BasicLocalization" /> to and from the flat formats translation tools use.
    ///     <para>
    ///         Every entry becomes <c>"key" = "translate"</c>. Entries that use a tooltip add a second pair
    ///         <c>"key + tooltipSuffix" = "tooltip"</c>; entries with <see cref="BasicTranslate.useTooltip" /> off are
    ///         written without one. Import merges by key and never deletes entries.
    ///     </para>
    /// </summary>
    [PublicAPI]
    public static class LocalizationConverter
    {
        /// <summary>Suffixes recognised when a key is read back as a tooltip entry.</summary>
        public static readonly string[] TooltipSuffixes = { ".tooltip", ":tooltip", "_tooltip" };

        /// <summary>Writes a flat JSON object: key to text, plus one tooltip key per entry that uses one.</summary>
        public static string ToJson(BasicLocalization localization)
        {
            var builder = new StringBuilder();
            builder.Append("{\n");

            var first = true;
            foreach (var pair in Flatten(localization))
            {
                if (!first) builder.Append(",\n");
                first = false;

                builder.Append("  \"").Append(EscapeJson(pair.Key)).Append("\": \"")
                    .Append(EscapeJson(pair.Value)).Append('"');
            }

            builder.Append("\n}\n");
            return builder.ToString();
        }

        /// <summary>Writes a PO file with one msgid/msgstr pair per key, tooltips included.</summary>
        public static string ToPo(BasicLocalization localization)
        {
            var builder = new StringBuilder();
            builder.Append("msgid \"\"\n");
            builder.Append("msgstr \"\"\n");
            builder.Append("\"Content-Type: text/plain; charset=UTF-8\\n\"\n\n");

            foreach (var pair in Flatten(localization))
            {
                builder.Append("msgid \"").Append(EscapePo(pair.Key)).Append("\"\n");
                builder.Append("msgstr \"").Append(EscapePo(pair.Value)).Append("\"\n\n");
            }

            return builder.ToString();
        }

        /// <summary>Reads a flat JSON object into <paramref name="localization" />.</summary>
        public static bool TryImportJson(BasicLocalization localization, string? text, out Exception? exception)
        {
            try
            {
                Apply(localization, ParseJson(text ?? ""));
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }
        }

        /// <summary>Reads a PO file into <paramref name="localization" />.</summary>
        public static bool TryImportPo(BasicLocalization localization, string? text, out Exception? exception)
        {
            try
            {
                Apply(localization, ParsePo(text ?? ""));
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> Flatten(BasicLocalization localization)
        {
            foreach (var entry in localization.translates)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;

                yield return new KeyValuePair<string, string>(entry.key, entry.translate ?? "");

                if (!entry.useTooltip) continue;

                var suffix = string.IsNullOrEmpty(entry.tooltipSuffix) ? ".tooltip" : entry.tooltipSuffix;
                yield return new KeyValuePair<string, string>(entry.key + suffix, entry.tooltip ?? "");
            }
        }

        private static void Apply(BasicLocalization localization, List<KeyValuePair<string, string>> pairs)
        {
            var fileKeys = new HashSet<string>(pairs.Select(p => p.Key), StringComparer.Ordinal);
            var byKey = new Dictionary<string, BasicTranslate>(StringComparer.Ordinal);

            foreach (var entry in localization.translates)
                byKey[entry.key] = entry;

            // A key ending in a tooltip suffix is only a tooltip when its base key exists as well; otherwise it is a
            // normal entry that happens to end with ".tooltip" (our own label.basic_translate.tooltip is one).
            bool IsTooltip(string key, out string baseKey, out string suffix)
            {
                foreach (var candidate in TooltipSuffixes)
                {
                    if (key.Length <= candidate.Length || !key.EndsWith(candidate, StringComparison.Ordinal))
                        continue;

                    var candidateBase = key.Substring(0, key.Length - candidate.Length);
                    if (!fileKeys.Contains(candidateBase) && !byKey.ContainsKey(candidateBase)) continue;

                    baseKey = candidateBase;
                    suffix = candidate;
                    return true;
                }

                baseKey = key;
                suffix = "";
                return false;
            }

            foreach (var pair in pairs)
            {
                if (IsTooltip(pair.Key, out _, out _)) continue;

                if (!byKey.TryGetValue(pair.Key, out var entry))
                {
                    entry = new BasicTranslate { key = pair.Key };
                    localization.translates.Add(entry);
                    byKey[pair.Key] = entry;
                }

                entry.translate = pair.Value;
            }

            foreach (var pair in pairs)
            {
                if (!IsTooltip(pair.Key, out var baseKey, out var suffix)) continue;

                if (!byKey.TryGetValue(baseKey, out var entry))
                {
                    entry = new BasicTranslate { key = baseKey };
                    localization.translates.Add(entry);
                    byKey[baseKey] = entry;
                }

                entry.tooltip = pair.Value;
                entry.useTooltip = true;
                entry.tooltipSuffix = suffix;
            }
        }

        private static List<KeyValuePair<string, string>> ParseJson(string text)
        {
            var index = 0;
            var result = new List<KeyValuePair<string, string>>();

            SkipWhitespace(text, ref index);
            Expect(text, ref index, '{');
            SkipWhitespace(text, ref index);

            if (Peek(text, index) == '}')
            {
                index++;
                return result;
            }

            while (true)
            {
                SkipWhitespace(text, ref index);
                var key = ParseJsonString(text, ref index);

                SkipWhitespace(text, ref index);
                Expect(text, ref index, ':');
                SkipWhitespace(text, ref index);

                var value = ParseJsonString(text, ref index);
                result.Add(new KeyValuePair<string, string>(key, value));

                SkipWhitespace(text, ref index);
                var next = Peek(text, index);
                if (next == ',')
                {
                    index++;
                    continue;
                }

                if (next == '}')
                {
                    index++;
                    break;
                }

                throw new FormatException($"Expected ',' or '}}' at position {index}.");
            }

            SkipWhitespace(text, ref index);
            if (index < text.Length)
                throw new FormatException($"Unexpected text after the object at position {index}.");

            return result;
        }

        private static string ParseJsonString(string text, ref int index)
        {
            Expect(text, ref index, '"');

            var builder = new StringBuilder();
            while (index < text.Length)
            {
                var c = text[index++];
                if (c == '"') return builder.ToString();

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (index >= text.Length) break;

                var escape = text[index++];
                switch (escape)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (index + 4 > text.Length) throw new FormatException("Truncated \\u escape.");
                        builder.Append((char)Convert.ToInt32(text.Substring(index, 4), 16));
                        index += 4;
                        break;
                    default:
                        throw new FormatException($"Unsupported escape '\\{escape}'.");
                }
            }

            throw new FormatException("Unterminated string.");
        }

        private static List<KeyValuePair<string, string>> ParsePo(string text)
        {
            var result = new List<KeyValuePair<string, string>>();
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            string? id = null;
            var value = new StringBuilder();
            var readingValue = false;

            void Flush()
            {
                if (id == null) return;
                if (id.Length > 0) result.Add(new KeyValuePair<string, string>(id, value.ToString()));
                id = null;
                value.Clear();
                readingValue = false;
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                {
                    Flush();
                    continue;
                }

                if (line[0] == '#') continue;

                if (line.StartsWith("msgid ", StringComparison.Ordinal))
                {
                    Flush();
                    id = UnescapePo(ExtractQuoted(line, 6));
                    continue;
                }

                if (line.StartsWith("msgstr ", StringComparison.Ordinal) ||
                    line.StartsWith("msgstr[0] ", StringComparison.Ordinal))
                {
                    readingValue = true;
                    var offset = line.StartsWith("msgstr[0] ", StringComparison.Ordinal) ? 10 : 7;
                    value.Append(UnescapePo(ExtractQuoted(line, offset)));
                    continue;
                }

                if (line.StartsWith("msgid_plural ", StringComparison.Ordinal) ||
                    line.StartsWith("msgctxt ", StringComparison.Ordinal))
                    continue;

                if (line[0] == '"')
                {
                    var part = UnescapePo(ExtractQuoted(line, 0));
                    if (readingValue) value.Append(part);
                    else if (id != null) id += part;
                    continue;
                }

                throw new FormatException($"Unsupported PO line: {line}");
            }

            Flush();
            return result;
        }

        private static string ExtractQuoted(string line, int offset)
        {
            var start = line.IndexOf('"', offset);
            if (start < 0) throw new FormatException($"Missing quoted string in: {line}");

            var builder = new StringBuilder();
            for (var i = start + 1; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '\\' && i + 1 < line.Length)
                {
                    builder.Append(c).Append(line[++i]);
                    continue;
                }

                if (c == '"') return builder.ToString();
                builder.Append(c);
            }

            throw new FormatException($"Unterminated quoted string in: {line}");
        }

        private static string UnescapePo(string value)
        {
            var builder = new StringBuilder(value.Length);

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c != '\\' || i + 1 >= value.Length)
                {
                    builder.Append(c);
                    continue;
                }

                var escape = value[++i];
                builder.Append(escape switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '"' => '"',
                    '\\' => '\\',
                    _ => escape
                });
            }

            return builder.ToString();
        }

        private static string EscapeJson(string value)
        {
            var builder = new StringBuilder(value.Length + 8);

            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 0x20) builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private static string EscapePo(string value) =>
            value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r")
                .Replace("\t", "\\t");

        private static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        }

        private static char Peek(string text, int index) => index < text.Length ? text[index] : '\0';

        private static void Expect(string text, ref int index, char expected)
        {
            if (index >= text.Length || text[index] != expected)
                throw new FormatException($"Expected '{expected}' at position {index}.");
            index++;
        }
    }
}
