using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace WikipediaDataParser
{
    internal class WikipediaArticleParser
    {
        public enum FileFormat
        {
            Xml,
            PlainText
        }

        public enum SpeedMode
        {
            Normal,
            Fast
        }

        public struct ParseResult
        {
            public Dictionary<string, int> NgramsDict;
            public long FullTextLength;
        }

        private static ImmutableArray<string> WrongArticleNames = ImmutableArray.Create 
        (
            "User",
            "Talk",
            "Help",
            "MediaWiki",
            "Wikipedia",

            "Portail",
            "Wikipédia",

            "Шаблон",
            "Википедия"
        );

        private const int SubresultCount = 5000;

        private readonly CultureInfo _cultureInfo;
        private readonly int _maxNgramLength;
        private readonly List<char> _charList;

        private long FullTextLength { get; set; }

        private Dictionary<string, int> NgramsDict { get; set; } = new Dictionary<string, int>();

        public string SourcePath { get; set; }
        public SpeedMode SMode { get;set;}


        public WikipediaArticleParser(CultureInfo cultureInfo, int maxNgramLength = 3)
        {
            _cultureInfo = cultureInfo;
            _maxNgramLength = maxNgramLength;

            _charList = Utils.GetCharacters(cultureInfo);
        }


        public ParseResult Parse(string sourcePath, FileFormat fileMode, SpeedMode speedMode = SpeedMode.Fast)
        {
            SourcePath = sourcePath;
            SMode = speedMode;

            FullTextLength = 0;

            switch (fileMode)
            {
                case FileFormat.Xml:
                    AnalazeLanguageXML();
                    break;
                case FileFormat.PlainText:
                    AnalizePlainText();
                    break;
            }

            return new ParseResult() { NgramsDict = NgramsDict, FullTextLength = FullTextLength };
        }

        private void AnalazeLanguageXML()
        {
            string line;
            bool isBlock = false;
            StringBuilder block = new StringBuilder();

            using (StreamReader reader = new StreamReader(new FileStream(SourcePath, FileMode.Open)))
            {
                while (true)
                {
                    line = reader.ReadLine();
                    if (line == null)
                        break;

                    if (line.Contains("<page>"))
                        isBlock = true;

                    if (isBlock)
                        block.Append(line);

                    if (line.Contains("</page>"))
                    {
                        isBlock = false;
                        AnalizeLanguageBlockXML(block.ToString());
                        block = new StringBuilder();
                    }
                }
            }
        }

        private void AnalizePlainText()
        {
            int counter = 0;

            foreach(string folder in Directory.GetDirectories(SourcePath))
            {
                foreach(string filename in Directory.GetFiles(folder))
                {
                    if (!WrongArticleNames.Any(x => filename.Contains(x)))
                    {
                        Console.WriteLine(filename);

                        using (StreamReader reader = new StreamReader(new FileStream(filename, FileMode.Open), Encoding.Default))
                        {
                            StringBuilder text = new StringBuilder(reader.ReadToEnd());
                            text = RemoveTrashPlainText(text);

                            FullTextLength += text.Length;

                            for (int i = 1; i <= _maxNgramLength; i++)
                            {
                                AddResult(text.ToString(), i);
                            }
                        }

                        counter++;
                        if (counter == SubresultCount)
                        {
                            counter = 0;
                            return;
                        }
                    }
                }
            }

            return;
        }

        private void AnalizeLanguageBlockXML(string block)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(block);
            XmlNode page = document.FirstChild;
            XmlNode textNode = page.FirstChild;
            bool perform = true;
            foreach (XmlNode outer_node in page.ChildNodes)
            {
                if (outer_node.Name == "title")
                {
                    if (WrongArticleNames.Any(x => outer_node.InnerText.Contains(x)) || outer_node.InnerText.Contains(":"))
                    {
                        perform = false;
                        break;
                    }
                    else
                    {
                        Console.WriteLine(outer_node.InnerText);
                    }
                }

                if (outer_node.Name == "revision")
                {
                    foreach (XmlNode inner_node in outer_node.ChildNodes)
                    {
                        if (inner_node.Name == "text")
                        {
                            textNode = inner_node;
                            break;
                        }
                    }
                    break;
                }
            }

            if (perform && textNode.InnerText.Length > 1 && textNode.InnerText[0] != '#')
            {
                StringBuilder text = null;
                switch (SMode)
                {
                    case SpeedMode.Normal:
                        text = RemoveTrashXML(new StringBuilder().Append(textNode.InnerText));
                        break;
                    case SpeedMode.Fast:
                        text = RemoveTrashXMLFast(new StringBuilder().Append(textNode.InnerText));
                        break;
                }

                FullTextLength += text.Length;

                for (int i = 1; i <= _maxNgramLength; i++)
                {
                    AddResult(text.ToString(), i);
                }
            }
        }

        private StringBuilder RemoveTrashXML(StringBuilder text)
        {
            // Removing ':{{', '{{{', '}}}', '}}}}', '== =='
            text = text.Replace(":{{", "{{").Replace("{{{", "{{").Replace("}}}", "").Replace("}}}}", "}}");

            // Removing 'wrapers' without depth
            List<Tuple<string, string>> wrapers = new List<Tuple<string, string>>() {
                new Tuple<string, string>("==", "=="),
                new Tuple<string, string>("=", "="),
                new Tuple<string, string>("<ref>", "</ref>"),
                new Tuple<string, string>("[[File:", ".]]"),
                new Tuple<string, string>("[[Image:", "]]"),
                new Tuple<string, string>("[[wikt:", "]]") };
            foreach (Tuple<string, string> wraper in wrapers)
            {
                while (text.ToString().IndexOf(wraper.Item1) != -1)
                {
                    int start_position = text.ToString().IndexOf(wraper.Item1);
                    int end_position = text.ToString().IndexOf(wraper.Item2, start_position);
                    int check_position = start_position;

                    if (end_position != -1)
                        text = text.Remove(start_position, end_position - start_position + wraper.Item2.Length);
                    else
                        break;
                }
            }

            // Removing 'wrapers'
            List<Tuple<string, string>> deep_wrapers = new List<Tuple<string, string>>() {
                new Tuple<string, string>("[[File:", "]]"),
                new Tuple<string, string>("{{", "}}"),
                new Tuple<string, string>("{|", "|}"),
                new Tuple<string, string>("(", ")")};
            foreach (Tuple<string, string> wraper in deep_wrapers)
            {
                while (text.ToString().IndexOf(wraper.Item1) != -1)
                {
                    int start_position = text.ToString().IndexOf(wraper.Item1);
                    int end_position = text.ToString().IndexOf(wraper.Item2, start_position);
                    int check_position = start_position;
                    while (true)
                    {
                        check_position = text.ToString().IndexOf(wraper.Item1, check_position + 1);
                        if (end_position > check_position && check_position != -1)
                        {
                            end_position = text.ToString().IndexOf(wraper.Item2, end_position + 1);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (end_position != -1)
                    {
                        text = text.Remove(start_position, end_position - start_position + wraper.Item2.Length);
                    }
                    else
                        break;
                }
            }

            // Removing '[[ ]]'
            while (text.ToString().IndexOf("[[") != -1)
            {
                int start_position = text.ToString().IndexOf("[[");
                int end_position = text.ToString().IndexOf("]]", start_position);
                int devider_position = text.ToString().IndexOf("|", start_position);
                if (end_position != -1)
                {
                    if (devider_position > start_position && devider_position < end_position)
                    {
                        text = text.Remove(devider_position, end_position - devider_position + 2);
                        text = text.Remove(start_position, 2);
                    }
                    else
                    {
                        text = text.Remove(end_position, 2);
                        text = text.Remove(start_position, 2);
                    }
                }
                else
                    break;
            }

            // Removing '[ ]'
            while (text.ToString().IndexOf("[") != -1)
            {
                int start_position = text.ToString().IndexOf("[");
                int end_position = text.ToString().IndexOf("]", start_position);
                int check_position = start_position;

                if (end_position != -1)
                {
                    text = text.Remove(start_position, end_position - start_position + 1);
                }
                else
                    break;
            }

            // Removing other
            if(text.ToString().Any(c => !_charList.Contains(c.ToString().ToLower()[0])))
            {
                foreach(char c in text.ToString().Where(c => !_charList.Contains(c.ToString().ToLower()[0])))
                {
                    text.Replace(c.ToString(), " ");
                }
            }

            return text;
        }

        private StringBuilder RemoveTrashXMLFast(StringBuilder text)
        {
            // Removing other
            if (text.ToString().Any(c => !_charList.Contains(char.ToLower(c))))
            {
                foreach (char c in text.ToString().Where(c => !_charList.Contains(char.ToLower(c))))
                {
                    text.Replace(c.ToString(), "");
                }
            }

            return text;
        }

        private StringBuilder RemoveTrashPlainText(StringBuilder text)
        {
            while (text.ToString().IndexOf('[') != -1)
            {
                int startPosition = text.ToString().IndexOf('[');
                int endPosition = text.ToString().IndexOf(']', startPosition);
                if (endPosition != -1)
                    text = text.Remove(startPosition, endPosition - startPosition + 1);
                else
                    break;
            }

            while (text.ToString().IndexOf('<') != -1)
            {
                int start_position = text.ToString().IndexOf('<');
                int end_position = text.ToString().IndexOf('>', start_position);
                if (end_position != -1)
                    text = text.Remove(start_position, end_position - start_position + 1);
                else
                    break;
            }

            text = text.Replace("==", "");

            if (text.ToString().Any(c => !_charList.Contains(c.ToString().ToLower()[0])))
            {
                foreach (char c in text.ToString().Where(c => !_charList.Contains(c.ToString().ToLower()[0])))
                {
                    text.Replace(c.ToString(), " ");
                }
            }

            return text;
        }

        private void AddResult(string text, int length)
        {
            for (int i = 0; i < text.Length - length + 1; i++)
            {
                string substring = ModifyNGram(text.Substring(i, length));
                if (substring != null)
                {
                    if (!NgramsDict.ContainsKey(substring))
                    {
                        NgramsDict.Add(substring, 1);
                    }
                    else
                    {
                        NgramsDict[substring]++;
                    }
                }
            }
        }

        private string ModifyNGram(string ngram)
        {
            ngram = ngram.ToLower();

            if (ngram.Length == 1)
            {
                if (!(_charList.Contains(ngram[0]) && char.IsLetter(ngram[0])))
                    return null;

                return ngram;
            }

            if (ngram.Length == 2)
            {
                string first = (_charList.Contains(ngram[0]) && char.IsLetter(ngram[0])) ? ngram[0].ToString() : "*";
                string last = (_charList.Contains(ngram[ngram.Length - 1]) &&char.IsLetter(ngram[ngram.Length - 1])) ? ngram[ngram.Length - 1].ToString() : "*";

                if (first == "*" && last == "*")
                    return null;

                return first + last;
            }
            else
            {
                string first = (_charList.Contains(ngram[0]) && char.IsLetter(ngram[0])) ? ngram[0].ToString() : "*";
                string last = (_charList.Contains(ngram[ngram.Length - 1]) && char.IsLetter(ngram[ngram.Length - 1])) ? ngram[ngram.Length - 1].ToString() : "*";
                string center = new string(ngram.Skip(1).Take(ngram.Length - 2).ToArray());

                foreach (char c in center)
                {
                    if (!_charList.Contains(c) || !char.IsLetter(c))
                        return null;
                }

                return first + center + last;
            }
        }
    }
}
