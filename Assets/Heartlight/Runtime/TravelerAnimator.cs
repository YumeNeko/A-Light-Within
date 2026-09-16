using UnityEngine;
namespace Heartlight
{
    public sealed class TravelerAnimator : MonoBehaviour
    {
        public Animation animationPlayer;
        public AnimationClip idle,walk,run,jump,interact;
        public Transform model;
        private CharacterController controller;
        private string current;
        private float actionUntil;
        private void Awake(){controller=GetComponent<CharacterController>();}
        public void Gesture(){actionUntil=Time.time+.8f;}
        private void Update()
        {
            if(animationPlayer==null)return;
            float speed=controller==null?0:new Vector2(controller.velocity.x,controller.velocity.z).magnitude;
            AnimationClip target=idle;
            if(Time.time<actionUntil&&interact!=null)target=interact;
            else if(controller!=null&&!controller.isGrounded&&jump!=null)target=jump;
            else if(speed>4&&run!=null)target=run;
            else if(speed>.15f&&walk!=null)target=walk;
            if(target==null)return;
            if(animationPlayer[target.name]==null)animationPlayer.AddClip(target,target.name);
            var state=animationPlayer[target.name];
            state.wrapMode=target==jump?WrapMode.ClampForever:WrapMode.Loop;
            state.speed=target==run&&speed<4?.6f:1f;
            if(current==target.name)return;
            animationPlayer.CrossFade(target.name,.18f);current=target.name;
        }
    }
}
