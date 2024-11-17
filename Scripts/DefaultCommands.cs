using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Less3.Terminal
{
    public static class DefaultCommands
    {
        [Command("help")]
        public static void Help(string[] args)
        {
            Log.Print("------------------------");
            Log.Print("Welcome to the terminal!");
            Log.Print("------------------------");
            Log.Print("");
            Log.Print("You can trigger commands defined in c# with a [Command] attribute.");
            Log.Print("Commands must be static and have a string[] parameter.");
            Log.Print("");
            Log.Print("To view a list of all commands, enter 'commands'");
        }

        [Command("commands")]
        public static void Commands(string[] args)
        {
            Log.Print("Commands:");
            var commands = TerminalManager.GetCommands();
            foreach (var command in commands)
            {
                Log.Print(command);
            }
        }
    }
}
