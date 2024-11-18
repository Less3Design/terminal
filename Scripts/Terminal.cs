using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Less3.Terminal
{
    public static class Log
    {
        public static void Print(string message, string description = "")
        {
            TerminalManager.OnCommandLogOut?.Invoke(message, description);
        }
    }

    public static class TerminalManager
    {
        /// <summary>
        /// Outputs a log for command input requests
        /// </summary>
        public static Action<string, string> OnCommandInputOut;
        /// <summary>
        /// Output command results. Must use the Terminal.Log class to output this
        /// </summary>
        public static Action<string, string> OnCommandLogOut;

        private static Dictionary<string, MethodInfo> commands = new Dictionary<string, MethodInfo>();

        static TerminalManager()
        {
#if UNITY_EDITOR || LESS3_TERMINAL_ENABLE_AT_RUNTIME
            FindAllCommands();
#endif
        }

#if UNITY_EDITOR || LESS3_TERMINAL_ENABLE_AT_RUNTIME
        private static void FindAllCommands()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods())
                    {
                        foreach (var attribute in method.GetCustomAttributes(true))
                        {
                            if (attribute is CommandAttribute commandAttribute)
                            {
                                if (!method.IsStatic)
                                {
                                    Debug.LogWarning($"Method {method.Name} in {type.Name} is not static and cannot be used as a terminal command.");
                                    continue;
                                }
                                if (method.GetParameters().Length != 1)
                                {
                                    Debug.LogWarning($"Method {method.Name} in {type.Name} does not have exactly one parameter and cannot be used as a terminal command.");
                                    continue;
                                }
                                if (method.GetParameters()[0].ParameterType != typeof(string[]))
                                {
                                    Debug.LogWarning($"Method {method.Name} in {type.Name} does not have a string[] parameter and cannot be used as a terminal command.");
                                    continue;
                                }

                                if (commandAttribute.subCommand == null)
                                {
                                    commands.Add(commandAttribute.command.ToLower(), method);
                                }
                                else
                                {
                                    commands.Add($"{commandAttribute.command.ToLower()}%{commandAttribute.subCommand.ToLower()}", method);
                                }
                            }
                        }
                    }
                }
            }
        }
#endif

        public static void TryCommand(string input)
        {
            string[] args = input.ToLower().Split(' ');
            if (args.Length >= 2)
            {
                // Check for subcommands
                string potentialSubCommand = $"{args[0]}%{args[1]}";
                if (commands.ContainsKey(potentialSubCommand))
                {
                    string[] subArgs = new string[args.Length - 2];
                    Array.Copy(args, 2, subArgs, 0, args.Length - 2);
                    PrintCommandSuccess(input, commands[potentialSubCommand]);
                    commands[potentialSubCommand].Invoke(null, new object[] { subArgs });
                    return;
                }
            }

            if (commands.ContainsKey(args[0]))
            {
                string[] subArgs = new string[args.Length - 1];
                Array.Copy(args, 1, subArgs, 0, args.Length - 1);
                PrintCommandSuccess(input, commands[args[0]]);
                commands[args[0]].Invoke(null, new object[] { subArgs });
            }
            else
            {
                OnCommandInputOut?.Invoke(input + " <color=red>Command not found</color>", "");
            }
        }

        private static void PrintCommandSuccess(string command, MethodInfo method)
        {
            // in the terminal you can click on a command input and see some description.
            string description = "";
            description += method.ReflectedType.Name + "\n";
            description += method.Name + "\n";
            description += "Parameters: " + method.GetParameters()[0].ParameterType.Name + "\n";
            OnCommandInputOut?.Invoke(command, description);
        }

        public static string[] GetCommands()
        {
            string[] keys = commands.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = keys[i].Replace("%", " ");
            }
            return keys;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string command;
        public string subCommand;

        /// <summary>
        /// Define a method as being invokable from the terminal. Teminal commands must be static and have a single string[] args parameter.
        /// </summary>
        public CommandAttribute(string command)
        {
            this.command = command;
            this.subCommand = null;
        }

        /// <summary>
        /// Define a method as being invokable from the terminal. Teminal commands must be static and have a single string[] args parameter.
        /// </summary>
        public CommandAttribute(string command, string subCommand)
        {
            this.command = command;
            this.subCommand = subCommand;
        }
    }
}
