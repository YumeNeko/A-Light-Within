using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Heartlight
{
    public sealed partial class HeartlightVerification
    {
        string reportDirectory;
        StarterAssets.ThirdPersonController movement;
        StarterAssets.StarterAssetsInputs inputs;

        void PrepareReports()
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-heartlightOutput");
            reportDirectory=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Path.GetDirectoryName(Application.dataPath),"HeartlightQA");
            Directory.CreateDirectory(reportDirectory);
            movement=GetComponent<StarterAssets.ThirdPersonController>();inputs=GetComponent<StarterAssets.StarterAssetsInputs>();
        }

        IEnumerator Capture(string name)
        {
            // Actual player render, including UI. This is not an OS keyboard/mouse test.
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            var cam=Camera.main;var character=GetComponent<TravelerAnimator>();
            Debug.Log("HEARTLIGHT_VIEW "+name+" player="+transform.position+" camera="+cam.transform.position+" model="+character.model.position);
            foreach(var r in character.model.GetComponentsInChildren<Renderer>())Debug.Log("HEARTLIGHT_RENDERER "+r.name+" enabled="+r.enabled+" layer="+r.gameObject.layer+" bounds="+r.bounds+" viewport="+cam.WorldToViewportPoint(r.bounds.center));
            File.WriteAllBytes(Path.Combine(reportDirectory,name+".jpg"),texture.EncodeToJPG(82));Destroy(texture);
            Debug.Log("HEARTLIGHT_CAPTURE "+name);yield return null;
        }

        void Drive(Vector3 target,bool sprint)
        {
            Vector3 delta=target-transform.position;delta.y=0;
            Vector3 relative=Quaternion.Inverse(Quaternion.Euler(0,Camera.main.transform.eulerAngles.y,0))*delta.normalized;
            inputs.MoveInput(delta.magnitude<.13f?Vector2.zero:new Vector2(relative.x,relative.z));inputs.SprintInput(sprint);inputs.LookInput(Vector2.zero);
        }

        IEnumerator VerifyMovementAndAnimation()
        {
            movement.enabled=true;inputs.MoveInput(Vector2.zero);game.Teleport(new Vector3(0,.15f,-45));
            yield return new WaitForSeconds(.35f);
            var animator=GetComponent<TravelerAnimator>();var bones=animator.model.GetComponentsInChildren<Transform>();
            var before=new Quaternion[bones.Length];for(int i=0;i<bones.Length;i++)before[i]=bones[i].localRotation;
            float startZ=transform.position.z;
            for(int frame=0;frame<60;frame++){Drive(new Vector3(0,0,-39),false);yield return null;}
            Check(transform.position.z>startZ+2,"production movement controller advances player");
            bool animated=false;for(int i=0;i<bones.Length;i++)if(Quaternion.Angle(before[i],bones[i].localRotation)>2)animated=true;
            Check(animated,"imported animation changes actual model bones");
            yield return Capture("02_traveler_walking");inputs.MoveInput(Vector2.zero);
            yield return new WaitForSeconds(.3f);yield return Capture("03_traveler_idle");movement.enabled=false;
            var renderers=animator.model.GetComponentsInChildren<Renderer>();bool visible=false;
            foreach(var r in renderers){var p=Camera.main.WorldToViewportPoint(r.bounds.center);if(r.enabled&&(Camera.main.cullingMask&(1<<r.gameObject.layer))!=0&&p.z>.1f&&p.x>0&&p.x<1&&p.y>0&&p.y<1)visible=true;}
            Check(visible,"textured player projects inside gameplay camera");
            var skin=animator.model.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh,true);
            var vertices=mesh.vertices;Bounds actual=new Bounds(skin.transform.TransformPoint(vertices[0]),Vector3.zero);foreach(var v in vertices)actual.Encapsulate(skin.transform.TransformPoint(v));Destroy(mesh);
            Debug.Log("HEARTLIGHT_ACTUAL_PLAYER_GEOMETRY "+actual);
            Check(actual.size.y>.9f&&actual.size.y<1.85f,"actual animated skin has human scale");
        }

        IEnumerator VerifyForestWalk()
        {
            movement.enabled=true;game.Teleport(new Vector3(0,.2f,17));yield return new WaitForSeconds(.25f);
            foreach(var point in new[]{new Vector3(0,0,21),new Vector3(0,0,24.5f),new Vector3(0,0,27)})
            {
                float time=0;while(Vector2.Distance(new Vector2(transform.position.x,transform.position.z),new Vector2(point.x,point.z))>.18f&&time<8){Drive(point,false);time+=Time.deltaTime;yield return null;}
                Check(time<8&&transform.position.y>-.2f,"forest clearing walk with production controller");
            }
            inputs.MoveInput(Vector2.zero);movement.enabled=false;
        }

        IEnumerator JumpTo(Vector3 target,Transform movingTarget=null)
        {
            inputs.MoveInput(Vector2.zero);inputs.JumpInput(false);yield return new WaitForSeconds(.3f);
            float initialY=transform.position.y,maxY=initialY;int deaths=game.Deaths;
            inputs.JumpInput(true);Drive(target,true);yield return null;inputs.JumpInput(false);
            float elapsed=0;bool airborne=false;
            while(elapsed<2.5f)
            {
                if(movingTarget!=null)target=movingTarget.position;
                Drive(target,true);maxY=Mathf.Max(maxY,transform.position.y);
                if(transform.position.y>initialY+.15f)airborne=true;
                elapsed+=Time.deltaTime;yield return null;
                if(airborne&&controller.isGrounded&&elapsed>.25f)break;
            }
            inputs.MoveInput(Vector2.zero);inputs.SprintInput(false);
            Check(airborne&&maxY>initialY+.6f,"production jump reaches intended height");
            var surface=movingTarget!=null?movingTarget.GetComponent<Collider>():game.Obj("Courage_Post_Void_Landing_Floor").GetComponent<Collider>();
            Bounds landingBounds=surface.bounds;landingBounds.Expand(.3f);
            Check(game.Deaths==deaths&&controller.isGrounded&&landingBounds.Contains(transform.position),"production jump lands on requested surface");
            if(movingTarget!=null)
            {
                float centerTime=0;while(Vector2.Distance(new Vector2(transform.position.x,transform.position.z),new Vector2(movingTarget.position.x,movingTarget.position.z))>.22f&&centerTime<1.4f){Drive(movingTarget.position,false);centerTime+=Time.deltaTime;yield return null;}
                inputs.MoveInput(Vector2.zero);
            }
        }

        IEnumerator VerifyVoidPhysicalRoute()
        {
            movement.enabled=true;game.Teleport(new Vector3(2.8f,.8f,56.2f));inputs.MoveInput(Vector2.zero);
            int deaths=game.Deaths;float elapsed=0;while(game.Deaths==deaths&&elapsed<4){elapsed+=Time.deltaTime;yield return null;}
            Check(game.Deaths==deaths+1,"uncovered void has no supporting floor");
            game.Teleport(new Vector3(0,.2f,54));yield return new WaitForSeconds(.35f);
            yield return JumpTo(game.platforms[0].position,game.platforms[0]);
            Check(!game.IsComplete("crossing"),"platform jump alone does not award far shore");
            yield return new WaitForSeconds(.65f);
            yield return Capture("08_first_platform");
            Vector3 relative=transform.position-game.platforms[0].position;yield return new WaitForSeconds(game.platformPeriod);
            Check(game.Deaths==deaths+1&&(transform.position-game.platforms[0].position-relative).magnitude<.5f,"rider remains on moving platform for full cycle");
            yield return JumpTo(game.platforms[1].position,game.platforms[1]);
            yield return JumpTo(game.platforms[2].position,game.platforms[2]);
            yield return JumpTo(new Vector3(8,.1f,59.2f));yield return null;
            Check(game.IsComplete("crossing"),"normal platform jumping awards far shore");
            yield return Capture("09_far_shore");inputs.MoveInput(Vector2.zero);movement.enabled=false;
        }

        IEnumerator VerifyPursuitContact()
        {
            movement.enabled=true;game.Teleport(new Vector3(8,.15f,62.2f));inputs.MoveInput(Vector2.zero);yield return new WaitForSeconds(.25f);
            var shadow=game.Obj("Nightmare_Shadow_Pursuer");Check(shadow.activeInHierarchy,"pursuer activates on route");
            Vector3 position=transform.position;float elapsed=0;
            while((transform.position-position).sqrMagnitude<.08f&&elapsed<6){elapsed+=Time.deltaTime;yield return null;}
            Check((transform.position-position).sqrMagnitude>.08f,"actual pursuer contact displaces player");
            game.Select(Virtue.Courage);game.CastCourage();yield return Capture("10_courage_pursuit");
            inputs.MoveInput(Vector2.zero);movement.enabled=false;
        }
    }
}
