using System.Collections;
using UnityEngine;

namespace Heartlight
{
    public sealed class DreamRockfall : MonoBehaviour
    {
        public GameObject[] rocks;
        public float warningSeconds=.85f, resetSeconds=4.5f;
        private Vector3[] starts;
        private Quaternion[] rotations;
        private bool armed;
        public int ActivationCount {get;private set;}
        private void Awake()
        {
            starts=new Vector3[rocks.Length];rotations=new Quaternion[rocks.Length];
            for(int i=0;i<rocks.Length;i++){starts[i]=rocks[i].transform.position;rotations[i]=rocks[i].transform.rotation;rocks[i].SetActive(false);}
        }
        private void OnTriggerEnter(Collider other){if(other.GetComponentInParent<HeartlightDirector>()!=null)Activate();}
        public void Activate(){if(!armed)StartCoroutine(Drop());}
        IEnumerator Drop()
        {
            armed=true;ActivationCount++;HeartlightDirector.Instance?.Notify("上方传来碎裂声！落石即将坠下，离开警示带。",3);
            FragmentsOfHer.Assignment3.FinalAudioDirector.Instance?.PlayDanger();
            yield return new WaitForSeconds(warningSeconds);
            foreach(var rock in rocks){rock.SetActive(true);var body=rock.GetComponent<Rigidbody>();body.isKinematic=false;body.useGravity=true;body.linearVelocity=Vector3.down*2;}
            yield return new WaitForSeconds(resetSeconds);
            for(int i=0;i<rocks.Length;i++)
            {
                var body=rocks[i].GetComponent<Rigidbody>();body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;rocks[i].SetActive(false);rocks[i].transform.SetPositionAndRotation(starts[i],rotations[i]);
            }
            armed=false;
        }
    }
}
