using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LanguageAnalizeApp
{
    public class LanguageAnalyzer
    {
        private class LanguageData
        {
            public Dictionary<string, int> NGramsDict { get; set; }
            public long FullTextLength { get; set; }
        }
        
        private Dictionary<string, LanguageData> Languages { get; }
        private int MaxNGramLength { get; set; }

        public LanguageAnalyzer()
        {
            Languages = new Dictionary<string, LanguageData>();

            try
            {
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
            catch
            {
                LanguageData enData = new LanguageData();
                enData.NGramsDict = new Dictionary<string, int>();
                Languages.Add("en", enData);

                LanguageData ruData = new LanguageData();
                ruData.NGramsDict = new Dictionary<string, int>();
                Languages.Add("ru", ruData);
            }
        }

        public Dictionary<string, double> AnalizeLanguages(string text, int ngram_start_length)
        {
            if (ngram_start_length > MaxNGramLength)
            {
                //throw new Exception("Ngram start length can't be less than max ngram length.");

                Dictionary<string, double> r = new Dictionary<string, double>();

                r.Add("en", 0.5);
                r.Add("ru", 0.3);
                r.Add("bg", 0.3);
                r.Add("fr", 0.3);
                r.Add("it", 0.3);

                return r;
            }

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
