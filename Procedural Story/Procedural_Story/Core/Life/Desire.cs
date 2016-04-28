using Microsoft.Xna.Framework;

namespace Procedural_Story.Core.Life {
    public enum DesireType {
        Eat,
        Drink,
        Shelter,
        Rest,
        Security
    }
    class Desire {
        public DesireType Type;
        public Vector3 Location;
        public object associatedObject;
        public bool Fulfilled;
        public float Intensity; // desire with most intensity will be acted on
        public float IntensityGain; // how much it increases over time
    }
}
