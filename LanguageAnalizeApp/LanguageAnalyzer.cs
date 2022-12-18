using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LanguageAnalyzeApp
{
    public class LanguageAnalyzer
    {
        private class LanguageData
        {
            public Dictionary<string, int> NGramsDict { get; set; }
            public long FullTextLength { get; set; }
        }

        private readonly Dictionary<string, LanguageData> _languages;
        private readonly int _maxNGramLength;


        public LanguageAnalyzer(IEnumerable<string> dictPaths)
        {
            _languages = new Dictionary<string, LanguageData>();

            foreach (string filePath in dictPaths)
            {
                if (!File.Exists(filePath))
                {
                    continue;
                }

                try
                {
                    string langCode = Path.GetFileNameWithoutExtension(filePath);

                    using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open), Encoding.Unicode))
                    {
                        string text = reader.ReadToEnd();
                        LanguageData languge = JsonConvert.DeserializeObject<LanguageData>(text);
                        _languages.Add(langCode, languge);
                    }
                }
                catch
                {
                    // Nothing to do here
                }
            }

            if (_languages.Count == 0)
            {
                throw new ArgumentException("Dictionaries weren't found.");
            }

            _maxNGramLength = _languages.Select(pair => pair.Value.NGramsDict.Max(x => x.Key.Length)).Min();
        }

        public Dictionary<string, double> AnalyzeLanguages(string text, int ngramStartLength)
        {
            if (ngramStartLength > _maxNGramLength)
            {
                throw new ArgumentOutOfRangeException("Ngram start length can't be less than max ngram length.");
            }

            Dictionary<string, double> result = new Dictionary<string, double>();

            long minLength = _languages.Values.Min(x => x.FullTextLength);
            foreach (KeyValuePair<string, LanguageData> language in _languages)
            {
                long sum = 0;
                for (int i = ngramStartLength; i <= _maxNGramLength; i++)
                {
                    sum += CountScore(text, language.Value, i);
                }

                result.Add(language.Key, (double)sum / language.Value.FullTextLength);
            }

            return result;
        }

        private static long CountScore(string text, LanguageData language, int ngramLength)
        {
            long counter = 0;
            for (int i = 0; i < text.Length - ngramLength + 1; i++)
            {
                string substring = GetNgram(text.Substring(i, ngramLength));

                if (substring != null)
                {
                    counter += language.NGramsDict.ContainsKey(substring) ? language.NGramsDict[substring] : 0;
                }
            }

            return counter * GetScoreModifier(ngramLength);
        }

        private static string GetNgram(string ngram)
        {
            ngram = ngram.ToLower();

            if (ngram.Length == 1)
            {
                if (!char.IsLetter(ngram[0]))
                {
                    return null;
                }

                return ngram;
            }

            char[] chars = ngram.ToArray();
            if (!char.IsLetter(chars[0]))
            {
                chars[0] = '*';
            }
            if (!char.IsLetter(chars[chars.Length - 1]))
            {
                chars[chars.Length - 1] = '*';
            }

            if (chars.Length == 2 && chars[0] == '*' && chars[chars.Length - 1] == '*')
            {
                return null;
            }

            if (chars.Where(c => char.IsLetter(c)).Count() > 2)
            {
                return null;
            }

            return new string(chars);
        }

        private static long GetScoreModifier(int length)
        {
            // (There should be super smart modifier calculation method for different nGrams, but whatever)
            // Exponentian fuction is used for super boost of long n-grams which actually help to determine language in most
            return (long)Math.Pow(length, length + 10);
        }
    }
}
