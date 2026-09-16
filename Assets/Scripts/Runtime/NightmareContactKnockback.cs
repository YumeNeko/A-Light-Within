using System.Collections;
using UnityEngine;

namespace FragmentsOfHer.Assignment3
{
    public class NightmareContactKnockback : MonoBehaviour
    {
        public float horizontalForce=7f;
        public float verticalForce=1.8f;
        public float duration=.3f;
        public float cooldown=.9f;
        private float nextAllowedTime;
        private Coroutine activeKnockback;

        private void OnTriggerEnter(Collider other)
        {
            Transform target=FindPlayerRoot(other.transform);
            if(target!=null)TryKnockback(target);
        }

        public void TriggerForAutomatedTest(Transform player)
        {
            if(player!=null)TryKnockback(player);
        }

        private static Transform FindPlayerRoot(Transform candidate)
        {
            if(candidate==null)return null;
            Transform root=candidate.root;
            return root.CompareTag("Player")?root:null;
        }

        private void TryKnockback(Transform player)
        {
            if(Time.unscaledTime<nextAllowedTime)return;
            nextAllowedTime=Time.unscaledTime+cooldown;
            if(FinalAudioDirector.Instance!=null)FinalAudioDirector.Instance.PlayImpact();
            Vector3 impactDirection=player.position-transform.position;impactDirection.y=0f;if(impactDirection.sqrMagnitude<.01f)impactDirection=-transform.forward;impactDirection.Normalize();
            CharacterController impactController=player.GetComponent<CharacterController>();if(impactController!=null&&impactController.enabled)impactController.Move(impactDirection*.45f+Vector3.up*.08f);
            if(activeKnockback!=null)StopCoroutine(activeKnockback);
            activeKnockback=StartCoroutine(ApplyKnockback(player));
        }

        private IEnumerator ApplyKnockback(Transform player)
        {
            Vector3 away=player.position-transform.position;
            away.y=0f;
            if(away.sqrMagnitude<.01f)away=-transform.forward;
            away.Normalize();

            CharacterController controller=player.GetComponent<CharacterController>();
            Light aura=player.GetComponentInChildren<Light>(true);
            Color previousColor=aura!=null?aura.color:Color.white;
            float previousIntensity=aura!=null?aura.intensity:0f;
            if(aura!=null){aura.color=new Color(1f,.25f,.45f);aura.intensity=previousIntensity+1.8f;}
            Heartlight.HeartlightDirector.Instance?.Notify("梦魇将你推开了！展开勇气之光，继续向出口前进。",2.5f);

            float elapsed=0f;
            while(elapsed<duration&&player!=null)
            {
                float delta=Time.deltaTime;
                if(delta<=0f){yield return null;continue;}
                float strength=1f-Mathf.Clamp01(elapsed/duration);
                Vector3 velocity=away*(horizontalForce*strength)+Vector3.up*(verticalForce*strength);
                if(controller!=null&&controller.enabled)controller.Move(velocity*delta);
                else player.position+=velocity*delta;
                elapsed+=delta;
                yield return null;
            }

            if(aura!=null){aura.color=previousColor;aura.intensity=previousIntensity;}
            activeKnockback=null;
        }
    }
}
