

using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Reflection/ScreenSpaceReflection(forward)")]
    public class QxSSR : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isActive = new BoolParameter(false, false);
        public FloatParameter MaxStep = new FloatParameter(10, false);
        public FloatParameter StepSize = new FloatParameter(1, false);
        public FloatParameter MaxDistance = new FloatParameter(10, false);
        public FloatParameter Thickness = new FloatParameter(1, false);

        public bool IsActive()
        {
            return isActive.value;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}