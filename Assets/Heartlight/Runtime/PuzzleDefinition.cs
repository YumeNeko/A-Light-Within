using UnityEngine;
namespace Heartlight
{
    public enum Virtue { Insight, Resonance, Courage }
    public enum PuzzleKind { Channel, FalseDoor, Trail, Animal, Align, Bridge, Growth, BossReveal, BossLink, BossCore, Safety }
    [CreateAssetMenu(menuName="心灯/机关规则")]
    public sealed class PuzzleDefinition : ScriptableObject
    {
        public string id, title;
        [TextArea] public string clue, hint;
        public Virtue ability;
        public PuzzleKind kind;
        public string[] prerequisites=new string[0];
        [Min(1f)] public float radius=2.1f;
        [Range(0,3)] public int targetRotation=1, initialRotation;
        public int milestone;
    }
}
