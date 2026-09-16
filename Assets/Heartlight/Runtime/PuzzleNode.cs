using UnityEngine;
namespace Heartlight
{
    public enum PuzzleState { Locked, Ready, Complete }
    public sealed class PuzzleNode : MonoBehaviour
    {
        public PuzzleDefinition definition;
        public Transform rotor;
        public Renderer indicator;
        public Vector3 destination;
        public PuzzleState State { get; private set; }
        public int Rotation { get; private set; }
        public bool Aligned => Rotation==definition.targetRotation;
        private float errorUntil;
        private MaterialPropertyBlock properties;
        private void Awake(){Rotation=definition.initialRotation;properties=new MaterialPropertyBlock();}
        public void Rotate(){Rotation=(Rotation+1)%4;HeartlightDirector.Instance?.Record("rotate",definition.id,Rotation.ToString());}
        public void Reject(){errorUntil=Time.time+.65f;}
        private void Update()
        {
            var game=HeartlightDirector.Instance;if(game==null||definition==null)return;
            bool done=game.IsComplete(definition.id),ready=game.ConditionsMet(definition);
            State=done?PuzzleState.Complete:ready?PuzzleState.Ready:PuzzleState.Locked;
            if(rotor!=null)rotor.localRotation=Quaternion.Slerp(rotor.localRotation,Quaternion.Euler(0,Rotation*90,0),10*Time.deltaTime);
            if(indicator==null)return;
            Color color=done?new Color(.55f,1,.82f):ready?HeartlightDirector.AbilityColor(definition.ability):new Color(.26f,.34f,.43f);
            if(Time.time<errorUntil)color=new Color(1,.27f,.3f);
            float pulse=done?.9f:.72f+Mathf.Sin(Time.time*2.8f)*.16f;
            properties.SetColor("_Color",color);properties.SetColor("_EmissionColor",color*pulse);indicator.SetPropertyBlock(properties);
        }
    }
}
