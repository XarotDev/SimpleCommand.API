using HarmonyLib;
using SimpleCommand.API.Classes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SimpleCommand.API.SimpleCommand;

namespace SimpleCommand.API.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    public class SimpleCommandPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void PatchAwake(ref Terminal __instance)
        {
            var inputField = __instance.screenText;
            List<string> commands = new();
            List<string> lines = new();
            var pageNumber = 1;
            var currentLines = 0;
            const int maxVisibleLines = 16;

            if (Plugin.ConfigSimpleCommandLogging.Value)
            {
                Plugin.Log.LogDebug("Patching Terminal Awake");
            }

            if (__instance.terminalNodes.specialNodes[13] != null)
            {
                if (Plugin.ConfigSimpleCommandLogging.Value)
                {
                    Plugin.Log.LogDebug("Editing SpecialNode 13");
                }
                
                var newnode = __instance.terminalNodes.specialNodes[13];
                newnode.displayText = newnode.displayText[..^23] + ">MODCOMMANDS [page] (short. mdc, modc, modcmds)\nTo see a list of all SimpleCommand.API commands." + newnode.displayText[^23..];
            }
            
            if (SimpleCommandList != null && SimpleCommandList.Count > 0)
            {
                foreach (var module in SimpleCommandList)
                {
                    if (module.HideFromCommandList == false)
                    {
                        var text = "";
                        text += "\n>" + module.DisplayName.ToUpper();
                        if (module.Arguments != null && module.HasDynamicInput)
                        {
                            foreach (var param in module.Arguments)
                            {
                                text += " [" + param.ToLower() + "]";
                            }
                        }
                        if (module.Abbreviations != null)
                        {
                            text += " (short. ";
                            foreach (var abbreviation in module.Abbreviations)
                            {
                                text += abbreviation.ToLower() + ", ";
                            }
                            text = text.Substring(0, text.Length - 2) + ")";
                        }
                        text += "\n";

                        if (module.Description != null)
                        {
                            text += module.Description + "\n";
                        }
                        commands.Add(text);
                    }
                    if (module.ChildrenModules != null)
                    {
                        foreach (var childModule in module.ChildrenModules)
                        {
                            AddChildrenToList(childModule, module.DisplayName.ToUpper(), commands);
                        }
                    }
                }

                if (Plugin.ConfigSimpleCommandSortCommands.Value == true)
                {
                    commands.Sort();
                }

                if (Plugin.ConfigSimpleCommandLogging.Value)
                {
                    Plugin.Log.LogDebug("Creating SimpleCommandDictionary");
                }
                // On every game start, the dictionary gets cleared
                // Prevents double insertion and makes adding plugins later possible 
                SimpleCommandDictionary = new SortedDictionary<int, List<string>>();

                foreach (var command in commands)
                {
                    var lineCount = Mathf.CeilToInt(inputField.textComponent.GetTextInfo(command).lineCount);

                    if (currentLines + lineCount <= maxVisibleLines)
                    {
                        lines.Add(command);
                        currentLines += lineCount;
                    }
                    else
                    {
                        var distance = maxVisibleLines - currentLines;
                        if (distance > 0)
                        {
                            string fillLines = new('\n', distance);
                            lines.Add(fillLines);
                        }

                        SimpleCommandDictionary.TryAdd(pageNumber, lines);
                        lines = new()
                        {
                            command
                        };
                        currentLines = lineCount;
                        pageNumber++;
                    }
                }

                if (lines.Count > 0)
                {
                    var distance = maxVisibleLines - currentLines;
                    if (distance > 0)
                    {
                        string fillLines = new('\n', distance);
                        lines.Add(fillLines);
                    }
                    SimpleCommandDictionary.TryAdd(pageNumber, lines);
                }
                else if (lines.Count == 0)
                {
                    var error = "\n [ERROR] No commands added to list..\n";
                    string fillLines = new('\n', maxVisibleLines-2);
                    lines.Add(error);
                    lines.Add(fillLines);
                    SimpleCommandDictionary.TryAdd(pageNumber, lines);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnSubmit")]
        private static bool PatchSubmit(ref Terminal __instance)
        {
            if (__instance.terminalInUse && __instance.textAdded != 0)
            {
                if (SimpleCommandList != null && SimpleCommandList.Count > 0)
                {
                    var screenTextString = RemovePunctuation(__instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded));
                    var screenTextArray = screenTextString.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);

                    foreach (var module in SimpleCommandList)
                    {
                        if (screenTextString.Replace(" ", "") == "")
                        {
                            break;
                        }

                        if (module.DisplayName != null && screenTextArray[0].Replace(" ", "").Equals(module.DisplayName.ToLower()))
                        {

                            if (module.IgnoreModule == true)
                            {
                                return true;
                            }

                            if (__instance.terminalNodes.allKeywords.Any(keyword => keyword.word.Equals(screenTextArray[0].Replace(" ", ""))))
                            {
                                Plugin.Log.LogWarning(screenTextArray[0].Replace(" ", "") + " already exists as a default terminal keyword. This might cause issues.");
                            }

                            if (Plugin.ConfigSimpleCommandLogging.Value)
                            {
                                Plugin.Log.LogDebug("Player submitted sentence: " + screenTextString);
                            }

                            if (screenTextArray.Length > 1)
                            {
                                if (module.HasDynamicInput)
                                {
                                    if (module.Method != null)
                                    {
                                        __instance.LoadNewNode(module.Method(__instance));
                                        if (Plugin.ConfigSimpleCommandLogging.Value)
                                        {
                                            Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                                        }
                                        LoadNecessarySubmitActions(__instance);
                                        return false;
                                    }
                                    else
                                    {
                                        Plugin.Log.LogDebug("No Method found for module " + module.DisplayName);
                                    }
                                }
                                else if (module.ChildrenModules != null)
                                {
                                    foreach (var childModule in module.ChildrenModules)
                                    {
                                        if (IterateThroughChildren(childModule, __instance, screenTextArray, 1))
                                        {
                                            LoadNecessarySubmitActions(__instance);
                                            return false;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (module.Method != null)
                                {
                                    __instance.LoadNewNode(module.Method(__instance));
                                    if (Plugin.ConfigSimpleCommandLogging.Value)
                                    {
                                        Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                                    }
                                    LoadNecessarySubmitActions(__instance);
                                    return false;
                                }
                                else
                                {
                                    Plugin.Log.LogDebug("No Method found for module " + module.DisplayName);
                                }
                            }
                        }
                        else if (module.Abbreviations != null)
                        {
                            foreach (var abbreviation in module.Abbreviations)
                            {
                                if (screenTextArray[0].Replace(" ", "").Equals(abbreviation))
                                {
                                    if (Plugin.ConfigSimpleCommandLogging.Value)
                                    {
                                        Plugin.Log.LogDebug("Player submitted sentence: " + screenTextString);
                                    }

                                    if (screenTextArray.Length > 1)
                                    {
                                        if (module.HasDynamicInput)
                                        {
                                            if (module.Method != null)
                                            {
                                                __instance.LoadNewNode(module.Method(__instance));
                                                if (Plugin.ConfigSimpleCommandLogging.Value)
                                                {
                                                    Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                                                }
                                                LoadNecessarySubmitActions(__instance);
                                                return false;
                                            }
                                            else
                                            {
                                                Plugin.Log.LogDebug("No Method found for module " + module.DisplayName);
                                            }
                                        }
                                        else if (module.ChildrenModules != null)
                                        {
                                            foreach (var childModule in module.ChildrenModules)
                                            {
                                                if (IterateThroughChildren(childModule, __instance, screenTextArray, 1))
                                                {
                                                    LoadNecessarySubmitActions(__instance);
                                                    return false;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (module.Method != null)
                                        {
                                            __instance.LoadNewNode(module.Method(__instance));
                                            if (Plugin.ConfigSimpleCommandLogging.Value)
                                            {
                                                Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                                            }
                                            LoadNecessarySubmitActions(__instance);
                                            return false;
                                        } 
                                        else
                                        {
                                            Plugin.Log.LogDebug("No Method found for module " + module.DisplayName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private static bool IterateThroughChildren(SimpleCommandModule module, Terminal instance, string[] screenTextArray, int count)
        {
            if (module.IgnoreModule == true) 
            {
                return false;
            }

            if (screenTextArray[count].Replace(" ", "").Equals(module.DisplayName))
            {
                // Execute the current module
                if (module.HasDynamicInput && module.Method != null)
                {
                    instance.LoadNewNode(module.Method(instance));

                    if (Plugin.ConfigSimpleCommandLogging.Value)
                    {
                        Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                    }
                    return true;
                }
                // If the module has children, recursively execute them
                else if (module.ChildrenModules != null)
                {
                    foreach (var childModule in module.ChildrenModules)
                    {
                      IterateThroughChildren(childModule, instance, screenTextArray, count+1);
                    }
                }
                else if (module.Method != null)
                {
                    instance.LoadNewNode(module.Method(instance));
                    if (Plugin.ConfigSimpleCommandLogging.Value)
                    {
                        Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                    }
                    return true;
                }
            }
            else if (module.Abbreviations != null)
            {
                foreach (var abbreviation in module.Abbreviations)
                {
                    if (screenTextArray[count].Replace(" ", "").Equals(abbreviation))
                    {
                        // Execute the current module
                        if (module.HasDynamicInput && module.Method != null)
                        {
                            instance.LoadNewNode(module.Method(instance));

                            if (Plugin.ConfigSimpleCommandLogging.Value)
                            {
                                Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                            }
                            return true;
                        }
                        // If the module has children, recursively execute them
                        else if (module.ChildrenModules != null)
                        {
                            foreach (var childModule in module.ChildrenModules)
                            {
                                IterateThroughChildren(childModule, instance, screenTextArray, count + 1);
                            }
                        }
                        else if (module.Method != null)
                        {
                            instance.LoadNewNode(module.Method(instance));
                            if (Plugin.ConfigSimpleCommandLogging.Value)
                            {
                                Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                            }
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void AddChildrenToList(SimpleCommandModule module, string nestedString, List<string> commands)
        {
            var nestedText = nestedString + " " + module.DisplayName.ToUpper();
            if (module.HideFromCommandList == false)
            {
                var text = "\n>" + nestedText;
                if (module.Arguments != null && module.HasDynamicInput)
                {
                    foreach (var arguments in module.Arguments)
                    {
                        text += " [" + arguments.ToLower() + "]";
                    }
                }
                if (module.Abbreviations != null)
                {
                    text += " (short. ";
                    foreach (var abbreviation in module.Abbreviations)
                    {
                        text += abbreviation.ToLower() + ", ";
                    }
                    text = text[..^2] + ")";
                }
                text += "\n";

                if (module.Description != null)
                {
                    text += module.Description + "\n";
                }
                commands.Add(text);
            }
            if (module.ChildrenModules != null)
            {
                foreach (var childModule in module.ChildrenModules)
                {
                    AddChildrenToList(childModule, nestedText, commands);
                }
            }
        }

        private static void LoadNecessarySubmitActions(Terminal instance)
        {
            if (Plugin.ConfigSimpleCommandLogging.Value)
            {
                Plugin.Log.LogDebug("Loading necessary actions after Method submit.");
            }
            instance.screenText.text = instance.screenText.text.Substring(0, instance.screenText.text.Length - instance.textAdded);
            instance.currentText = instance.screenText.text;
            instance.textAdded = 0;
            instance.screenText.ActivateInputField();
            instance.screenText.Select();
        }
    }
}
