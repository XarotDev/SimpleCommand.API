using HarmonyLib;
using SimpleCommand.API.Classes;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
            TMP_InputField inputField = __instance.screenText;
            List<string> commands = new();
            List<string> lines = new();
            int pageNumber = 1;
            int currentLines = 0;
            int maxVisibleLines = 16;

            if (Plugin.configSimpleCommandLogging.Value)
            {
                Plugin.Log.LogDebug("Patching Terminal Awake");
            }
            
            if (SimpleCommandList != null && SimpleCommandList.Count > 0)
            {
                foreach (SimpleCommandModule module in SimpleCommandList)
                {
                    if (module.HideFromCommandList == false)
                    {
                        string text = "";
                        text += "\n>" + module.DisplayName.ToUpper();
                        if (module.Arguments != null && module.HasDynamicInput)
                        {
                            foreach (string param in module.Arguments)
                            {
                                text += " [" + param.ToLower() + "]";
                            }
                        }
                        if (module.Abbreviations != null)
                        {
                            text += " (short. ";
                            foreach (string abbreviation in module.Abbreviations)
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
                        foreach (SimpleCommandModule childModule in module.ChildrenModules)
                        {
                            AddChildrenToList(childModule, module.DisplayName.ToUpper(), commands);
                        }
                    }
                }

                if (Plugin.configSimpleCommandSortCommands.Value == true)
                {
                    commands.Sort();
                }

                foreach (string command in commands)
                {
                    int lineCount = Mathf.CeilToInt(inputField.textComponent.GetTextInfo(command).lineCount);

                    if (currentLines + lineCount <= maxVisibleLines)
                    {
                        lines.Add(command);
                        currentLines += lineCount;
                    }
                    else
                    {
                        int distance = maxVisibleLines - currentLines;
                        if (distance > 0)
                        {
                            string fillLines = new('\n', distance);
                            lines.Add(fillLines);
                        }

                        SimpleCommandDictionary.Add(pageNumber, lines);
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
                    int distance = maxVisibleLines - currentLines;
                    if (distance > 0)
                    {
                        string fillLines = new('\n', distance);
                        lines.Add(fillLines);
                    }
                    SimpleCommandDictionary.Add(pageNumber, lines);
                }
                else if (lines.Count == 0)
                {
                    string error = "\n [ERROR] No commands added to list..\n";
                    string fillLines = new('\n', maxVisibleLines-2);
                    lines.Add(error);
                    lines.Add(fillLines);
                    SimpleCommandDictionary.Add(pageNumber, lines);
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
                    string screenTextString = RemovePunctuation(__instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded));
                    string[] screenTextArray = screenTextString.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);

                    foreach (SimpleCommandModule module in SimpleCommandList)
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

                            if (Plugin.configSimpleCommandLogging.Value)
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
                                        if (Plugin.configSimpleCommandLogging.Value)
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
                                    foreach (SimpleCommandModule childModule in module.ChildrenModules)
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
                                    if (Plugin.configSimpleCommandLogging.Value)
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
                            foreach (string abbreviation in module.Abbreviations)
                            {
                                if (screenTextArray[0].Replace(" ", "").Equals(abbreviation))
                                {
                                    if (Plugin.configSimpleCommandLogging.Value)
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
                                                if (Plugin.configSimpleCommandLogging.Value)
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
                                            foreach (SimpleCommandModule childModule in module.ChildrenModules)
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
                                            if (Plugin.configSimpleCommandLogging.Value)
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

        [HarmonyPostfix]
        [HarmonyPatch("ParsePlayerSentence")]
        private static void PatchHelpNode(ref Terminal __instance, ref TerminalNode __result)
        {
            string screenTextString = RemovePunctuation(__instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded));
            if (screenTextString == "help")
            {
                TerminalNode newnode = ScriptableObject.CreateInstance<TerminalNode>();
                newnode.displayText = __result.displayText[..^23] + ">MODCOMMANDS [page] (short. mdc, modc, modcmds)\nTo see a list of all SimpleCommand.API commands." + __result.displayText[^23..];
                newnode.terminalEvent = "";
                newnode.clearPreviousText = true;

                __result = newnode;
                return;
            }
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

                    if (Plugin.configSimpleCommandLogging.Value)
                    {
                        Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                    }
                    return true;
                }
                // If the module has children, recursively execute them
                else if (module.ChildrenModules != null)
                {
                    foreach (SimpleCommandModule childModule in module.ChildrenModules)
                    {
                      IterateThroughChildren(childModule, instance, screenTextArray, count+1);
                    }
                }
                else if (module.Method != null)
                {
                    instance.LoadNewNode(module.Method(instance));
                    if (Plugin.configSimpleCommandLogging.Value)
                    {
                        Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                    }
                    return true;
                }
            }
            else if (module.Abbreviations != null)
            {
                foreach (string abbreviation in module.Abbreviations)
                {
                    if (screenTextArray[count].Replace(" ", "").Equals(abbreviation))
                    {
                        // Execute the current module
                        if (module.HasDynamicInput && module.Method != null)
                        {
                            instance.LoadNewNode(module.Method(instance));

                            if (Plugin.configSimpleCommandLogging.Value)
                            {
                                Plugin.Log.LogDebug("Method from module " + module.DisplayName + " successfully invoked.");
                            }
                            return true;
                        }
                        // If the module has children, recursively execute them
                        else if (module.ChildrenModules != null)
                        {
                            foreach (SimpleCommandModule childModule in module.ChildrenModules)
                            {
                                IterateThroughChildren(childModule, instance, screenTextArray, count + 1);
                            }
                        }
                        else if (module.Method != null)
                        {
                            instance.LoadNewNode(module.Method(instance));
                            if (Plugin.configSimpleCommandLogging.Value)
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
            string nestedText = nestedString + " " + module.DisplayName.ToUpper();
            if (module.HideFromCommandList == false)
            {
                string text = "\n>" + nestedText;
                if (module.Arguments != null && module.HasDynamicInput)
                {
                    foreach (string Arguments in module.Arguments)
                    {
                        text += " [" + Arguments.ToLower() + "]";
                    }
                }
                if (module.Abbreviations != null)
                {
                    text += " (short. ";
                    foreach (string abbreviation in module.Abbreviations)
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
                foreach (SimpleCommandModule childModule in module.ChildrenModules)
                {
                    AddChildrenToList(childModule, nestedText, commands);
                }
            }
        }

        private static void LoadNecessarySubmitActions(Terminal instance)
        {
            if (Plugin.configSimpleCommandLogging.Value)
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
