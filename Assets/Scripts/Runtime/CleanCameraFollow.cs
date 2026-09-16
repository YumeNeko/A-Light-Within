using System.Collections.Generic;
using UnityEngine;

namespace FragmentsOfHer.Clean
{
    public class CleanCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 focusOffset=new Vector3(0f,1.05f,0f);
        public float cameraDistance=4f;
        public float cameraHeight=0f;
        public float followSmoothness=14f;
        public float rotationSmoothness=16f;
        public float mouseSensitivity=95f;
        public float minPitch=-6f;
        public float maxPitch=32f;
        public float collisionRadius=.36f;
        public float collisionPadding=.24f;
        public LayerMask collisionMask=~0;
        public LayerMask hiddenGameplayLayers=0;

        private readonly List<Renderer> temporarilyHidden=new List<Renderer>();
        private float yaw;
        private float pitch=4f;
        private bool initialized;

        public void SnapToTarget(){initialized=false;}

        private void Awake()
        {
            Camera camera=GetComponent<Camera>();
            if(camera!=null){camera.fieldOfView=66f;camera.nearClipPlane=.1f;camera.farClipPlane=220f;camera.cullingMask&=~hiddenGameplayLayers.value;}
            if(target!=null)yaw=target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            RestoreOccluders();
            if(target==null)return;
            yaw+=Input.GetAxis("Mouse X")*mouseSensitivity*Time.deltaTime;
            pitch-=Input.GetAxis("Mouse Y")*mouseSensitivity*Time.deltaTime;
            pitch=Mathf.Clamp(pitch,minPitch,maxPitch);
            Vector3 focus=target.position+focusOffset;
            Quaternion orbit=Quaternion.Euler(pitch,yaw,0f);
            Vector3 desired=focus+orbit*new Vector3(0f,cameraHeight,-cameraDistance);
            Vector3 safe=ResolveCollision(focus,desired);
            if(!initialized){transform.position=safe;initialized=true;}else transform.position=ResolveCollision(focus,Vector3.Lerp(transform.position,safe,1f-Mathf.Exp(-followSmoothness*Time.deltaTime)));
            Vector3 look=focus-transform.position;
            if(look.sqrMagnitude>.001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look.normalized),1f-Mathf.Exp(-rotationSmoothness*Time.deltaTime));
            HideBlockingCeilings(focus);
        }

        private Vector3 ResolveCollision(Vector3 focus,Vector3 desired)
        {
            Vector3 direction=desired-focus;float distance=direction.magnitude;if(distance<=.01f)return desired;direction.Normalize();
            float closest=distance;
            foreach(var hit in Physics.SphereCastAll(focus,collisionRadius,direction,distance,collisionMask,QueryTriggerInteraction.Ignore))
            {
                if(target!=null&&(hit.transform==target||hit.transform.IsChildOf(target)))continue;
                closest=Mathf.Min(closest,Mathf.Max(0,hit.distance-collisionPadding));
            }
            return focus+direction*closest;
        }

        private void HideBlockingCeilings(Vector3 focus)
        {
            foreach(Collider nearby in Physics.OverlapSphere(focus,5.5f,collisionMask,QueryTriggerInteraction.Ignore))
            {
                Renderer nearbyRenderer=nearby.GetComponent<Renderer>();if(nearbyRenderer==null||nearbyRenderer.transform.IsChildOf(target))continue;
                if(nearbyRenderer.gameObject.name.IndexOf("Ceiling",System.StringComparison.OrdinalIgnoreCase)<0)continue;
                if(!temporarilyHidden.Contains(nearbyRenderer)){nearbyRenderer.enabled=false;temporarilyHidden.Add(nearbyRenderer);}
            }
            Vector3 ray=focus-transform.position;float distance=ray.magnitude;if(distance<.01f)return;
            foreach(RaycastHit hit in Physics.RaycastAll(transform.position,ray.normalized,distance,collisionMask,QueryTriggerInteraction.Ignore))
            {
                Renderer renderer=hit.collider.GetComponent<Renderer>();
                if(renderer==null||renderer.transform.IsChildOf(target))continue;
                if(renderer.gameObject.name.IndexOf("Ceiling",System.StringComparison.OrdinalIgnoreCase)<0)continue;
                renderer.enabled=false;temporarilyHidden.Add(renderer);
            }
        }

        private void RestoreOccluders(){foreach(Renderer r in temporarilyHidden)if(r!=null)r.enabled=true;temporarilyHidden.Clear();}
        private void OnDisable(){RestoreOccluders();}
    }
}
