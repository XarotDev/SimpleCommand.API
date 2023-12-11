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
                displayName = "modcommands",
                bHideFromCommandList = true,
                bHasDynamicInput = true,
                method = SimpleCommand.ShowModCommands,
                abbreviations = new[] {"mdc", "modc", "modcmds"}
            };

            SimpleCommand.AddSimpleCommand(commandListModule);
            
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

            if (module.displayName != null)
            {
                SimpleCommandList.Add(module);
                if (Plugin.configSimpleCommandLogging.Value)
                {
                    Plugin.Log.LogDebug("Module " + module.displayName + " was successfully registered by SimpleCommand.API.");
                }
            }
            else
            {
                Plugin.Log.LogWarning("Warning, displayName of module not assigned. Module will not load!");
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

        // Method to show all commands in list
        public static TerminalNode ShowModCommands(Terminal __instance)
        {
            string list = "All available mod commands: \n";
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;

            string input = GetInputValue(__instance, 1)[0];
  
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

            if (SimpleCommandDictionary != null && SimpleCommandDictionary.Count > 0)
            {
                highestPageNumber = SimpleCommandDictionary.Last().Key;

                if (requestedPageNum > highestPageNumber)
                {
                    requestedPageNum = highestPageNumber;
                }

                if (SimpleCommandDictionary.TryGetValue(requestedPageNum, out var stringList))
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

        public static void AddChildrenToList(SimpleCommandModule module, string nestedString, List<string> commands)
        {
            string nestedText = nestedString + " " + module.displayName.ToUpper();
            if (module.bHideFromCommandList == false)
            {
                string text = "\n>" + nestedText;
                if (module.parameter != null && module.bHasDynamicInput)
                {
                    foreach (string parameter in module.parameter)
                    {
                        text += " [" + parameter.ToLower() + "]";
                    }
                }
                if (module.abbreviations != null)
                {
                    text += " (short. ";
                    foreach (string abbreviation in module.abbreviations)
                    {
                        text += abbreviation.ToLower() + ", ";
                    }
                    text = text[..^2] + ")";
                }
                text += "\n";

                if (module.description != null)
                {
                    text += module.description + "\n";
                }
                commands.Add(text);
            }
            if (module.childrenModules != null)
            {
                foreach (SimpleCommandModule childModule in module.childrenModules)
                {
                    AddChildrenToList(childModule, nestedText, commands);
                }
            }
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