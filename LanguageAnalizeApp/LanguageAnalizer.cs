using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LanguageAnalizeApp
{
    static class LanguageAnalizer
    {
        private class LanguageData
        {
            public Dictionary<string, int> NGramsDict { get; set; }
            public long FullTextLength { get; set; }
        }
        
        private static Dictionary<string, LanguageData> Languages { get; }
        private static int MaxNGramLength { get; set; }

        static LanguageAnalizer()
        {
            Languages = new Dictionary<string, LanguageData>();

            foreach (string file_path in Directory.GetFiles(Directory.GetCurrentDirectory() + @"\languages\"))
            {
                string lang_code = file_path.Substring(Directory.GetCurrentDirectory().Length + 11, 2);

                using (StreamReader reader = new StreamReader(new FileStream(file_path, FileMode.Open), Encoding.Unicode))
                {
                    string text = reader.ReadToEnd();
                    LanguageData languge = JsonConvert.DeserializeObject<LanguageData>(text);
                    Languages.Add(lang_code, languge);
                }
            }

            MaxNGramLength = Languages.First().Value.NGramsDict.Max(x => x.Key.Length);

            if (Languages.Count == 0)
                throw new Exception("Dictionaries weren't found.");
        }

        public static Dictionary<string, double> AnalizeLanguages(string text, int ngram_start_length)
        {
            if (ngram_start_length > MaxNGramLength)
                throw new Exception("Ngram start length can't be less than max ngram length.");

            Dictionary<string, double> result = new Dictionary<string, double>();

            long min_length = Languages.Values.Min(x => x.FullTextLength);
            foreach(KeyValuePair<string, LanguageData> language in Languages)
            {
                double sum = 0;
                for(int i = ngram_start_length; i <= MaxNGramLength; i++)
                {
                    sum += AnalizeText(text, language.Value, i) * ScoreModifier(i);
                }
                result.Add(language.Key, sum / language.Value.FullTextLength * min_length);
            }

            return result;
        }

        private static int AnalizeText(string text, LanguageData language, int ngram_length)
        {
            int counter = 0;
            for(int i = 0; i < text.Length - ngram_length + 1; i++)
            {
                string substring = ModifyNgram(text.Substring(i, ngram_length));

                if (substring != null)
                    counter += language.NGramsDict.ContainsKey(substring) ? language.NGramsDict[substring] : 0;
            }

            return counter;
        }

        private static string ModifyNgram(string ngram)
        {
            ngram = ngram.ToLower();

            if (ngram.Length == 1)
            {
                if (char.IsLetter(ngram[0]))
                    return ngram;
                else
                    return null;
            }

            string first = char.IsLetter(ngram[0]) ? ngram[0].ToString() : "*";
            string last = char.IsLetter(ngram[ngram.Length - 1]) ? ngram[ngram.Length - 1].ToString() : "*";

            if (ngram.Length == 2)
            {
                if (first == "*" && last == "*")
                    return null;

                return first + last;
            }
            else
            {
                string center = ngram.Substring(1, ngram.Length - 2);
                foreach (char c in center)
                {
                    if (!char.IsLetter(c))
                        return null;
                }
                return first + center + last;
            }
        }

        private static int ScoreModifier(int length)
        {
            return 1;
            //return Enumerable.Range(1, length).Aggregate((a, b) => a * b);
        }
    }
}
