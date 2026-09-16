using UnityEngine;
namespace Heartlight
{
    public sealed class HeartlightHazard : MonoBehaviour
    {
        private float lastHit=-10;
        private void OnCollisionEnter(Collision collision){Hit(collision.gameObject.GetComponentInParent<HeartlightDirector>());}
        public void Hit(HeartlightDirector game){if(game==null||!game.Playing||Time.time-lastHit<1)return;lastHit=Time.time;game.Respawn(true);}
    }
}
