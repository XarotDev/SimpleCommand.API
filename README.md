# SimpleCommand.API [![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](https://choosealicense.com/licenses/mit/)
## Introduction

To create new commands for the Terminal, this plugin uses a nested structure of a class called **SimpleCommandModule**. This allows the user to create complex command structures with ease. Every command will get added to the **SimpleCommandList**, if not specified otherwise.

> [!IMPORTANT]
> This documentation does not explain how to create a plugin in general. Please look up how to create a plugin first.

## Getting Started
Start by downloading this plugin and referencing it inside of your IDE of choice. For Visual Studio 2022, you can check out this [documentation](https://docs.bepinex.dev/v5.4.11/articles/dev_guide/plugin_tutorial/1_setup.html).

## Reference as BepInDependency
For the plugin to work best, add it as BepInDependency above your initial plugin class.
```csharp
[BepInDependency(SimpleCommand.API.MyPluginInfo.PLUGIN_GUID)]
public class MyPlugin : BaseUnityPlugin
```
## SimpleCommandModule
The SimpleCommandModule class has the following properties:
```csharp
SimpleCommandModule reviveModule = new SimpleCommandModule()
{
```
### DisplayName (string)
What the player needs to type for the command to trigger, e.g. "revive"
```csharp
DisplayName: "revive",
```
### Method (Func<Terminal, TerminalNode>)
This method will get called if the player enters the command.
```csharp
Method: ReviveDeadPlayer,
```
> [!WARNING]
> Your initial method needs to have Terminal as a parameter and a TerminalNode as a return. The TerminalNode will be your text on the terminal after your method was invoked. You can see how to create a TerminalNode [here](#terminalnode).
### Description (string) (optional)
This text will appear in the command list. Tell the player what this command does.
```csharp
Description: "Revive a dead player"
```
### Abbreviations (string[]) (optional)
Shorter form of the word, which also works. This value can also contain several abbreviations.
```csharp
Abbreviations: ["rev", "reviv"],
```
### Arguments (string[]) (optional)
If the command has dynamic input, you can list what values the player has to type (shows in list later).
```csharp
Arguments: ["number"],
```
### HasDynamicInput (bool) (optional)
If the command awaits dynamic input like numbers, otherwise it will check for the children names.
```csharp
HasDynamicInput: false,
```
### HideFromCommandList (bool) (optional)
If this command is allowed to appear on on the command list. Useful, if you have a command structure like "buy coffee", but you don't actually want to display "buy" as extra.
```csharp
HideFromCommandList: false,
```
### IgnoreModule (bool) (optional)
Useful for plugins that already have their own system and just want to appear on the command list.
```csharp
IgnoreModule: false,
```
### ChildrenModules (SimpleCommandModule[]) (optional)
SimpleCommandModules that come after the current module, e.g. revive "player"
```csharp
ChildrenModules: [newModule]
```
## All methods
### AddSimpleCommand(SimpleCommandModule)
Adds the SimpleCommandModule to the command list.
> [!IMPORTANT]
> Please pay attention to duplicates. If your command name already exists in other plugins, it will usually load first, but errors may still occur.
### AddSimpleCommands(SimpleCommandModule[])
Just an additional way of adding commands to the list via array.
### GetInputValue(Terminal terminal)
Returns the complete string of what was written and submitted on the terminal. Terminal reference needed.
### GetInputValue(Terminal terminal, int index)
Returns a string array of the last wordsa depending on what `index` will be. (Counts backwards) Terminal reference needed aswell.
### RemovePunctuation(string s)
Removes every punctuation character in the given string and converts it to lowercase.
## TerminalNode
TerminalNodes are a default class defined by the Lethal Company developer. It will look something like this:
```csharp
public class TerminalNode : ScriptableObject
{
    [TextArea(2, 20)]
    public string displayText;
    public string terminalEvent;
    [Space(5f)]
    public bool clearPreviousText;
    public int maxCharactersToType = 25;
    [Space(5f)]
    [Header("Purchasing items")]
    public int buyItemIndex = -1;
    public bool isConfirmationNode;
    public int buyRerouteToMoon = -1;
    public int displayPlanetInfo = -1;
    public bool lockedInDemo;
    [Space(3f)]
    public int shipUnlockableID = -1;
    public bool buyUnlockable;
    public bool returnFromStorage;
    [Space(3f)]
    public int itemCost;
    [Header("Bestiary / Logs")]
    public int creatureFileID = -1;
    public string creatureName;
    public int storyLogFileID = -1;
    [Space(5f)]
    public bool overrideOptions;
    public bool acceptAnything;
    public CompatibleNoun[] terminalOptions;
    [Header("Misc")]
    public AudioClip playClip;
    public int playSyncedClip = -1;
    public Texture displayTexture;
    public VideoClip displayVideo;
    public bool loadImageSlowly;
    public bool persistentImage;
}
```
The most important properties for just diplaying text in the terminal are ``displayText`` and ``clearPreviousText``.
For this purpose, the following code will work just fine.
```csharp
TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
node.clearPreviousText = true;
node.displayText = "Insert text here";

return node;
```
## Usage/Examples
```csharp
using SimpleCommand.API.Classes;
using static SimpleCommand.API.SimpleCommand;

[BepInDependency(SimpleCommand.API.MyPluginInfo.PLUGIN_GUID)]
public class MyPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Create nested command module
        SimpleCommandModule reviveModule = new()
        {
            DisplayName = "revive",
            Description = "To revive dead players.\nType revive for dead player list.",
            HasDynamicInput = true,
            Abbreviations = new string[] { "rev", "reviv" },
            Arguments = new string[] {"number"},
            Method = ReviveDeadPlayer,
        };

        // Register command to SimpleCommand.API
        AddSimpleCommand(reviveModule);
    }
}
```
```csharp
// Example method for demonstration purposes
public static TerminalNode ReviveDeadPlayer(Terminal __terminal)
{
    string input = SimpleCommand.API.SimpleCommand.GetInputValue(__terminal);
    string[] textArray = input.Split(" ", StringSplitOptions.None);
    TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();

    if (textArray.Length <= 1 || textArray.Last().Equals(""))
    {
        string text = "Revive dead player with: revive [number]\n";
        int playerCount = 0;

        foreach (PlayerControllerB player in playersManager.allPlayerScripts)
        {
            if (player.isPlayerControlled && player.isPlayerDead)
            {
                playerCount++;
                text += "\n" + playerCount + ": " + player.playerUsername;
            }
        }

        if (playerCount == 0)
        {
            text += "\n [ERROR] No dead players found..";
        }
        
        node.displayText = text + "\n\n\n";
        node.clearPreviousText = true;
    }
    else
    {
        string name = GetPlayer(textArray[1]);
        node.displayText = "Revived player: " + name + "\n\n\n";
        node.clearPreviousText = true;
    }

    return node;

}
```
## SimpleCommand Config
The config file will look something like this.
General settings will change the behaviour of this plugin.
For developer, extra debugging can be enabled. Every action will be logged inside the BepInEx Console from now on.

> [!WARNING]
> Debug logs inside the BepInEx Console are disabled by default. You'll have to add them to the log levels in the BepInEx.cfg manually.
```
[Developer]

## Enables logging of every SimpleCommand.API action.
# Setting type: Boolean
# Default value: false
EnableDebugLogging = false

[General]

## By default, commands are sorted by plugin. Set the value to 'true' if you want them to be sorted alphabetically.
# Setting type: Boolean
# Default value: false
SortCommandsAlphabetically = false
```
## Screenshots

![file1](https://github.com/XarotDev/SimpleCommand.API/assets/127869475/d7215a9a-1554-4369-880a-5a916c882308)

![file2](https://github.com/XarotDev/SimpleCommand.API/assets/127869475/98e80df3-f632-44ad-a26a-67855fc61245)