// Copyright (c) 2025 Kanders-II. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Launcher.Services
{
    /// <summary>
    /// Simple Markdown-to-FlowDocument parser for rendering script output in flyout windows.
    /// Supports: headings, bold, italic, code blocks, inline code, bullet lists, numbered lists,
    /// horizontal rules, links, and paragraphs.
    /// No third-party dependencies - uses only WPF FlowDocument primitives.
    /// </summary>
    public static class MarkdownParser
    {
        /// <summary>
        /// Parses a Markdown string into a WPF FlowDocument.
        /// </summary>
        public static FlowDocument Parse(string markdown)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                PagePadding = new Thickness(16)
            };

            if (string.IsNullOrWhiteSpace(markdown))
                return doc;

            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i];

                // Blank line - skip
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                // Fenced code block ```
                if (line.TrimStart().StartsWith("```"))
                {
                    var codeLines = new List<string>();
                    i++; // skip opening fence
                    while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                    {
                        codeLines.Add(lines[i]);
                        i++;
                    }
                    if (i < lines.Length) i++; // skip closing fence

                    var codePara = new Paragraph
                    {
                        FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                        FontSize = 12.5,
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 4, 0, 4),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                        BorderThickness = new Thickness(1)
                    };
                    codePara.Inlines.Add(new Run(string.Join(Environment.NewLine, codeLines)));
                    doc.Blocks.Add(codePara);
                    continue;
                }

                // Heading (#, ##, ###, etc.)
                var headingMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
                if (headingMatch.Success)
                {
                    int level = headingMatch.Groups[1].Value.Length;
                    string text = headingMatch.Groups[2].Value;
                    double size;
                    switch (level)
                    {
                        case 1: size = 26; break;
                        case 2: size = 22; break;
                        case 3: size = 18; break;
                        case 4: size = 16; break;
                        default: size = 14; break;
                    }
                    var heading = new Paragraph
                    {
                        FontSize = size,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, level <= 2 ? 12 : 8, 0, 4)
                    };
                    AddInlineElements(heading.Inlines, text);
                    doc.Blocks.Add(heading);
                    i++;
                    continue;
                }

                // Horizontal rule (---, ***, ___)
                if (Regex.IsMatch(line.Trim(), @"^[-*_]{3,}$"))
                {
                    var hr = new Paragraph
                    {
                        Margin = new Thickness(0, 8, 0, 8),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    doc.Blocks.Add(hr);
                    i++;
                    continue;
                }

                // Unordered list (- item or * item)
                if (Regex.IsMatch(line, @"^\s*[-*+]\s+"))
                {
                    var list = new System.Windows.Documents.List
                    {
                        MarkerStyle = TextMarkerStyle.Disc,
                        Margin = new Thickness(16, 4, 0, 4)
                    };
                    while (i < lines.Length && Regex.IsMatch(lines[i], @"^\s*[-*+]\s+"))
                    {
                        var itemText = Regex.Replace(lines[i], @"^\s*[-*+]\s+", "");
                        var listItem = new ListItem();
                        var itemPara = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
                        AddInlineElements(itemPara.Inlines, itemText);
                        listItem.Blocks.Add(itemPara);
                        list.ListItems.Add(listItem);
                        i++;
                    }
                    doc.Blocks.Add(list);
                    continue;
                }

                // Ordered list (1. item)
                if (Regex.IsMatch(line, @"^\s*\d+\.\s+"))
                {
                    var list = new System.Windows.Documents.List
                    {
                        MarkerStyle = TextMarkerStyle.Decimal,
                        Margin = new Thickness(16, 4, 0, 4)
                    };
                    while (i < lines.Length && Regex.IsMatch(lines[i], @"^\s*\d+\.\s+"))
                    {
                        var itemText = Regex.Replace(lines[i], @"^\s*\d+\.\s+", "");
                        var listItem = new ListItem();
                        var itemPara = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
                        AddInlineElements(itemPara.Inlines, itemText);
                        listItem.Blocks.Add(itemPara);
                        list.ListItems.Add(listItem);
                        i++;
                    }
                    doc.Blocks.Add(list);
                    continue;
                }

                // Blockquote (> text)
                if (line.TrimStart().StartsWith(">"))
                {
                    var quoteLines = new List<string>();
                    while (i < lines.Length && lines[i].TrimStart().StartsWith(">"))
                    {
                        quoteLines.Add(Regex.Replace(lines[i], @"^\s*>\s?", ""));
                        i++;
                    }
                    var quote = new Paragraph
                    {
                        Margin = new Thickness(0, 4, 0, 4),
                        Padding = new Thickness(12, 4, 12, 4),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                        BorderThickness = new Thickness(3, 0, 0, 0),
                        Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                        FontStyle = FontStyles.Italic
                    };
                    AddInlineElements(quote.Inlines, string.Join(" ", quoteLines));
                    doc.Blocks.Add(quote);
                    continue;
                }

                // Pipe table (| col1 | col2 |)
                if (line.TrimStart().StartsWith("|") && line.TrimEnd().EndsWith("|"))
                {
                    var tableRows = new List<string>();
                    while (i < lines.Length && lines[i].TrimStart().StartsWith("|") && lines[i].TrimEnd().EndsWith("|"))
                    {
                        tableRows.Add(lines[i]);
                        i++;
                    }

                    if (tableRows.Count >= 2)
                    {
                        var table = new Table
                        {
                            CellSpacing = 0,
                            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                            BorderThickness = new Thickness(1),
                            Margin = new Thickness(0, 6, 0, 6)
                        };

                        // Parse first row to determine column count
                        var firstCells = ParseTableRow(tableRows[0]);
                        foreach (var _ in firstCells)
                        {
                            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
                        }

                        var rowGroup = new TableRowGroup();
                        int dataRowIndex = 0;

                        for (int r = 0; r < tableRows.Count; r++)
                        {
                            var rowText = tableRows[r].Trim();

                            // Skip separator rows (|---|---|)
                            if (Regex.IsMatch(rowText, @"^\|[\s\-:]+(\|[\s\-:]+)*\|$"))
                                continue;

                            var cells = ParseTableRow(tableRows[r]);
                            var tableRow = new TableRow();

                            bool isHeader = (r == 0 && tableRows.Count > 1 &&
                                             r + 1 < tableRows.Count &&
                                             Regex.IsMatch(tableRows[r + 1].Trim(), @"^\|[\s\-:]+(\|[\s\-:]+)*\|$"));

                            if (isHeader)
                            {
                                tableRow.Background = new SolidColorBrush(Color.FromRgb(45, 45, 50));
                            }
                            else if (dataRowIndex % 2 == 1)
                            {
                                tableRow.Background = new SolidColorBrush(Color.FromRgb(35, 35, 38));
                            }

                            foreach (var cellText in cells)
                            {
                                var cell = new TableCell
                                {
                                    Padding = new Thickness(8, 4, 8, 4),
                                    BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                                    BorderThickness = new Thickness(0, 0, 1, 1)
                                };
                                var cellPara = new Paragraph { Margin = new Thickness(0) };
                                if (isHeader)
                                {
                                    cellPara.FontWeight = FontWeights.SemiBold;
                                }
                                AddInlineElements(cellPara.Inlines, cellText.Trim());
                                cell.Blocks.Add(cellPara);
                                tableRow.Cells.Add(cell);
                            }

                            rowGroup.Rows.Add(tableRow);
                            if (!isHeader) dataRowIndex++;
                        }

                        table.RowGroups.Add(rowGroup);
                        doc.Blocks.Add(table);
                        continue;
                    }
                }

                // Regular paragraph
                {
                    var para = new Paragraph { Margin = new Thickness(0, 3, 0, 3) };
                    AddInlineElements(para.Inlines, line);
                    doc.Blocks.Add(para);
                    i++;
                }
            }

            return doc;
        }

        /// <summary>
        /// Parses a pipe-delimited table row into individual cell strings.
        /// Strips leading/trailing pipes and splits on remaining pipes.
        /// </summary>
        private static List<string> ParseTableRow(string row)
        {
            var trimmed = row.Trim();
            // Remove leading and trailing |
            if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
            if (trimmed.EndsWith("|")) trimmed = trimmed.Substring(0, trimmed.Length - 1);
            var cells = new List<string>(trimmed.Split('|'));
            return cells;
        }

        /// <summary>
        /// Parses inline Markdown elements (bold, italic, code, links) into WPF Inlines.
        /// </summary>
        private static void AddInlineElements(InlineCollection inlines, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Pattern matches: **bold**, *italic*, `code`, [text](url)
            var pattern = @"(\*\*(.+?)\*\*)|(\*(.+?)\*)|(`(.+?)`)|(\[(.+?)\]\((.+?)\))";
            int lastIndex = 0;

            foreach (Match match in Regex.Matches(text, pattern))
            {
                // Add text before this match
                if (match.Index > lastIndex)
                {
                    inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                }

                if (match.Groups[1].Success) // **bold**
                {
                    inlines.Add(new Bold(new Run(match.Groups[2].Value)));
                }
                else if (match.Groups[3].Success) // *italic*
                {
                    inlines.Add(new Italic(new Run(match.Groups[4].Value)));
                }
                else if (match.Groups[5].Success) // `code`
                {
                    var codeRun = new Run(match.Groups[6].Value)
                    {
                        FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                        FontSize = 12.5,
                        Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                        Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212))
                    };
                    inlines.Add(codeRun);
                }
                else if (match.Groups[7].Success) // [text](url)
                {
                    var hyperlink = new Hyperlink(new Run(match.Groups[8].Value))
                    {
                        NavigateUri = new Uri(match.Groups[9].Value, UriKind.RelativeOrAbsolute),
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212))
                    };
                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = e.Uri.AbsoluteUri,
                                UseShellExecute = true
                            });
                        }
                        catch { }
                        e.Handled = true;
                    };
                    inlines.Add(hyperlink);
                }

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < text.Length)
            {
                inlines.Add(new Run(text.Substring(lastIndex)));
            }
        }
    }
}
