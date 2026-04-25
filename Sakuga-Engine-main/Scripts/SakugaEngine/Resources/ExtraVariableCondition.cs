using Godot;
using System;

namespace SakugaEngine.Resources
{
    [GlobalClass] [Tool]
    public partial class ExtraVariableCondition : Resource
    {
        [Export] public uint Value;
        [Export] public Global.ExtraVariableMode Mode;
        [Export] public Global.ExtraVariableCompareMode CompareMode;
    }
}
