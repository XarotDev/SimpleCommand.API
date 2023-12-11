using System;

namespace SimpleCommand.API.Classes
{
    public class SimpleCommandModule
    {
        public string displayName { get; set; }
        public string description { get; set; }
        public string[] abbreviations { get; set; }
        public string[] parameter { get; set; }
        public bool bHasDynamicInput { get; set; }
        public bool bHideFromCommandList { get; set; }
        public bool bSkipModuleOnSubmit { get; set; }
        public Func<Terminal, TerminalNode> method { get; set; }
        public SimpleCommandModule[] childrenModules { get; set; }

    }

}
