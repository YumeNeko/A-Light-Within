using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Heartlight
{
    [DefaultExecutionOrder(200)]
    public sealed class HeartlightUI : MonoBehaviour
    {
        public enum ScreenMode { Introduction, Playing, Paused, Ending }
        public ScreenMode Mode {get;private set;}=ScreenMode.Introduction;
        public HeartlightDirector game;
        private StarterAssetsInputs inputs;
        private GUIStyle title,heading,body,small,button,centerTitle,centerHeading,centerBody,centerSmall;
        private float volume=.65f,sensitivity=95f,areaStarted=-10;
        private bool settings;
        private int currentArea=-1;
        private readonly Color ink=new Color(.045f,.10f,.16f,.94f),paper=new Color(.88f,.94f,.94f),gold=new Color(.96f,.74f,.41f);
        private static readonly string[] AreaNames={"回声庭院","共鸣林地","长夜回廊","心灯圣所"};
        private static readonly string[] AreaDescriptions={"完整的眼纹指向真实。","回应林中的生命，让沉睡的树根成为道路。","让勇气照亮落石、虚空与追逐。","组合三种能力，解开梦魇核心。"};

        private void Awake(){inputs=GetComponent<StarterAssetsInputs>();volume=PlayerPrefs.GetFloat("Heartlight.Volume",.65f);sensitivity=PlayerPrefs.GetFloat("Heartlight.Sensitivity",95);AudioListener.volume=volume;SetMode(ScreenMode.Introduction);}
        private void Start(){var camera=FindFirstObjectByType<FragmentsOfHer.Clean.CleanCameraFollow>();if(camera!=null)camera.mouseSensitivity=sensitivity;}
        public void StartJourney(){currentArea=-1;SetMode(ScreenMode.Playing);UpdateArea();FragmentsOfHer.Assignment3.FinalAudioDirector.Instance?.StartMusic();}
        public void SetMode(ScreenMode mode){Mode=mode;if(mode!=ScreenMode.Paused)settings=false;Time.timeScale=mode==ScreenMode.Playing?1:0;UpdateCursor();}
        public void FinishAfter(float seconds){StartCoroutine(Finish(seconds));}
        private IEnumerator Finish(float seconds){yield return new WaitForSeconds(seconds);game.Record("ending","journey","complete");SetMode(ScreenMode.Ending);}

        private void Update()
        {
            if(Mode==ScreenMode.Playing)UpdateArea();
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(Mode==ScreenMode.Playing)SetMode(ScreenMode.Paused);
                else if(Mode==ScreenMode.Paused){if(settings)settings=false;else SetMode(ScreenMode.Playing);}
            }
            if(Mode==ScreenMode.Introduction&&Input.GetKeyDown(KeyCode.Return))StartJourney();
        }
        private void UpdateArea()
        {
            float z=transform.position.z;int area=z<9?0:z<38?1:z<80?2:3;
            if(area==currentArea)return;currentArea=area;areaStarted=Time.unscaledTime;
        }
        private void LateUpdate(){UpdateCursor();}
        private void UpdateCursor()
        {
            bool menu=Mode!=ScreenMode.Playing;
            if(inputs!=null){inputs.cursorLocked=!menu;inputs.cursorInputForLook=!menu;if(menu){inputs.MoveInput(Vector2.zero);inputs.LookInput(Vector2.zero);}}
            Cursor.visible=menu;Cursor.lockState=menu?CursorLockMode.None:CursorLockMode.Locked;
        }

        private void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){font=game.chineseFont,fontSize=62,fontStyle=FontStyle.Normal,normal={textColor=paper},alignment=TextAnchor.MiddleLeft,clipping=TextClipping.Overflow};
            heading=new GUIStyle(title){fontSize=25};
            body=new GUIStyle(title){fontSize=18,wordWrap=true};
            small=new GUIStyle(body){fontSize=14,normal={textColor=new Color(.67f,.79f,.83f)}};
            centerTitle=new GUIStyle(title){alignment=TextAnchor.MiddleCenter};
            centerHeading=new GUIStyle(heading){alignment=TextAnchor.MiddleCenter};
            centerBody=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};
            centerSmall=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(GUI.skin.button){font=game.chineseFont,fontSize=19,padding=new RectOffset(20,20,8,8),normal={textColor=paper},hover={textColor=gold},clipping=TextClipping.Overflow};
            button.normal.background=null;button.hover.background=null;button.active.background=null;button.focused.background=null;
        }
        private void Fill(Rect r,Color c){Color before=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=before;}
        private void Label(float x,float y,float w,float h,string text,GUIStyle style){GUI.Label(new Rect(x,y,w,h),text,style);}
        private void FloatingText(Rect rect,string text,GUIStyle style,float alpha=1)
        {
            Color before=GUI.color;GUI.color=new Color(0,0,0,.8f*alpha);GUI.Label(new Rect(rect.x+2,rect.y+2,rect.width,rect.height),text,style);
            GUI.color=new Color(1,1,1,alpha);GUI.Label(rect,text,style);GUI.color=before;
        }
        private bool Button(float x,float y,float w,float h,string text)
        {
            var r=new Rect(x,y,w,h);bool hover=r.Contains(Event.current.mousePosition);
            Fill(r,hover?new Color(.14f,.31f,.37f):new Color(.085f,.22f,.28f));Fill(new Rect(x,y,3,h),hover?gold:new Color(.35f,.57f,.61f));
            return GUI.Button(r,text,button);
        }
        private void OnGUI()
        {
            Styles();float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);Matrix4x4 previous=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)*.5f,(Screen.height-720*scale)*.5f),Quaternion.identity,new Vector3(scale,scale,1));
            if(Mode==ScreenMode.Playing)Hud();else Menu();GUI.matrix=previous;
        }

        private void Hud()
        {
            for(int i=0;i<3;i++)
            {
                bool unlocked=i<game.Unlocked,selected=i==(int)game.Selected;float x=24+i*72;
                Color color=unlocked?HeartlightDirector.AbilityColor((Virtue)i):Color.gray;
                Fill(new Rect(x,632,64,64),selected?new Color(color.r*.32f,color.g*.32f,color.b*.32f,.95f):new Color(ink.r,ink.g,ink.b,.78f));
                Fill(new Rect(x,632,64,3),color);
                Label(x,638,64,24,(i+1).ToString(),centerBody);
                Label(x+3,665,58,22,unlocked?HeartlightDirector.AbilityName((Virtue)i):"未领悟",centerSmall);
            }
            if(game.CourageRemaining>0)
            {
                Fill(new Rect(248,668,150,5),new Color(ink.r,ink.g,ink.b,.72f));
                Fill(new Rect(248,668,150*game.CourageRemaining/game.courageDuration,5),HeartlightDirector.AbilityColor(Virtue.Courage));
                Label(248,678,180,22,"光芒 "+game.CourageRemaining.ToString("F1")+" 秒",small);
            }
            PuzzleNode node=game.Nearest;
            if(node!=null)
            {
                string action=node.definition.kind==PuzzleKind.Safety?"E  安全通行":!game.ConditionsMet(node.definition)?"先完成当前旅程目标":game.IsRotating(node)?"F  旋转方向    E  确认":"E  交互";
                FloatingText(new Rect(340,500,600,34),node.definition.title,centerHeading);
                FloatingText(new Rect(340,536,600,30),action,centerBody);
            }
            else if(game.Unlocked>=3&&game.Selected==Virtue.Courage)FloatingText(new Rect(440,540,400,32),"E  释放勇气之光",centerBody);

            float areaAge=Time.unscaledTime-areaStarted;
            if(currentArea>=0&&areaAge<3)
            {
                float alpha=1-Mathf.InverseLerp(2.35f,3,areaAge);
                FloatingText(new Rect(260,25,760,38),AreaNames[currentArea],centerHeading,alpha);
                FloatingText(new Rect(260,61,760,30),AreaDescriptions[currentArea],centerBody,alpha);
            }
            if(Time.unscaledTime<game.MessageUntil)
            {
                float alpha=Mathf.Clamp01((game.MessageUntil-Time.unscaledTime)/.55f);
                FloatingText(new Rect(230,112,820,52),game.Message,centerBody,alpha);
            }
        }

        private void Menu()
        {
            Fill(new Rect(-2000,-2000,5280,4720),new Color(.025f,.065f,.10f,.78f));
            Fill(new Rect(350,70,580,580),ink);Fill(new Rect(350,70,4,580),gold);
            Label(390,108,500,34,"A LIGHT WITHIN",centerBody);Label(390,148,500,88,"心灯",centerTitle);
            if(settings){SettingsMenu();return;}
            if(Mode==ScreenMode.Introduction)
            {
                Label(410,285,460,125,"WASD  移动      鼠标  视角\n空格  跳跃      Shift  疾跑\n1 / 2 / 3  选择能力\nE  交互      F  旋转      R  返回检查点\nEsc  暂停",centerBody);
                if(Button(430,472,420,52,"开始游戏  /  Enter"))StartJourney();
                if(Button(430,540,420,48,"退出游戏"))Quit();
            }
            else if(Mode==ScreenMode.Paused)
            {
                Label(410,255,460,46,"旅途暂停",centerHeading);
                if(Button(430,320,420,46,"继续游戏"))SetMode(ScreenMode.Playing);
                if(Button(430,380,200,44,"返回检查点")){SetMode(ScreenMode.Playing);game.Respawn(false);}
                if(Button(650,380,200,44,"设置"))settings=true;
                if(Button(430,440,420,44,"重新开始"))Restart();
                if(Button(430,500,420,44,"退出游戏"))Quit();
            }
            else
            {
                Label(390,267,500,42,"洞察真实，回应世界，拥抱勇气。",centerHeading);
                Label(390,340,500,74,"旅途完成  "+Mathf.FloorToInt(game.PlaySeconds/60)+" 分 "+Mathf.FloorToInt(game.PlaySeconds%60)+" 秒\n心灯 11 / 11",centerBody);
                if(Button(430,470,200,48,"再来一次"))Restart();
                if(Button(650,470,200,48,"退出游戏"))Quit();
            }
        }
        private void SettingsMenu()
        {
            Label(410,270,460,34,"声音音量  "+Mathf.RoundToInt(volume*100)+"%",body);float nextVolume=GUI.HorizontalSlider(new Rect(430,314,420,24),volume,0,1);
            Label(410,354,460,34,"镜头灵敏度  "+Mathf.RoundToInt(sensitivity),body);float nextSensitivity=GUI.HorizontalSlider(new Rect(430,398,420,24),sensitivity,40,150);
            if(nextVolume!=volume||nextSensitivity!=sensitivity){volume=nextVolume;sensitivity=nextSensitivity;AudioListener.volume=volume;var camera=FindFirstObjectByType<FragmentsOfHer.Clean.CleanCameraFollow>();if(camera!=null)camera.mouseSensitivity=sensitivity;PlayerPrefs.SetFloat("Heartlight.Volume",volume);PlayerPrefs.SetFloat("Heartlight.Sensitivity",sensitivity);}
            if(Button(430,480,420,48,"返回")){settings=false;PlayerPrefs.Save();}
        }
        private void Restart(){Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
        private void Quit(){PlayerPrefs.Save();Time.timeScale=1;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy(){Time.timeScale=1;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;}
    }
}
