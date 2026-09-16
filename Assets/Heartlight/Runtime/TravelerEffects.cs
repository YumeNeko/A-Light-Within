using UnityEngine;

namespace Heartlight
{
    /// <summary>Visible ability feedback without adding solid obstacles.</summary>
    public sealed class TravelerEffects : MonoBehaviour
    {
        public Material effectMaterial;
        private HeartlightDirector game;
        private LineRenderer pulse;
        private float pulseStarted=-10;
        private Color pulseColor;

        private void Awake()
        {
            game=GetComponent<HeartlightDirector>();
            pulse=CreateLine("Ability_Pulse",49,.045f);
        }

        private LineRenderer CreateLine(string name,int count,float width)
        {
            var child=new GameObject(name);child.transform.SetParent(transform);
            var line=child.AddComponent<LineRenderer>();line.useWorldSpace=true;
            line.sharedMaterial=effectMaterial;line.widthMultiplier=width;
            line.positionCount=count;line.numCapVertices=3;line.enabled=false;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows=false;return line;
        }

        public void PlayPulse(Color color){pulseStarted=Time.time;pulseColor=color;}

        private void LateUpdate()
        {
            if(game==null)return;
            float elapsed=Time.time-pulseStarted;
            bool activePulse=elapsed<.85f;
            bool courage=game.Playing&&game.CourageRemaining>0;
            pulse.enabled=game.Playing&&(activePulse||courage);
            if(!pulse.enabled)return;
            float radius=activePulse?Mathf.Lerp(.2f,2.3f,elapsed/.85f):.7f+Mathf.Sin(Time.time*3)*.035f;
            pulse.widthMultiplier=activePulse?Mathf.Lerp(.065f,.006f,elapsed/.85f):.025f;
            pulse.startColor=pulse.endColor=activePulse?pulseColor:HeartlightDirector.AbilityColor(Virtue.Courage);
            for(int i=0;i<pulse.positionCount;i++)
            {
                float angle=i*Mathf.PI*2/(pulse.positionCount-1);
                pulse.SetPosition(i,transform.position+new Vector3(Mathf.Cos(angle)*radius,.14f,Mathf.Sin(angle)*radius));
            }
        }
    }
}
