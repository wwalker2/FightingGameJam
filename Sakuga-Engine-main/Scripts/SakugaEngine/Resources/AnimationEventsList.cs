using Godot;

namespace SakugaEngine.Resources
{
    [GlobalClass] [Tool]
    public partial class AnimationEventsList : Resource
    {    
        [Export] public int Frame;
        [Export] public AnimationEvent[] Events;
    }
}
