using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Heartlight
{
    /// <summary>Opt-in local regression run; never runs during a normal player session.</summary>
    public sealed partial class HeartlightVerification : MonoBehaviour
    {
        HeartlightDirector game;
        CharacterController controller;
        int assertions;
        IEnumerator Start()
        {
            if(!Environment.GetCommandLineArgs().Contains("-heartlightTest"))yield break;
            game=GetComponent<HeartlightDirector>();controller=GetComponent<CharacterController>();
            PrepareReports();
            game.AutomatedInput=true;
            GetComponent<StarterAssets.ThirdPersonController>().enabled=false;
            var bridge=GetComponent<FragmentsOfHer.Player.StarterAssetsLegacyInputBridge>();if(bridge!=null)bridge.enabled=false;
            Time.captureFramerate=60;yield return null;
            Check(game.ui.Mode==HeartlightUI.ScreenMode.Introduction,"intro visible");Check(Cursor.visible,"intro cursor visible");yield return Capture("01_title");game.ui.StartJourney();yield return null;
            yield return VerifyMovementAndAnimation();
            var boss=Node("boss_core");game.Teleport(boss.transform.position+Vector3.back*.9f);game.Attempt(boss);Check(!game.IsComplete("boss_core"),"boss cannot skip prerequisites");
            game.Teleport(new Vector3(0,.2f,-46));game.Attempt(Node("truth"));Check(!game.IsComplete("truth"),"remote interaction rejected");
            yield return Solve("truth",Virtue.Insight);
            yield return Capture("04_courtyard_truth");
            game.Teleport(Node("false_left").transform.position+Vector3.back*.9f);game.Attempt(Node("false_left"));Check(!game.IsComplete("false_left"),"false door rejected");
            yield return Solve("door",Virtue.Insight);yield return Solve("memory",Virtue.Insight);
            yield return WalkRoute(new[]{new Vector3(6.5f,.15f,-7.7f),new Vector3(5.1f,.15f,-5.3f),new Vector3(5,.15f,-4),new Vector3(5,.15f,3.3f),new Vector3(4.2f,.15f,5.2f),new Vector3(2,.15f,5.2f)});
            Check(game.IsComplete("memory_arrival"),"memory guide physically traversable");
            yield return Capture("05_courtyard_exit");
            yield return Solve("insight_growth",Virtue.Insight);Check(game.Unlocked==2,"resonance unlock");
            var fox=Node("fox");game.Teleport(fox.transform.position+Vector3.back*.9f);game.Select(Virtue.Insight);game.Attempt(fox);Check(!game.IsComplete("fox"),"wrong ability rejected");
            game.Select(Virtue.Resonance);game.ui.SetMode(HeartlightUI.ScreenMode.Paused);game.Attempt(fox);Check(!game.IsComplete("fox"),"paused interaction rejected");game.ui.SetMode(HeartlightUI.ScreenMode.Playing);
            yield return Solve("fox",Virtue.Resonance);yield return Solve("deer",Virtue.Resonance);
            yield return VerifyForestWalk();yield return Capture("06_forest_bridge_before");
            var align=Node("bridge_align");game.Teleport(align.transform.position+Vector3.back*.9f);game.Attempt(align);Check(!game.IsComplete("bridge_align"),"wrong alignment rejected");align.Rotate();yield return Solve("bridge_align",Virtue.Resonance);
            Check(!Physics.Raycast(new Vector3(0,2,30.2f),Vector3.down,2.7f,~0,QueryTriggerInteraction.Ignore),"bridge gap empty before activation");
            yield return Solve("bridge",Virtue.Resonance);yield return new WaitForSeconds(2.2f);
            yield return WalkRoute(new[]{new Vector3(0,.25f,28),new Vector3(0,.25f,30.2f),new Vector3(0,.25f,32.5f)});Check(transform.position.z>32,"activated bridge physically traversable");
            yield return Capture("07_bridge_after");
            yield return Solve("forest_growth",Virtue.Resonance);Check(game.Unlocked==3,"courage unlock");
            yield return new WaitForSeconds(2);
            yield return WalkRoute(new[]{new Vector3(0,.2f,34.8f),new Vector3(0,.2f,37.6f),new Vector3(0,.2f,40)});
            yield return VerifyRockfall();
            yield return UseSafety("safety_rocks");yield return Solve("light",Virtue.Courage);
            Check(game.CourageRemaining>0,"courage timer starts");
            game.Teleport(new Vector3(1.4f,.4f,56.2f));yield return null;Check(!game.IsComplete("crossing"),"standing on platform is not completion");
            yield return VerifyVoidPhysicalRoute();
            yield return UseSafety("safety_void");yield return null;Check(game.IsComplete("crossing"),"safety arrival completes crossing");
            yield return VerifyPursuitContact();
            yield return UseSafety("safety_shadow");yield return null;Check(game.IsComplete("pursuit"),"safety pursuit exit does not lock progress");
            game.Teleport(new Vector3(0,-6,86));yield return null;Check(game.Deaths>0&&transform.position.y>0,"void respawn works");
            yield return Solve("boss_reveal",Virtue.Insight);
            yield return Capture("11_sanctuary_reveal");
            var link=Node("boss_link");game.Teleport(link.transform.position+Vector3.back*.9f);game.Select(Virtue.Resonance);game.Attempt(link);Check(!game.IsComplete("boss_link"),"boss link needs alignment");for(int i=0;i<3;i++)link.Rotate();yield return Solve("boss_link",Virtue.Resonance);
            game.Teleport(boss.transform.position+Vector3.back*.9f);game.Select(Virtue.Insight);game.Attempt(boss);Check(!game.IsComplete("boss_core"),"boss core rejects wrong ability");yield return Solve("boss_core",Virtue.Courage);
            float finishTimeout=Time.realtimeSinceStartup+15;while(game.ui.Mode!=HeartlightUI.ScreenMode.Ending&&Time.realtimeSinceStartup<finishTimeout)yield return null;
            yield return null;Check(game.Milestones==11,"all eleven journey milestones");Check(game.ui.Mode==HeartlightUI.ScreenMode.Ending&&Cursor.visible,"ending menu and cursor");
            yield return Capture("12_ending");
            string path=Path.Combine(reportDirectory,"heartlight-test-results.tsv");File.WriteAllLines(path,new[]{"seconds\taction\tid\tdetail"}.Concat(game.SessionEvents));
            Debug.Log("HEARTLIGHT_RUNTIME_OK assertions="+assertions+" milestones="+game.Milestones+" safety="+game.SafetyUses);
            Application.Quit(0);
        }
        PuzzleNode Node(string id)=>game.nodes.First(n=>n.definition.id==id);
        IEnumerator VerifyRockfall()
        {
            var trap=FindFirstObjectByType<DreamRockfall>();Check(trap!=null&&trap.rocks.Length>0,"rockfall configured");
            int deaths=game.Deaths,activations=trap.ActivationCount;
            Vector3 rockPosition=trap.rocks[0].transform.position;
            game.Teleport(new Vector3(rockPosition.x,.15f,rockPosition.z));controller.Move(Vector3.down*.05f);
            float timer=0;while(game.Deaths==deaths&&timer<5){controller.Move(Vector3.down*.035f);timer+=Time.deltaTime;yield return null;}
            Check(trap.ActivationCount>activations,"walking into warning zone triggers rockfall");
            Check(game.Deaths==deaths+1,"physical falling rock contact respawns player");
            Check(transform.position.z<42,"rockfall returns to safe checkpoint");
            yield return new WaitForSeconds(6);
            activations=trap.ActivationCount;game.Teleport(new Vector3(rockPosition.x,.15f,rockPosition.z));controller.Move(Vector3.down*.05f);
            yield return new WaitForSeconds(.12f);Check(trap.ActivationCount==activations+1,"rockfall rearms after reset");
            game.Respawn(false);yield return new WaitForSeconds(6);
        }
        IEnumerator Solve(string id,Virtue ability)
        {
            PuzzleNode node=Node(id);game.Teleport(node.transform.position+Vector3.back*.85f);game.Select(ability);yield return null;
            game.Attempt(node);yield return null;
            Check(game.IsComplete(id),"complete "+id);
        }
        IEnumerator UseSafety(string id)
        {
            PuzzleNode node=Node(id);game.Teleport(node.transform.position+Vector3.back*.7f);int before=game.SafetyUses;yield return null;
            game.Attempt(node);yield return null;
            Check(game.SafetyUses==before+1,"use "+id);
        }
        IEnumerator WalkRoute(Vector3[] route)
        {
            game.Teleport(route[0]);yield return null;
            for(int i=1;i<route.Length;i++)
            {
                float timer=0;
                while(Vector2.Distance(new Vector2(transform.position.x,transform.position.z),new Vector2(route[i].x,route[i].z))>.12f&&timer<8)
                {Vector3 direction=route[i]-transform.position;direction.y=0;controller.Move(direction.normalized*3.4f/60+Vector3.down*.04f);timer+=1f/60;yield return null;}
                Check(timer<8&&transform.position.y>-.5f,"walk route segment "+i);
            }
        }
        void Check(bool ok,string label){if(!ok){Debug.LogError("HEARTLIGHT_RUNTIME_FAIL "+label+" at "+transform.position);Application.Quit(2);throw new Exception(label);}assertions++;Debug.Log("HEARTLIGHT_CHECK "+label);}
    }
}
