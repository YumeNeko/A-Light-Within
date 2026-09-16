using UnityEngine;
namespace Heartlight
{
    public sealed class TravelerHazards : MonoBehaviour
    {
        private void OnControllerColliderHit(ControllerColliderHit hit){var hazard=hit.collider.GetComponentInParent<HeartlightHazard>();if(hazard!=null)hazard.Hit(GetComponent<HeartlightDirector>());}
    }
}
