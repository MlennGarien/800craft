
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
                sb.AppendLine("# This file should be filled with each word you want the profanity filter to change to asterixis");
                sb.AppendLine("# Each word should be on a new line all by itself");
                File.WriteAllText("SwearWords.txt", sb.ToString());
            }

            var tempSwearWords = File.ReadAllLines("SwearWords.txt").Where(line => line.StartsWith("#") == false || line.Trim().Equals(String.Empty));
            SwearWords = from sw in tempSwearWords where !sw.StartsWith("#") select Reduce(sw.ToLower());
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
                bool swearwordfound = false;
                foreach (string swearword in SwearWords)
                {
                    if (reducedWords[i].Contains(swearword))
                    {
                        swearwordfound = true;
                        break;
                    }
                }

                if (swearwordfound)
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
