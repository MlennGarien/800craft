
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace fCraft
{
    static class ProfanityFilter
    {
        private static Dictionary<string, string> Reducer;
        private static IEnumerable<string> SwearWords;
        public static void Init()
        {
            Reducer = new Dictionary<string, string>();
            Reducer.Add("a", "[@]");
            Reducer.Add("b", "i3|l3");
            Reducer.Add("c", "[(]");
            Reducer.Add("e", "[3]");
            Reducer.Add("f", "ph");
            Reducer.Add("g", "[6]");
            Reducer.Add("h", "#");
            
            Reducer.Add("i", "[l!1]");
            Reducer.Add("o", "[0]");
            Reducer.Add("q", "[9]");
            Reducer.Add("s", "[$5]");
            Reducer.Add("w", "vv");
            Reducer.Add("z", "[2]");

            if (!File.Exists("SwearWords.txt"))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# This file contains a list of bad words to remove via the profanity filter");
                sb.AppendLine("# Each bad word should be on a new line all by itself");
                File.WriteAllText("SwearWords.txt", sb.ToString());
            }

            var tempSwearWords = File.ReadAllLines("SwearWords.txt").Where(s => s.StartsWith("#") == false || s.Trim().Equals(String.Empty));
            SwearWords = from s in tempSwearWords where !s.StartsWith("#") select Reduce(s.ToLower());
        }

        public static string Parse(string text)
        {
            return ParseMatchPartialWords(text);
        }

        
        private static string ParseMatchWholeWords(string text)
        {
            var result = new List<string>();
            var originalWords = text.Split(' ');
            var reducedWords = Reduce(text).Split(' ');
            for (var i = 0; i < originalWords.Length; i++)
            {
                if (SwearWords.Contains(reducedWords[i].ToLower()))
                {
                    
                    result.Add(new String('*', originalWords[i].Length));
                }
                else
                {
                    result.Add(originalWords[i]);
                }
            }

            return String.Join(" ", result.ToArray());
        }

        
        private static string ParseMatchPartialWords(string text)
        {
            var result = new List<string>();
            var originalWords = text.Split(' ');
            var reducedWords = Reduce(text).Split(' ');

            
            for (int i = 0; i < reducedWords.Length; i++)
            {
                bool badwordfound = false;
                foreach (string badword in SwearWords)
                {
                    if (reducedWords[i].Contains(badword))
                    {
                        badwordfound = true;
                        break;
                    }
                }

                if (badwordfound)
                {
                   
                    result.Add(new String('*', originalWords[i].Length));
                }
                else
                {
                    
                    result.Add(originalWords[i]);
                }
            }

            return String.Join(" ", result.ToArray());
        }


        private static string Reduce(string text)
        {
            text = text.ToLower();
            foreach (var pattern in Reducer)
            {
                text = Regex.Replace(text, pattern.Value, pattern.Key);
            }
            return text;
        }
    }
}
