using BepInEx;
using GorillaNetworking;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Valve.VR;

namespace FortniteEmoteWheel.Classes
{
    public class Wheel : MonoBehaviour
    {
        public static Wheel instance;

        private bool IsSteam;
        private GameObject Base;
        private GameObject Selector;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }

            try
            {
                if (PlayFabAuthenticator.instance != null)
                {
                    var platformObj = Traverse.Create(PlayFabAuthenticator.instance).Field("platform").GetValue();
                    if (platformObj != null)
                        IsSteam = platformObj.ToString().ToLower() == "steam";
                }
            }
            catch
            {
                IsSteam = false;
            }

            instance = this;

            Transform baseTransform = transform.Find("Base");
            if (baseTransform != null)
            {
                Base = baseTransform.gameObject;
                Transform selectorTransform = baseTransform.Find("Selected");
                if (selectorTransform != null)
                    Selector = selectorTransform.gameObject;
            }
            if (Base != null)
            {
                Canvas canvas = Base.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.worldCamera = Camera.main; // Prevents white screen overlay
                }

                SetupWheelUI();
                SetVisible(false);
            }
        }
        private void SetupWheelUI()
        {
            Transform canvasTransform = Base.transform.Find("Canvas");
            if (canvasTransform == null) return;

            WheelUISetup.ApplyCanvasScaler(canvasTransform.gameObject);
            WheelUISetup.ApplyGraphicRaycaster(canvasTransform.gameObject);

            // Create PageName GameObject if it doesn't exist in bundle
            Transform pageNameTransform = canvasTransform.Find("PageName");
            if (pageNameTransform == null)
            {
                GameObject pageObj = new GameObject("PageName");
                pageObj.transform.SetParent(canvasTransform, false);
                pageNameTransform = pageObj.transform;
            }
            WheelUISetup.ApplyText(pageNameTransform.gameObject, "ORIGINAL 1");

            // Create EmoteName GameObject if it doesn't exist in bundle
            Transform emoteNameTransform = canvasTransform.Find("EmoteName");
            if (emoteNameTransform == null)
            {
                GameObject emoteObj = new GameObject("EmoteName");
                emoteObj.transform.SetParent(canvasTransform, false);
                emoteNameTransform = emoteObj.transform;
            }
            WheelUISetup.ApplyText(emoteNameTransform.gameObject, "");
        }
        public void SetVisible(bool visible)
        {
            if (Base != null)
                Base.SetActive(visible);
        }

        public float GetTurnAngle(Vector2 position)
        {
            if (position.magnitude < 0.01f)
                return 90;

            return Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
        }

        public Vector2 GetLeftJoystickAxis()
        {
            if (IsSteam)
                return SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.GetAxis(SteamVR_Input_Sources.LeftHand);

            if (ControllerInputPoller.instance != null)
            {
                ControllerInputPoller.instance.leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 leftJoystick);
                return leftJoystick;
            }
            return Vector2.zero;
        }

        public Vector2 GetRightJoystickAxis()
        {
            if (IsSteam)
                return SteamVR_Actions.gorillaTag_RightJoystick2DAxis.GetAxis(SteamVR_Input_Sources.RightHand);

            if (ControllerInputPoller.instance != null)
            {
                ControllerInputPoller.instance.rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 rightJoystick);
                return rightJoystick;
            }
            return Vector2.zero;
        }

        public bool GetLeftJoystickDown()
        {
            if (IsSteam)
                return SteamVR_Actions.gorillaTag_LeftJoystickClick.GetState(SteamVR_Input_Sources.LeftHand);

            if (ControllerInputPoller.instance != null)
            {
                ControllerInputPoller.instance.leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out bool leftJoystickClick);
                return leftJoystickClick;
            }
            return false;
        }

        public bool GetRightJoystickDown()
        {
            if (IsSteam)
                return SteamVR_Actions.gorillaTag_RightJoystickClick.GetState(SteamVR_Input_Sources.RightHand);

            if (ControllerInputPoller.instance != null)
            {
                ControllerInputPoller.instance.rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out bool rightJoystickClick);
                return rightJoystickClick;
            }
            return false;
        }

        private float changePageDelay = 0f;
        private float prevRotation = -9999f;

        private int Selection = 0;
        private int Page = 0;

        public void Update()
        {
            if (Base == null) return;

            bool bHeld = !XRSettings.isDeviceActive && UnityInput.Current.GetKey(KeyCode.B);
            bool vHeld = !XRSettings.isDeviceActive && UnityInput.Current.GetKey(KeyCode.V);
            bool leftButton = !XRSettings.isDeviceActive && Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool rightButton = !XRSettings.isDeviceActive && Mouse.current != null && Mouse.current.rightButton.isPressed;

            Vector2 Direction = bHeld && Mouse.current != null ? -new Vector2(Screen.width / 2f - Mouse.current.position.x.value, Screen.height / 2f - Mouse.current.position.y.value).normalized : GetRightJoystickAxis();

            if (GetLeftJoystickDown() || vHeld)
            {
                Plugin.emoteTime = -9999f;
                Plugin.audiomgr?.GetComponent<AudioSource>()?.Stop();
            }

            if ((leftButton || rightButton || Mathf.Abs(GetLeftJoystickAxis().x) > 0.5f) && Time.time > changePageDelay && Base.activeSelf)
            {
                changePageDelay = Time.time + 0.15f;
                int lastPage = 9;

                Plugin.Play2DAudio(Plugin.LoadSoundFromResource("nav"), 0.5f);

                Page += (GetLeftJoystickAxis().x > 0.5f || rightButton ? 1 : -1);
                if (Page < 0)
                    Page = lastPage;

                if (Page > lastPage)
                    Page = 0;

                Transform pagesTransform = Base.transform.Find("Pages");
                if (pagesTransform != null)
                {
                    for (int i = 0; i < pagesTransform.childCount; i++)
                    {
                        Transform child = pagesTransform.GetChild(i);
                        if (int.TryParse(child.name, out int childPage))
                        {
                            child.gameObject.SetActive(childPage == Page);
                        }
                    }
                }
            }

            if (GetRightJoystickDown() || bHeld)
            {
                if (Time.time > Plugin.emoteTime)
                {
                    // Safe search and disable for SnapTurn component
                    GameObject gorillaPlayer = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer");
                    if (gorillaPlayer != null && gorillaPlayer.TryGetComponent<GorillaSnapTurn>(out var snapTurn))
                    {
                        snapTurn.enabled = false;
                    }

                    SetVisible(true);
                }

                if (Base.activeSelf)
                {
                    if (bHeld && GorillaTagger.Instance != null && GorillaTagger.Instance.headCollider != null)
                    {
                        Base.transform.position = GorillaTagger.Instance.headCollider.transform.position + GorillaTagger.Instance.headCollider.transform.forward;
                        Base.transform.rotation = GorillaTagger.Instance.headCollider.transform.rotation * Quaternion.Euler(0f, 0f, 180f);
                    }

                    if (Selector != null)
                    {
                        Selector.transform.localRotation = Quaternion.Euler(0f, 0f, 45 * Mathf.Round((GetTurnAngle(Direction) - 90f) / 45f));

                        if (Selector.transform.localRotation.z != prevRotation)
                            Plugin.Play2DAudio(Plugin.LoadSoundFromResource("nav"), 0.5f);
                        prevRotation = Selector.transform.localRotation.z;
                    }

                    int selected = (int)Math.Floor(Math.Round((GetTurnAngle(Direction) - 90f) / 45f));
                    Selection = selected;

                    string pageTitle = "";
                    string emoteTitle = "";

                    if (Page == 0)
                    {
                        pageTitle = "ORIGINAL 1";
                        switch (selected)
                        {
                            case 0: emoteTitle = "DANCE MOVES"; break;
                            case -1: emoteTitle = "TAKE THE L"; break;
                            case -2: emoteTitle = "REANIMATED"; break;
                            case -3: emoteTitle = "ELECTRO SHUFFLE"; break;
                            case -4: emoteTitle = "ORANGE JUSTICE"; break;
                            case -5: emoteTitle = "RIDE THE PONY"; break;
                            case -6: case 2: emoteTitle = "FRESH"; break;
                            case 1: emoteTitle = "ELECTRO SWING"; break;
                        }
                    }
                    else if (Page == 1)
                    {
                        pageTitle = "ORIGINAL 2";
                        switch (selected)
                        {
                            case 0: emoteTitle = "FLOSS"; break;
                            case -1: emoteTitle = "DISCO FEVER"; break;
                            case -2: emoteTitle = "BOOGIE DOWN"; break;
                            case -3: emoteTitle = "THE ROBOT"; break;
                            case -4: emoteTitle = "BEST MATES"; break;
                        }
                    }
                    else if (Page == 2)
                    {
                        pageTitle = "NEW";
                        switch (selected)
                        {
                            case 0: emoteTitle = "PAWS & CLAWS"; break;
                            case -1: emoteTitle = "GET GRIDDY"; break;
                            case -2: emoteTitle = "PULL UP"; break;
                            case -3: emoteTitle = "POPULAR VIBE"; break;
                            case -4: emoteTitle = "LUCID DREAMS"; break;
                            case -5: emoteTitle = "EMPTY OUT YOUR POCKETS"; break;
                            case -6: case 2: emoteTitle = "WHAT YOU WANT"; break;
                            case 1: emoteTitle = "THE RENEGADE"; break;
                        }
                    }
                    else if (Page == 3)
                    {
                        pageTitle = "COMMISSIONS 1";
                        switch (selected)
                        {
                            case 0: emoteTitle = "JABBA SWITCHWAY"; break;
                            case -1: emoteTitle = "INFINITE DAB"; break;
                            case -2: emoteTitle = "CELEBRATE ME"; break;
                            case -3: emoteTitle = "BILLY BOUNCE"; break;
                            case -4: emoteTitle = "WINDMILL FLOSS"; break;
                            case -5: emoteTitle = "HYPE"; break;
                            case -6: case 2: emoteTitle = "ENTRANCED"; break;
                            case 1: emoteTitle = "LAUGH IT UP"; break;
                        }
                    }
                    else if (Page == 4)
                    {
                        pageTitle = "COMMISSIONS 2";
                        switch (selected)
                        {
                            case 0: emoteTitle = "SNOOP WALK"; break;
                            case -1: emoteTitle = "SCENARIO"; break;
                            case -2: emoteTitle = "NIGHT OUT"; break;
                            case -3: emoteTitle = "POINT AND STRUT"; break;
                            case -4: emoteTitle = "MOONGAZER"; break;
                            case -5: emoteTitle = "ROLLIE"; break;
                            case -6: case 2: emoteTitle = "HEEL CLICK BREAKDOWN"; break;
                            case 1: emoteTitle = "SWITCHSTEP"; break;
                        }
                    }
                    else if (Page == 5)
                    {
                        pageTitle = "COMMISSIONS 3";
                        switch (selected)
                        {
                            case 0: emoteTitle = "FREESTYLIN'"; break;
                            case -1: emoteTitle = "GO MUFASA"; break;
                            case -2: emoteTitle = "JUBI SLIDE"; break;
                            case -3: emoteTitle = "RUNNING MAN"; break;
                            case -4: emoteTitle = "ZANY"; break;
                            case -5: emoteTitle = "PUMPERNICKEL"; break;
                            case -6: case 2: emoteTitle = "PONY UP"; break;
                            case 1: emoteTitle = "HULA"; break;
                        }
                    }
                    else if (Page == 6)
                    {
                        pageTitle = "COMMISSIONS 4";
                        switch (selected)
                        {
                            case 0: emoteTitle = "NEVER GONNA'"; break;
                            case -1: emoteTitle = "SAY SO"; break;
                            case -2: emoteTitle = "TAKE IT SLOW"; break;
                            case -3: emoteTitle = "MACARENA"; break;
                            case -4: emoteTitle = "CUPID'S ARROW"; break;
                            case -5: emoteTitle = "GANGNAM STYLE"; break;
                            case -6: case 2: emoteTitle = "REAL SLIM SHADY"; break;
                            case 1: emoteTitle = "PARTY HIPS"; break;
                        }
                    }
                    else if (Page == 7)
                    {
                        pageTitle = "COMMISSIONS 5";
                        switch (selected)
                        {
                            case 0: emoteTitle = "OUT WEST"; break;
                            case -1: emoteTitle = "MY WORLD"; break;
                            case -2: emoteTitle = "JAKE BUG DANCE"; break;
                            case -3: emoteTitle = "MIKU MIKU BEAM"; break;
                        }
                    }
                    else if (Page == 8)
                    {
                        pageTitle = "COMMISSIONS 6";
                        switch (selected)
                        {
                            case 0: emoteTitle = "WITHOUT YOU"; break;
                            case -1: emoteTitle = "TOOSIE SLIDE"; break;
                            case -2: emoteTitle = "LOVE ME NOT"; break;
                            case -3: emoteTitle = "CALIFORNIA GURLS"; break;
                            case -4: emoteTitle = "MR BRIGHTSIDE"; break;
                            case -5: emoteTitle = "RANSOM"; break;
                            case -6: case 2: emoteTitle = "GET LUCKY"; break;
                            case 1: emoteTitle = "REBELLIOUS"; break;


                        }
                    }
                    else if (Page == 9)
                    {
                        pageTitle = "COMMISSIONS 7";
                        switch (selected)
                        {
                            case 0: emoteTitle = "DANCIN' DOMINO"; break;
                            case -1: emoteTitle = "CHASE ME DOWN"; break;
                            case -2: emoteTitle = "ALL I WANT IS YOU"; break;
                            case -3: emoteTitle = "LUCID DREAMS (UPDATED)"; break;
                        }

                    }
                        if (Selector != null)
                    {
                        Selector.transform.localRotation = Quaternion.Euler(0f, 0f, 45 * Mathf.Round((GetTurnAngle(Direction) - 90f) / 45f));

                        if (Selector.transform.localRotation.z != prevRotation)
                            Plugin.Play2DAudio(Plugin.LoadSoundFromResource("nav"), 0.5f);
                        prevRotation = Selector.transform.localRotation.z;
                    }


                    Transform pageTextObj = Base.transform.Find("Canvas/PageName");
                    if (pageTextObj != null && pageTextObj.TryGetComponent<Text>(out var pageText))
                        pageText.text = pageTitle;

                    Transform emoteTextObj = Base.transform.Find("Canvas/EmoteName");
                    if (emoteTextObj != null && emoteTextObj.TryGetComponent<Text>(out var emoteText))
                        emoteText.text = emoteTitle;
                }
            }
            else
            {
                if (Base.activeSelf)
                {
                    GameObject gorillaPlayer = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer");
                    if (gorillaPlayer != null && gorillaPlayer.TryGetComponent<GorillaSnapTurn>(out var snapTurn))
                    {
                        snapTurn.enabled = true;
                    }

                    SetVisible(false);

                    if (Page == 0)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("Dance Moves", "default"); break;
                            case -1: Plugin.Emote("TakeTheL", "takethel", -1f, true); break;
                            case -2: Plugin.Emote("Reanimated", "reanimated", -1f, true); break;
                            case -3: Plugin.Emote("ElectroShuffle", "electroshuffle", -1f, true); break;
                            case -4: Plugin.Emote("OrangeJustice", "oj", -1f, true); break;
                            case -5: Plugin.Emote("RideThePony", "ridethepony", -1f, true); break;
                            case -6: case 2: Plugin.Emote("Emote_Fresh", "fresh", -1f, true); break;
                            case 1: Plugin.Emote("ElectroSwing", "swing", -1f, true); break;
                        }
                    }
                    else if (Page == 1)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("Emote_FlossDance_CMM", "floss", -1f, true); break;
                            case -1: Plugin.Emote("DiscoFever", "discofever", -1f, true); break;
                            case -2: Plugin.Emote("BoogieDownLoop", "boogiedown", -1f, true); break;
                            case -3: Plugin.Emote("Emote_RobotDance", "therobot", -1f, true); break;
                            case -4: Plugin.Emote("BestMates", "bestmates", -1f, true); break;
                        }
                    }
                    else if (Page == 2)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("Paws&Claws", "pawsclaws", -1f, true); break;
                            case -1: Plugin.Emote("Get Griddy", "Emote_Griddles_Music_Loop_01", -1f, true); break;
                            case -2: Plugin.Emote("Pull Up", "Gas_Station_Loop", -1f, true); break;
                            case -3: Plugin.Emote("Popular Vibe", "Emote_SpeedDial_Loop", -1f, true); break;
                            case -4: Plugin.Emote("Lucid DreamsLoop", "Emote_KelpLinen_Music_Loop", -1f, true); break;
                            case -5: Plugin.Emote("Empty Out Your PocketsLoop", "eoyp", -1f, true); break;
                            case -6: case 2: Plugin.Emote("WhatYouWant", "whatyouwant", -1f, true); break;
                            case 1: Plugin.Emote("The Renegade", "Emote_Just_Home_Music_Loop", -1f, true); break;
                        }
                    }
                    else if (Page == 3)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("Jabba Switchway Loop", "Emote_January_Bop_Loop", -1f, true); break;
                            case -1: Plugin.Emote("InfinidabLoop", "infinitedab", -1f, true); break;
                            case -2: Plugin.Emote("Celebrate Me", "IP_Emote_Cottontail_Loop", -1f, true); break;
                            case -3: Plugin.Emote("BillyBounce", "billybounce", -1f, true); break;
                            case -4: Plugin.Emote("WindmillFloss", "whirlfloss", -1f, true); break;
                            case -5: Plugin.Emote("Hype", "hype", -1f, true); break;
                            case -6: case 2: Plugin.Emote("Entranced", "entranced", -1f, true); break;
                            case 1: Plugin.Emote("LaughItUp", "Emote_Laugh_01"); break;
                        }
                    }
                    else if (Page == 4)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("SnoopWalk", "snoopwalk", -1f, true); break;
                            case -1: Plugin.Emote("Scenario", "scenario", -1f, true); break;
                            case -2: Plugin.Emote("Night Out", "nightout", -1f, true); break;
                            case -3: Plugin.Emote("pointandstrut", "pointandstrut", -1f, true); break;
                            case -4: Plugin.Emote("moongazer", "moongazer", -1f, true); break;
                            case -5: Plugin.Emote("Rollie", "Emote_Twist_Daytona_Music_Loop_01", -1f, true); break;
                            case -6: case 2: Plugin.Emote("HEEL", "heelclickbreakdown", -1f, true); break;
                            case 1: Plugin.Emote("SwitchStep", "switchstep", -1f, true); break;
                        }
                    }
                    else if (Page == 5)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("Freestylin'", "freestylin", -1f, true); break;
                            case -1: Plugin.Emote("Go Mufasa", "Emote_Sandwich_Bop_Loop", -1f, true); break;
                            case -2: Plugin.Emote("jubislide", "Emote_GoodbyeUpbeat_Loop", -1f, true); break;
                            case -3: Plugin.Emote("RunningMan", "Athena_Emote_Music_RunningMan", -1f, true); break;
                            case -4: Plugin.Emote("Zany", "zany", -1f, true); break;
                            case -5: Plugin.Emote("pumpernickel2", "Athena_Emotes_Music_PumpDance", -1f, true); break;
                            case -6: case 2: Plugin.Emote("RideThePony", "ponyup", -1f, true); break;
                            case 1: Plugin.Emote("HULA", "emote_hula_01", -1f, true); break;
                        }
                    }
                    else if (Page == 6)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("Never Gonna Loop", "Emote_NeverGonna_Loop_01", -1f, true); break;
                            case -1: Plugin.Emote("Say So", "Emote_HotPink_Loop_258", -1f, true); break;
                            case -2: Plugin.Emote("Takeitslow", "takeitslow", -1f, true); break;
                            case -3: Plugin.Emote("Macarena", "Emote_Macaroon_Music_Loop_01", -1f, true); break;
                            case -4: Plugin.Emote("cupid", "cupid", -1f, true); break;
                            case -5: Plugin.Emote("gangnam", "gangnam", -1f, true); break;
                            case -6: case 2: Plugin.Emote("realslimshady", "slim", -1f, true); break;
                            case 1: Plugin.Emote("partyhips", "partyhips", -1f, true); break;
                        }
                    }
                    else if (Page == 7)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("outwest", "outwest", -1f, true); break;
                            case -1: Plugin.Emote("myworld", "Myworld", -1f, true); break;
                            case -2: Plugin.Emote("Jake", "jake", -1f, true); break;
                            case -3: Plugin.Emote("miku", "miku", -1f, true); break;
                        }
                    }
                    else if (Page == 8)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("Without You", "Devotion", -1f, true); break;
                            case -1: Plugin.Emote("Toosie Slide", "Toosie", -1f, true); break;
                            case -2: Plugin.Emote("Love Me Not", "love not", -1f, true); break;
                            case -3: Plugin.Emote("California Gurls", "Cali girl", -1f, true); break;
                            case -4: Plugin.Emote("Mr Brightside", "brightside", -1f, true); break;
                            case -5: Plugin.Emote("Ransom", "Ransom", -1f, true); break;
                            case -6: case 2: Plugin.Emote("Get Lucky", "Lucky", -1f, true); break;
                            case 1: Plugin.Emote("Rebellious", "Rebel", 1f, true); break;
                        }
                    }
                    else if (Page == 9)
                    {
                        switch (Selection)
                        {
                            case 0: Plugin.Emote("Dancin' Domino", "Domino", -1f, true); break;
                            case -1: Plugin.Emote("Chase Me Down", "Chase me", -1f, true); break;
                            case -2: Plugin.Emote("All i Want is You", "all i want", -1f, true); break;
                            case -3: Plugin.Emote("Lucid Dreams new", "Lucid dreams NEW", -1f, true); break;
                        }
                    }
                    prevRotation = -9999f;
                }
            }
        }
    }
}