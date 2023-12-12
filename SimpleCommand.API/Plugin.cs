using System.Text;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SimpleCommand.API.Classes;
using UnityEngine;
using System.Linq;
using System;
using BepInEx.Configuration;

namespace SimpleCommand.API
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Lethal Company.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> configSimpleCommandSortCommands;
        public static ConfigEntry<bool> configSimpleCommandLogging;
        public static ManualLogSource Log;
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        private void Awake()
        {
            Log = Logger;
            configSimpleCommandSortCommands = Config.Bind("General", "SortCommandsAlphabetically", false, "By default, commands are sorted by plugin. Set the value to 'true' if you want them to be sorted alphabetically.");
            configSimpleCommandLogging = Config.Bind("Developer", "EnableDebugLogging", false, "Enables logging of every SimpleCommand.API action.");

            if (configSimpleCommandLogging.Value)
            {
                Log.LogDebug("Debug Logging enabled. Logging..");
                if (configSimpleCommandSortCommands.Value)
                {
                    Log.LogDebug("List of commands is now sorted alphabetically.");
                }
            }

            harmony.PatchAll();

            // Command setup
            PrepareSetup();

            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        // Initialize default api commands
        private void PrepareSetup()
        {
            SimpleCommandModule commandListModule = new SimpleCommandModule
            {
                DisplayName = "modcommands",
                HideFromCommandList = true,
                HasDynamicInput = true,
                Method = ShowModCommands,
                Abbreviations = new[] {"mdc", "modc", "modcmds"}
            };

            SimpleCommand.AddSimpleCommand(commandListModule);
            
        }

        // Method to show all commands in list
        private static TerminalNode ShowModCommands(Terminal __instance)
        {
            string list = "All available mod commands: \n";
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;

            string input = SimpleCommand.GetInputValue(__instance, 1)[0];

            int requestedPageNum = 1;
            int highestPageNumber = 1;

            if (int.TryParse(input, out int result))
            {
                if (result > 1)
                {
                    requestedPageNum = result;
                }
                else
                {
                    requestedPageNum = 1;
                }
            }

            if (SimpleCommand.SimpleCommandDictionary != null && SimpleCommand.SimpleCommandDictionary.Count > 0)
            {
                highestPageNumber = SimpleCommand.SimpleCommandDictionary.Last().Key;

                if (requestedPageNum > highestPageNumber)
                {
                    requestedPageNum = highestPageNumber;
                }

                if (SimpleCommand.SimpleCommandDictionary.TryGetValue(requestedPageNum, out var stringList))
                {
                    foreach (string str in stringList)
                    {
                        if (!string.IsNullOrEmpty(str))
                        {
                            list += str;
                        }
                    }
                }

            }

            list += "\nPage " + requestedPageNum + " of " + highestPageNumber + "\n";

            node.displayText = list;

            return node;
        }
    }

    public static class SimpleCommand
    {
        // List where all commands get saved in
        internal static List<SimpleCommandModule> SimpleCommandList = new List<SimpleCommandModule>();

        // Dictionary with commands for terminal sorted by page
        internal static SortedDictionary<int, List<string>> SimpleCommandDictionary = new();
        
        // Method to add a module to the command list
        public static void AddSimpleCommand(SimpleCommandModule module)
        {

            if (module.DisplayName != null)
            {
                SimpleCommandList.Add(module);
                if (Plugin.configSimpleCommandLogging.Value)
                {
                    Plugin.Log.LogDebug("Module " + module.DisplayName + " was successfully registered by SimpleCommand.API.");
                }
            }
            else
            {
                Plugin.Log.LogWarning("Warning, DisplayName of module not assigned. Module will not load!");
            }
        }

        // Additional way to add commands with array
        public static void AddSimpleCommands(SimpleCommandModule[] modules)
        {
            foreach(SimpleCommandModule module in modules)
            {
                AddSimpleCommand(module);
            }
        }

        // Remove every punctuation from player sentence input
        public static string RemovePunctuation(string s)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in s)
            {
                if (!char.IsPunctuation(c)) stringBuilder.Append(c);
            }
            return stringBuilder.ToString().ToLower();
        }

        public static string GetInputValue(Terminal terminal)
        {
            string input = RemovePunctuation(terminal.screenText.text.Substring(terminal.screenText.text.Length - terminal.textAdded));
            if (Plugin.configSimpleCommandLogging.Value)
            {
                Plugin.Log.LogDebug("GetInputValue:" + input);
            }
            return input;
        }

        public static string[] GetInputValue(Terminal terminal, int index)
        {
            string input = RemovePunctuation(terminal.screenText.text.Substring(terminal.screenText.text.Length - terminal.textAdded));
            string[] inputArray = input.Split(" ");
            string[] filteredArray = inputArray.Skip(Math.Max(0, inputArray.Length - index)).Take(index).ToArray();
            if (Plugin.configSimpleCommandLogging.Value)
            {
                int i = 1;
                Plugin.Log.LogDebug("GetInputValue Array:");
                foreach (string str in filteredArray)
                {
                    Plugin.Log.LogDebug(i + ": " + str);
                    i++;
                }
            }
            return filteredArray;
        }
    }
}