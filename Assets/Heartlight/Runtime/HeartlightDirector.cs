using System;
using System.Collections.Generic;
using FragmentsOfHer.Assignment3;
using FragmentsOfHer.Clean;
using UnityEngine;

namespace Heartlight
{
    public sealed class HeartlightDirector : MonoBehaviour
    {
        public static HeartlightDirector Instance { get; private set; }
        public Font chineseFont;
        public PuzzleNode[] nodes;
        public float courageDuration=10f,platformPeriod=5.5f;
        public Transform[] platforms;
        public Transform checkpoint;
        public HeartlightUI ui;
        [NonSerialized] public bool AutomatedInput;
        public bool Playing=>ui!=null&&ui.Mode==HeartlightUI.ScreenMode.Playing;
        public Virtue Selected {get;private set;}
        public int Unlocked {get;private set;}=1;
        public int Milestones=>milestones.Count;
        public int Deaths {get;private set;}
        public int WrongChoices {get;private set;}
        public int SafetyUses {get;private set;}
        public float PlaySeconds {get;private set;}
        public float CourageRemaining=>Mathf.Max(0,courageUntil-Time.time);
        public PuzzleNode Nearest {get;private set;}
        public IReadOnlyList<string> SessionEvents=>events;
        public string Message {get;private set;}
        public float MessageUntil {get;private set;}
        private readonly HashSet<string> completed=new HashSet<string>();
        private readonly HashSet<int> milestones=new HashSet<int>();
        private readonly Dictionary<string,GameObject> objects=new Dictionary<string,GameObject>();
        private readonly List<string> events=new List<string>();
        private Vector3[] platformStarts;
        private CharacterController controller;
        private Vector3 lastSafe;
        private float courageUntil,castCooldown;
        private bool chasing,defeated;

