using Godot;
using System;

namespace SakugaEngine.Resources
{
    [GlobalClass] [Tool]
    public partial class ChangeSideAnimationEvent : AnimationEvent
    {
        [Export] public int Index;
    }
}
