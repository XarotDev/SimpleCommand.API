using System;

namespace SimpleCommand.API.Classes
{
    public class SimpleCommandModule
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string[] Abbreviations { get; set; }
        public string[] Arguments { get; set; }
        public bool HasDynamicInput { get; set; }
        public bool HideFromCommandList { get; set; }
        public bool IgnoreModule { get; set; }
        public Func<Terminal, TerminalNode> Method { get; set; }
        public SimpleCommandModule[] ChildrenModules { get; set; }

    }

}