        public static string AbilityName(Virtue v)=>v==Virtue.Insight?"洞察":v==Virtue.Resonance?"共鸣":"勇气之光";
        public static Color AbilityColor(Virtue v)=>v==Virtue.Insight?new Color(.36f,.77f,.94f):v==Virtue.Resonance?new Color(1,.73f,.35f):new Color(.77f,.66f,1);
        private void Awake()
        {
            Instance=this;
            controller=GetComponent<CharacterController>();
            foreach(Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(!objects.ContainsKey(t.name))objects.Add(t.name,t.gameObject);
            platformStarts=new Vector3[platforms.Length];for(int i=0;i<platforms.Length;i++)platformStarts[i]=platforms[i].position;
            lastSafe=transform.position;
        }
        private void Update()
        {
            if(!Playing)return;
            PlaySeconds+=Time.deltaTime;
            for(int i=0;i<3;i++)if(!AutomatedInput&&Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))Select((Virtue)i);
            UpdateNearest();
            if(!AutomatedInput&&Nearest!=null)
            {
                if(Input.GetKeyDown(KeyCode.E))Attempt(Nearest);
                if(Input.GetKeyDown(KeyCode.F)&&IsRotating(Nearest)&&ConditionsMet(Nearest.definition))Nearest.Rotate();
            }
            else if(!AutomatedInput&&Input.GetKeyDown(KeyCode.E)&&Selected==Virtue.Courage&&Unlocked>=3)CastCourage();
            if(Input.GetKeyDown(KeyCode.R))Respawn(false);
            if(transform.position.y< -5f)Respawn(true);
            UpdateProgress();AnimateEnvironment();
        }
        public void Select(Virtue ability)
        {
            if((int)ability>=Unlocked){Notify(ability==Virtue.Resonance?"共鸣尚未领悟：先走出回声庭院。":"勇气之光尚未领悟：先唤醒森林出口。",3);return;}
            Selected=ability;Record("ability","player",AbilityName(ability));
        }
        public bool IsComplete(string id)=>completed.Contains(id);
        public bool ConditionsMet(PuzzleDefinition d){foreach(string required in d.prerequisites)if(!completed.Contains(required))return false;return true;}
        public bool IsRotating(PuzzleNode n)=>n.definition.kind==PuzzleKind.Align||n.definition.kind==PuzzleKind.BossLink;
        public bool InRange(PuzzleNode node)=>Vector3.Distance(transform.position,node.transform.position)<=node.definition.radius;
        private bool ClearSight(PuzzleNode node)
        {
            Vector3 start=transform.position+Vector3.up*.8f,end=node.transform.position+Vector3.up*.65f;
            foreach(RaycastHit hit in Physics.RaycastAll(start,end-start,Vector3.Distance(start,end),~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(node.transform)&&hit.collider.bounds.max.y>.5f)return false;
            return true;
        }
        public void UpdateNearest()
        {
            Nearest=null;float best=float.MaxValue;
            foreach(PuzzleNode n in nodes){if(n==null||IsComplete(n.definition.id)||!n.gameObject.activeInHierarchy||!InRange(n)||!ClearSight(n))continue;float distance=Vector3.Distance(transform.position,n.transform.position);if(distance<best){best=distance;Nearest=n;}}
        }
        public bool Attempt(PuzzleNode node)
        {
            if(!Playing||node==null||IsComplete(node.definition.id)||!InRange(node)||!ClearSight(node))return false;
            var d=node.definition;
            if(!ConditionsMet(d)){Notify("这里仍在沉睡。先完成当前旅程目标。",3);return false;}
            if(d.kind==PuzzleKind.Safety)
            {
                Teleport(node.destination);SafetyUses++;Record("safety",d.id,"used");Notify("已抵达安全落点。你仍可以完成前方的谜题。",3);return true;
            }
            if((int)d.ability>=Unlocked||Selected!=d.ability)
            {
                WrongChoices++;node.Reject();Record("wrong_ability",d.id,AbilityName(Selected));Notify("机关没有回应："+d.clue,3);Sfx(false);return false;
            }
            if(d.kind==PuzzleKind.FalseDoor){node.Reject();WrongChoices++;Notify("门中的印记已经断裂。这只是幻象，寻找完整的眼纹。",3);Sfx(false);return false;}
            if(IsRotating(node)&&!node.Aligned){node.Reject();Notify("能量方向尚未对齐。按 F 旋转，将箭头对准地面上的金色印记。",3);return false;}
            CompleteNode(node);return true;
        }
        private void CompleteNode(PuzzleNode node)
        {
            var d=node.definition;completed.Add(d.id);if(d.milestone>0)milestones.Add(d.milestone);
            Record("complete",d.id,PlaySeconds.ToString("F1"));PlayAbilityFeedback(d.ability);Notify("「"+d.title+"」已回应。"+d.hint,4);
            switch(d.id)
            {
                case "truth":Active("Revealed_Truth_Glyphs",true);Active("Illusion_Veil",false);break;
                case "door":Active("Illusion_Door_Left",false);Active("Illusion_Door_Right",false);break;
                case "memory":Active("Memory_Echo_Trail",true);break;
                case "insight_growth":Unlocked=2;Selected=Virtue.Resonance;SetCheckpoint(new Vector3(0,.15f,10));Notify("你领悟了「共鸣」。按 2 选择，聆听森林的回应。",6);break;
                case "fox":Active("Spirit_Fox",true);break;
                case "deer":Active("Spirit_Deer",true);milestones.Add(5);break;
                case "bridge_align":Active("Bridge_Connection_Beam",true);break;
                case "bridge":SetCheckpoint(new Vector3(0,.15f,27));break;
                case "forest_growth":Unlocked=3;Selected=Virtue.Courage;SetCheckpoint(new Vector3(0,.15f,40));Notify("你领悟了「勇气之光」。按 3 选择，在空处按 E 释放持续光波。",6);break;
                case "light":CastCourage();SetCheckpoint(new Vector3(0,.15f,47));break;
                case "boss_reveal":Active("Boss_Runes",true);Active("Revealed_Nightmare_True_Form",true);Active("Boss_False_Left",false);Active("Boss_False_Right",false);break;
                case "boss_link":Active("Boss_Connection_Beam",true);break;
                case "boss_core":defeated=true;Active("Nightmare_Core_Final_Boss",false);Active("Revealed_Nightmare_True_Form",false);Active("Nightmare_Exit_Barrier",false);Active("Boss_Shield",false);Active("Final_Light_Wave",true);FinalAudioDirector.Instance?.PlayFinale();ui.FinishAfter(3);break;
            }
        }
        public void CastCourage()
        {
            if(Time.time<castCooldown||Unlocked<3)return;castCooldown=Time.time+.6f;courageUntil=Time.time+courageDuration;
            PlayAbilityFeedback(Virtue.Courage);Record("cast","courage",courageDuration.ToString());Notify("勇气之光已展开。光芒消散后，可以再次释放。",2.8f);
        }
        private void PlayAbilityFeedback(Virtue ability)
        {
            Sfx(true);
            GetComponent<TravelerAnimator>()?.Gesture();
            GetComponent<TravelerEffects>()?.PlayPulse(AbilityColor(ability));
        }
        private void UpdateProgress()
        {
            Vector3 p=transform.position;
            if(IsComplete("memory")&&!IsComplete("memory_arrival")&&p.z>3.8f&&p.x<3.1f){completed.Add("memory_arrival");milestones.Add(3);Sfx(true);Notify("你读懂了回声。前方的心灯正等待回应。",4);}
            if(IsComplete("light")&&!IsComplete("crossing")&&p.x>5.1f&&p.x<11&&p.z>=58.8f&&p.y>-.3f){completed.Add("crossing");milestones.Add(9);SetCheckpoint(new Vector3(8,.15f,59.5f));Active("Void_Crossing_Goal",false);Active("Void_Crossing_Completed",true);Record("complete","crossing","far_landing");Sfx(true);Notify("已穿越虚空，抵达远岸。前方有梦魇的影子。",4);}
            if(IsComplete("crossing")&&!IsComplete("pursuit")&&p.z>61&&p.z<72&&Mathf.Abs(p.x-8)<4&&!chasing){chasing=true;Active("Nightmare_Shadow_Pursuer",true);Obj("Nightmare_Shadow_Pursuer").transform.position=new Vector3(8,.15f,60);Notify("梦魇正在逼近！保持前进，勇气之光可以减缓它。",4);}
            if(IsComplete("crossing")&&!IsComplete("pursuit")&&p.z>=72&&p.y>-.3f){chasing=false;completed.Add("pursuit");milestones.Add(10);Active("Nightmare_Shadow_Pursuer",false);SetCheckpoint(new Vector3(8,.15f,74));Record("complete","pursuit","exit");Sfx(true);Notify("恐惧没有消失，但你已学会继续前行。前往心灯圣所。",5);}
            if(IsComplete("pursuit")&&p.z>84&&p.z<88&&lastSafe.z<84)SetCheckpoint(new Vector3(0,.15f,86));
            Active("Darkness_Blocker_1",!IsComplete("light"));Active("Courage_Timed_Shroud",CourageRemaining<=0&&!IsComplete("pursuit")&&Mathf.Abs(p.z-69)>.65f);
        }
        private void AnimateEnvironment()
        {
            if(IsComplete("door"))RotateObject("Truth_Door_Correct_Pivot",Quaternion.Euler(0,-105,0),100);
            if(IsComplete("fox"))Move("Spirit_Fox",new Vector3(-2.8f,.08f,26.5f),1.8f);
            if(IsComplete("deer"))Move("Spirit_Deer",new Vector3(2.8f,.08f,26.5f),1.5f);
            if(IsComplete("bridge")){Move("Rising_Root_Bridge",new Vector3(0,.08f,30.2f),1.35f);Move("Rising_Root_Bridge_Collider",new Vector3(0,.02f,30.2f),1.35f);}
            if(IsComplete("forest_growth")){Move("Kindness_Living_Gate_Left",new Vector3(-4.1f,.05f,36.2f),2);Move("Kindness_Living_Gate_Right",new Vector3(4.1f,.05f,36.2f),2);}
            for(int i=0;i<platforms.Length;i++)
            {
                float phase=i*2.1f,wave=Time.time*Mathf.PI*2/platformPeriod+phase;
                Vector3 before=platforms[i].position;platforms[i].position=platformStarts[i]+new Vector3(0,Mathf.Sin(wave*2+phase*.4f)*.1f,Mathf.Sin(wave)*(.72f+i*.09f));
                if(Physics.Raycast(transform.position+Vector3.up*.18f,Vector3.down,out RaycastHit hit,.65f,~0,QueryTriggerInteraction.Ignore)&&hit.transform==platforms[i])controller.Move(platforms[i].position-before);
            }
            if(chasing){GameObject shadow=Obj("Nightmare_Shadow_Pursuer");if(shadow!=null){Vector3 target=transform.position;target.y=.1f;shadow.transform.position=Vector3.MoveTowards(shadow.transform.position,target,(CourageRemaining>0?.8f:2.5f)*Time.deltaTime);Vector3 dir=target-shadow.transform.position;if(dir.sqrMagnitude>.01f)shadow.transform.rotation=Quaternion.LookRotation(dir);}}
            if(defeated){var light=Obj("Heartlight_Final_Sun");if(light!=null){Light l=light.GetComponent<Light>();l.intensity=Mathf.MoveTowards(l.intensity,3f,Time.deltaTime);}}
        }
        public void SetCheckpoint(Vector3 point){lastSafe=point;if(checkpoint!=null)checkpoint.position=point;if(PlayerRespawnManager.Instance!=null)PlayerRespawnManager.Instance.SetCheckpoint(checkpoint,"心灯检查点","梦境");}
        public void Respawn(bool death)
        {
            if(death)Deaths++;chasing=false;Active("Nightmare_Shadow_Pursuer",false);Teleport(lastSafe);Record(death?"death":"checkpoint","player",lastSafe.ToString());Notify("已返回最近的心灯。已解开的机关仍然保留。",3);
        }
        public void Teleport(Vector3 position)
        {
            controller.enabled=false;
            transform.position=position;
            controller.enabled=true;
            FindFirstObjectByType<CleanCameraFollow>()?.SnapToTarget();
            var movement=GetComponent<StarterAssets.ThirdPersonController>();
            if(movement!=null)movement.ResetVerticalVelocity();
        }
        public void Notify(string text,float seconds=3){Message=text;MessageUntil=Time.unscaledTime+seconds;}
        public void Record(string action,string id,string detail){events.Add(PlaySeconds.ToString("F2")+"\t"+action+"\t"+id+"\t"+detail);}
        public GameObject Obj(string name){objects.TryGetValue(name,out GameObject obj);return obj;}
        public void Active(string name,bool active){GameObject obj=Obj(name);if(obj!=null)obj.SetActive(active);}
        private void Move(string name,Vector3 target,float speed){GameObject obj=Obj(name);if(obj!=null)obj.transform.position=Vector3.MoveTowards(obj.transform.position,target,speed*Time.deltaTime);}
        private void RotateObject(string name,Quaternion target,float speed){GameObject obj=Obj(name);if(obj!=null)obj.transform.localRotation=Quaternion.RotateTowards(obj.transform.localRotation,target,speed*Time.deltaTime);}
        private void Sfx(bool success){if(FinalAudioDirector.Instance==null)return;if(success)FinalAudioDirector.Instance.PlayInteraction(1);else FinalAudioDirector.Instance.PlayImpact();}
        private void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
