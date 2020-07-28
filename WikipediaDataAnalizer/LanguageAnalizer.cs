using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace WikipediaDataAnalizer
{
    enum InputMode
    {
        XML,
        PlainText
    }

    enum SpeedMode
    {
        Normal,
        Fast
    }

    class LanguageAnalizer
    {
        private List<string> WrongArticleNames = new List<string>() {
            "User",
            "Talk",
            "Help",
            "MediaWiki",
            "Wikipedia",

            "Portail",
            "Wikipédia",

            "Шаблон",
            "Википедия"
        };

        private Dictionary<string, int> NGramsDict { get; set; }
        private long FullTextLength { get; set; }

        public string FromFilePath { get; set; }
        public string ToFilePath { get; set; }
        public InputMode InputMode { get; set; }
        public SpeedMode SpeedMode { get;set;}
        public List<char> CharList { get; set; }
        public int MaxNGramLength { get; set; }

        public int SubresultCount { get; set; }

        public LanguageAnalizer(List<char> char_list, string from_file_path, string to_file_path, InputMode input_mode = InputMode.XML, SpeedMode speed_mode = SpeedMode.Normal, int max_ngram_len = 3)
        {
            NGramsDict = new Dictionary<string, int>();
            FullTextLength = 0;

            FromFilePath = from_file_path;
            ToFilePath = to_file_path;
            InputMode = input_mode;
            SpeedMode = speed_mode;
            CharList = char_list;
            MaxNGramLength = max_ngram_len;

            SubresultCount = 5000;
        }

        public void Analize()
        {
            switch (InputMode)
            {
                case InputMode.XML:
                    AnalazeLanguageXML();
                    break;
                case InputMode.PlainText:
                    AnalizePlainText();
                    break;
            }
        }

        public void Analize(InputMode mode)
        {
            InputMode = mode;
            Analize();
        }

        private void AnalazeLanguageXML()
        {
            string line;
            bool is_block = false;
            StringBuilder block = new StringBuilder();

            using (StreamReader reader = new StreamReader(new FileStream(FromFilePath, FileMode.Open)))
            {
                while (true)
                {
                    line = reader.ReadLine();
                    if (line == null)
                        break;

                    if (line.Contains("<page>"))
                        is_block = true;

                    if (is_block)
                        block.Append(line);

                    if (line.Contains("</page>"))
                    {
                        is_block = false;
                        AnalizeLanguageBlockXML(block.ToString());
                        block = new StringBuilder();
                    }
                }
            }

            WriteResult();
        }

        private void AnalizePlainText()
        {
            int counter = 0;

            foreach(string folder in Directory.GetDirectories(FromFilePath))
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

                            for (int i = 1; i <= MaxNGramLength; i++)
                            {
                                AddResult(text.ToString(), i);
                            }
                        }

                        counter++;
                        if (counter == SubresultCount)
                        {
                            counter = 0;
                            WriteResult();
                        }
                    }
                }
            }

            WriteResult();
        }

        private void AnalizeLanguageBlockXML(string block)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(block);
            XmlNode page = document.FirstChild;
            XmlNode text_node = page.FirstChild;
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
                            text_node = inner_node;
                            break;
                        }
                    }
                    break;
                }
            }

            if (perform && text_node.InnerText.Length > 1 && text_node.InnerText[0] != '#')
            {
                StringBuilder text = null;
                switch (SpeedMode)
                {
                    case SpeedMode.Normal:
                        text = RemoveTrashXML(new StringBuilder().Append(text_node.InnerText));
                        break;
                    case SpeedMode.Fast:
                        text = RemoveTrashXMLFast(new StringBuilder().Append(text_node.InnerText));
                        break;
                }

                FullTextLength += text.Length;

                for (int i = 1; i <= MaxNGramLength; i++)
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
            if(text.ToString().Any(c => !CharList.Contains(c.ToString().ToLower()[0])))
            {
                foreach(char c in text.ToString().Where(c => !CharList.Contains(c.ToString().ToLower()[0])))
                {
                    text.Replace(c.ToString(), " ");
                }
            }

            return text;
        }

        private StringBuilder RemoveTrashXMLFast(StringBuilder text)
        {
            // Removing other
            if (text.ToString().Any(c => !CharList.Contains(c.ToString().ToLower()[0])))
            {
                foreach (char c in text.ToString().Where(c => !CharList.Contains(c.ToString().ToLower()[0])))
                {
                    text.Replace(c.ToString(), " ");
                }
            }

            return text;
        }

        private StringBuilder RemoveTrashPlainText(StringBuilder text)
        {
            while (text.ToString().IndexOf('[') != -1)
            {
                int start_position = text.ToString().IndexOf('[');
                int end_position = text.ToString().IndexOf(']', start_position);
                if (end_position != -1)
                    text = text.Remove(start_position, end_position - start_position + 1);
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

            if (text.ToString().Any(c => !CharList.Contains(c.ToString().ToLower()[0])))
            {
                foreach (char c in text.ToString().Where(c => !CharList.Contains(c.ToString().ToLower()[0])))
                {
                    text.Replace(c.ToString(), " ");
                }
            }

            return text;
        }

        private void AddResult(string text, int length)
        {
            string substring = string.Empty;
            for (int i = 0; i < text.Length - length + 1; i++)
            {
                substring = ModifyNGram(text.Substring(i, length));
                if (substring != null)
                {
                    if (!NGramsDict.ContainsKey(substring))
                    {
                        NGramsDict.Add(substring, 1);
                    }
                    else
                    {
                        NGramsDict[substring]++;
                    }
                }
            }
        }

        private string ModifyNGram(string ngram)
        {
            ngram = ngram.ToLower();

            if (ngram.Length == 1)
            {
                if (!(CharList.Contains(ngram[0]) && char.IsLetter(ngram[0])))
                    return null;

                return ngram;
            }

            if (ngram.Length == 2)
            {
                string first = (CharList.Contains(ngram[0]) && char.IsLetter(ngram[0])) ? ngram[0].ToString() : "*";
                string last = (CharList.Contains(ngram[ngram.Length - 1]) &&char.IsLetter(ngram[ngram.Length - 1])) ? ngram[ngram.Length - 1].ToString() : "*";

                if (first == "*" && last == "*")
                    return null;

                return first + last;
            }
            else
            {
                string first = (CharList.Contains(ngram[0]) && char.IsLetter(ngram[0])) ? ngram[0].ToString() : "*";
                string last = (CharList.Contains(ngram[ngram.Length - 1]) && char.IsLetter(ngram[ngram.Length - 1])) ? ngram[ngram.Length - 1].ToString() : "*";
                string center = new string(ngram.Skip(1).Take(ngram.Length - 2).ToArray());

                foreach (char c in center)
                {
                    if (!CharList.Contains(c) || !char.IsLetter(c))
                        return null;
                }

                return first + center + last;
            }
        }

        private void WriteResult()
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(ToFilePath, FileMode.Create), Encoding.Unicode))
            {
                foreach(char c in JsonConvert.SerializeObject(new { NGramsDict = NGramsDict, FullTextLength = FullTextLength }))
                {
                    writer.Write(c);
                }
            }
        }
    }
}
