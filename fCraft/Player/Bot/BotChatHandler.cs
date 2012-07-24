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
        public Player player;

        /// <summary>
        /// Create a new instance of the ALICE object
        /// </summary>
        public Alice()
        {
            myBot = new AIMLbot.Bot();
            myUser = new User("Player", myBot);
            Initialize();
        }

        public Alice(Player player_)
        {
            player = player_;
            myBot = new AIMLbot.Bot();
            myUser = new User(player.Name, myBot);//y wont u set my name
            Initialize();
        }

        /// <summary>
        /// This initialization can be put in the alice() method
        /// but I kept it seperate due to the nature of my program.
        /// This method loads all the AIML files located in the \AIML folder
        /// </summary>
        public void Initialize()
        {
            myBot.loadSettings("/botconfig/");
            myBot.isAcceptingUserInput = false;
            myBot.loadAIMLFromFiles();
            myBot.isAcceptingUserInput = true;
            SetUpSettings();
        }

        public void SetUpSettings()
        {
            myBot.GlobalSettings.addSetting("name", "Jimmy");
            myBot.Chat(new Request("my name is " + player.Name, this.myUser, this.myBot));
            myBot.GlobalSettings.addSetting("master", player.Name);
            myBot.GlobalSettings.addSetting("location", ConfigKey.ServerName.GetString());
            myBot.GlobalSettings.addSetting("birthplace", ConfigKey.ServerName.GetString());
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