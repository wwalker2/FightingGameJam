using Godot;
using System;

namespace SakugaEngine.Resources
{
    [GlobalClass] [Tool]
    public partial class FrameProperties : Resource
    {
        [Export] public int Frame;
        [Export] public Global.FrameProperties Properties;
    }
}
