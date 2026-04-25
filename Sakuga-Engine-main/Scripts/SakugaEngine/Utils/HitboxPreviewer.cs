using Godot;
using SakugaEngine.Resources;

namespace SakugaEngine.Utils
{
    [Tool] [GlobalClass]
    public partial class HitboxPreviewer : Node3D
    {
        [Export] private Sprite3D[] hitboxGraphics;
        [Export] private DataContainer Data;
        [Export] private FrameAnimator Animator;
        [Export] private FighterState State;
        [Export] private bool AutoRun;
        [Export(PropertyHint.Range, "0, 99999")] private int Frame;
        
        public override void _Process(double delta)
        {
            if (!Engine.IsEditorHint()) return;
            if (Animator == null || State == null)
            {
                for(int j = 0; j < hitboxGraphics.Length; j++)
                    hitboxGraphics[j].Visible = false;
                return;
            }

            if (AutoRun)
            {
                Frame++;
                if (Frame >= State.Duration) Frame = 0;
            }

            Animator.ViewAnimations(GetCurrentAnimationSettings(), Frame);

            var hitboxData = GetCurrentHitboxSettings();
            PreviewHitboxes(hitboxData);
            PreviewPushbox(hitboxData);
        }

        private void PreviewHitboxes(HitboxState previewData)
        {
            if (previewData == null || previewData.HitboxIndex < 0 || previewData.HitboxIndex >= Data.Hitboxes.Length) return;

            var Boxes = Data.Hitboxes[previewData.HitboxIndex];

            for(int j = 0; j < hitboxGraphics.Length; j++)
            {
                if (Boxes.Hitboxes == null || Boxes.Hitboxes.Length == 0 || 
                    j >= Boxes.Hitboxes.Length || Boxes.Hitboxes[j] == null)
                    {
                        hitboxGraphics[j].Hide();
                        continue;
                    }

                hitboxGraphics[j].Visible = Boxes.Hitboxes[j].Size != Vector2I.Zero;

                switch (Boxes.Hitboxes[j].HitboxType)
                {
                    case Global.HitboxType.HURTBOX:
                        hitboxGraphics[j].SortingOffset = 1;
                        hitboxGraphics[j].Modulate = new Color(0.0f, 1.0f, 0.0f);
                        break;
                    case Global.HitboxType.HITBOX:
                        hitboxGraphics[j].SortingOffset = 2;
                        hitboxGraphics[j].Modulate = new Color(1.0f, 0.0f, 0.0f);
                        break;
                    case Global.HitboxType.PROXIMITY_BLOCK:
                        hitboxGraphics[j].SortingOffset = 4;
                        hitboxGraphics[j].Modulate = new Color(1.0f, 0.0f, 1.0f);
                        break;
                    case Global.HitboxType.PROJECTILE:
                        hitboxGraphics[j].SortingOffset = 2;
                        hitboxGraphics[j].Modulate = new Color(1.0f, 0.64f, 0.0f);
                        break;
                    case Global.HitboxType.THROW:
                        hitboxGraphics[j].SortingOffset = 2;
                        hitboxGraphics[j].Modulate = new Color(0.0f, 0.0f, 1.0f);
                        break;
                    case Global.HitboxType.COUNTER:
                        hitboxGraphics[j].SortingOffset = 2;
                        hitboxGraphics[j].Modulate = new Color(0.5f, 0.5f, 0.5f);
                        break;
                    case Global.HitboxType.DEFLECT:
                        hitboxGraphics[j].SortingOffset = 2;
                        hitboxGraphics[j].Modulate = new Color(1.0f, 0.0f, 0.5f);
                        break;
                    /*case Global.HitboxType.PARRY:
                        hitboxGraphics[j].SortingOffset = 2;
                        hitboxGraphics[j].Modulate = new Color(0.5f, 0.5f, 0.5f);
                        break;*/
                }
                hitboxGraphics[j].GlobalPosition = Global.ToScaledVector3(Boxes.Hitboxes[j].Center);
                hitboxGraphics[j].Scale = Global.ToScaledVector3(Boxes.Hitboxes[j].Size, 1f);
            }
        }

        private void PreviewPushbox(HitboxState previewData)
        {
            int collisionViewer = hitboxGraphics.Length - 1;
            if (previewData == null || previewData.HitboxIndex < 0 || previewData.HitboxIndex >= Data.Hitboxes.Length)
            {
                hitboxGraphics[collisionViewer].Visible = false;
                return;
            }

            var Boxes = Data.Hitboxes[previewData.HitboxIndex];
            
            hitboxGraphics[collisionViewer].Visible = Boxes.PushboxSize != Vector2I.Zero;
            hitboxGraphics[collisionViewer].SortingOffset = 3;
            hitboxGraphics[collisionViewer].Modulate = new Color(1.0f, 1.0f, 0.0f);
            hitboxGraphics[collisionViewer].GlobalPosition = Global.ToScaledVector3(Boxes.PushboxCenter);
            hitboxGraphics[collisionViewer].Scale = Global.ToScaledVector3(Boxes.PushboxSize, 1f);
        }

        private AnimationSettings GetCurrentAnimationSettings()
        {
            if (State == null) return null;
            if (State.animationSettings == null || State.animationSettings.Length <= 0) return null;
            if (State.animationSettings.Length == 1)
                return State.animationSettings[0];

            int anim = 0;

            for (int i = 0; i < State.animationSettings.Length; i++)
            {
                if (State.animationSettings[i] == null) continue;
                int nextFrame = (i >= State.animationSettings.Length - 1 || State.animationSettings[i + 1] == null) ?
                                State.Duration - 1 :
                                State.animationSettings[i + 1].AtFrame - 1;
                
                if (Frame >= State.animationSettings[i].AtFrame && Frame <= nextFrame)
                    anim = i;
            }

            return State.animationSettings[anim];
        }

        private HitboxState GetCurrentHitboxSettings()
        {
            if (State == null) return null;
            if (State.hitboxStates == null || State.hitboxStates.Length <= 0) return null;
            if (State.hitboxStates.Length == 1)
                return State.hitboxStates[0];

            int anim = 0;

            for (int i = 0; i < State.hitboxStates.Length; i++)
            {
                int nextFrame = (i >= State.hitboxStates.Length - 1) ?
                                int.MaxValue :
                                State.hitboxStates[i + 1].Frame - 1;
                
                if (Frame >= State.hitboxStates[i].Frame && Frame <= nextFrame)
                    anim = i;
            }

            return State.hitboxStates[anim];
        }
    }
}
