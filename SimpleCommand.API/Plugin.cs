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
        public static ConfigEntry<bool> ConfigSimpleCommandSortCommands;
        public static ConfigEntry<bool> ConfigSimpleCommandLogging;
        public static ManualLogSource Log;
        private readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        private void Awake()
        {
            Log = Logger;
            ConfigSimpleCommandSortCommands = Config.Bind("General", "SortCommandsAlphabetically", false, "By default, commands are sorted by plugin. Set the value to 'true' if you want them to be sorted alphabetically.");
            ConfigSimpleCommandLogging = Config.Bind("Developer", "EnableDebugLogging", false, "Enables logging of every SimpleCommand.API action.");

            if (ConfigSimpleCommandLogging.Value)
            {
                Log.LogDebug("Debug Logging enabled. Logging..");
                if (ConfigSimpleCommandSortCommands.Value)
                {
                    Log.LogDebug("List of commands is now sorted alphabetically.");
                }
            }

            _harmony.PatchAll();

            // Command setup
            PrepareSetup();

            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        // Initialize default api commands
        private void PrepareSetup()
        {
            var commandListModule = new SimpleCommandModule
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
            var list = "All available mod commands: \n";
            var node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;

            var input = SimpleCommand.GetInputValue(__instance, 1)[0];

            var requestedPageNum = 1;
            var highestPageNumber = 1;

            if (int.TryParse(input, out var result))
            {
                requestedPageNum = result > 1 ? result : 1;
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
                    list = stringList.Where(str => !string.IsNullOrEmpty(str)).Aggregate(list, (current, str) => current + str);
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
        internal static List<SimpleCommandModule> SimpleCommandList = new();

        // Dictionary with commands for terminal sorted by page
        internal static SortedDictionary<int, List<string>> SimpleCommandDictionary = new();
        
        // Method to add a module to the command list
        public static void AddSimpleCommand(SimpleCommandModule module)
        {

            if (module.DisplayName != null)
            {
                SimpleCommandList.Add(module);
                if (Plugin.ConfigSimpleCommandLogging.Value)
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
            foreach(var module in modules)
            {
                AddSimpleCommand(module);
            }
        }

        // Remove every punctuation from player sentence input
        public static string RemovePunctuation(string s)
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in s)
            {
                if (!char.IsPunctuation(c)) stringBuilder.Append(c);
            }
            return stringBuilder.ToString().ToLower();
        }

        public static string GetInputValue(Terminal terminal)
        {
            var input = RemovePunctuation(terminal.screenText.text.Substring(terminal.screenText.text.Length - terminal.textAdded));
            if (Plugin.ConfigSimpleCommandLogging.Value)
            {
                Plugin.Log.LogDebug("GetInputValue:" + input);
            }
            return input;
        }

        public static string[] GetInputValue(Terminal terminal, int index)
        {
            var input = RemovePunctuation(terminal.screenText.text.Substring(terminal.screenText.text.Length - terminal.textAdded));
            var inputArray = input.Split(" ");
            var filteredArray = inputArray.Skip(Math.Max(0, inputArray.Length - index)).Take(index).ToArray();
            if (Plugin.ConfigSimpleCommandLogging.Value)
            {
                var i = 1;
                Plugin.Log.LogDebug("GetInputValue Array:");
                foreach (var str in filteredArray)
                {
                    Plugin.Log.LogDebug(i + ": " + str);
                    i++;
                }
            }
            return filteredArray;
        }
    }
}