//Written by Matt Gonzalez
//with help from Nicholas H.Tollervey's ConsoleBot2.5
//and AIML base files from the A.L.I.C.E Bot project
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIMLbot;

namespace fCraft
{
    public class Alice
    {
        private AIMLbot.Bot myBot;
        private User myUser;

        /// <summary>
        /// Create a new instance of the ALICE object
        /// </summary>
        public Alice()
        {
            myBot = new AIMLbot.Bot();
            myUser = new User("Player", myBot);
            Initialize();
        }

        /// <summary>
        /// This initialization can be put in the alice() method
        /// but I kept it seperate due to the nature of my program.
        /// This method loads all the AIML files located in the \AIML folder
        /// </summary>
        public void Initialize()
        {
            myBot.loadSettings();
            myBot.isAcceptingUserInput = false;
            myBot.loadAIMLFromFiles();
            myBot.isAcceptingUserInput = true;
        }

        /// <summary>
        /// This method takes an input string, then finds a response using the the AIMLbot library and returns it
        /// </summary>
        /// <param name="input">Input Text</param>
        /// <returns>Response</returns>
        public String getOutput(String input)
        {
            Request r = new Request(input, myUser, myBot);
            Result res = myBot.Chat(r);
            return (res.Output);
        }
    }
}