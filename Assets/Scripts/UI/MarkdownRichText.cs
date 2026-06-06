using System.Text;
using System.Text.RegularExpressions;

namespace MuseGameJam.UI
{
    /// <summary>
    /// Minimal Markdown -> UI Toolkit rich-text converter for the Info detail menu.
    ///
    /// UI Toolkit has no Markdown support, but a Label with rich text enabled understands
    /// a handful of TextCore tags (&lt;b&gt;, &lt;i&gt;, &lt;u&gt;, &lt;size&gt;). This maps the small
    /// Markdown subset an Info description is likely to use onto those tags:
    ///  - # / ## / ### headings  -> sized + bold
    ///  - **bold** / __bold__    -> &lt;b&gt;
    ///  - *italic* / _italic_    -> &lt;i&gt;
    ///  - `code`                 -> &lt;i&gt;
    ///  - -, *, + bullet lists   -> "•" prefix
    ///  - [text](url)            -> underlined text (the URL is dropped; we cannot open links here)
    ///
    /// Anything it does not recognise passes through unchanged, so plain text still renders.
    /// </summary>
    public static class MarkdownRichText
    {
        public static string Convert(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return string.Empty;
            }

            string[] lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                builder.Append(ConvertBlock(lines[i]));

                if (i < lines.Length - 1)
                {
                    builder.Append('\n');
                }
            }

            return builder.ToString();
        }

        // Handles line-level (block) syntax — headings and bullets — then inline syntax.
        private static string ConvertBlock(string line)
        {
            string trimmed = line.TrimStart();
            string indent = line.Substring(0, line.Length - trimmed.Length);

            Match heading = Regex.Match(trimmed, @"^(#{1,3})\s+(.*)$");
            if (heading.Success)
            {
                int level = heading.Groups[1].Value.Length;
                int size = level == 1 ? 64 : level == 2 ? 54 : 48;
                string text = ConvertInline(heading.Groups[2].Value);
                return $"{indent}<size={size}><b>{text}</b></size>";
            }

            Match bullet = Regex.Match(trimmed, @"^[-*+]\s+(.*)$");
            if (bullet.Success)
            {
                string text = ConvertInline(bullet.Groups[1].Value);
                return $"{indent}•  {text}";
            }

            return ConvertInline(line);
        }

        // Handles inline emphasis. Order matters: code and links are protected before the
        // emphasis passes, and bold (** / __) runs before italic (* / _) so they don't clash.
        private static string ConvertInline(string text)
        {
            text = Regex.Replace(text, "`([^`]+)`", "<i>$1</i>");
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]*\)", "<u>$1</u>");
            text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "<b>$1</b>");
            text = Regex.Replace(text, "__([^_]+)__", "<b>$1</b>");
            text = Regex.Replace(text, @"(?<!\*)\*(?!\*)([^*]+?)\*(?!\*)", "<i>$1</i>");
            text = Regex.Replace(text, @"(?<!_)_(?!_)([^_]+?)_(?!_)", "<i>$1</i>");
            return text;
        }
    }
}
