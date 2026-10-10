using BepInEx;
using HarmonyLib;
using Photon.Pun;
using Colossal.MakeItFuckingWork;
using Console;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Atlas_Remade.Emotes;
using BepInEx.Configuration;

namespace FortniteEmoteWheel
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

        // Custom config file instance and entries
        public static ConfigFile CustomConfig;
        public static ConfigEntry<bool> EnableElbowTracking;
        public static ConfigEntry<bool> EnableBodyTracking;
        public static ConfigEntry<bool> EnableFingerTracking;

        public void Awake()
        {
            Instance = this;

            string customConfigPath = Path.Combine(Paths.ConfigPath, "com.Ademuzel.Fortnitemotewheel.Config.cfg");

            CustomConfig = new ConfigFile(customConfigPath, true);

            EnableElbowTracking = CustomConfig.Bind(
                "Tracking",
                "EnableElbowTracking",
                true,
                "==Elbow Tracking==\nDefault Value: True\nExamples: \"True\"   \"False\"\nCurrent: True"
            );

            EnableBodyTracking = CustomConfig.Bind(
                "Tracking",
                "EnableBodyTracking",
                true,
                "==Body Tracking==\nDefault Value: True\nExamples: \"True\"   \"False\"\nCurrent: True"
            );

            EnableFingerTracking = CustomConfig.Bind(
                "Tracking",
                "EnableFingerTracking",
                true,
                "==Finger Tracking==\nDefault Value: True\nExamples: \"True\"   \"False\"\nCurrent: True"
            );

            Console.Console.LoadConsole();

            if (RigUtils.Instance == null)
                gameObject.AddComponent<RigUtils>();
        }

        // ---- Update checker -------------------------------------------------
        private const string UpdateApiUrl = "https://api.github.com/repos/Ademuzel/Adems-Fortnite-Emote-Wheel/releases/latest";
        private const string UpdateMessage = "NEW UPDATE AVAILABLE!\nDownload the latest Fortnite Emote Wheel from GitHub.";
        private const string BetaWarningMessage = "<color=grey>[<color=red>WARNING</color>] You Are Using A Beta Version, Mod Might Become Unstable</color>";
        private const float UpdateWarningSeconds = 6f;

        private static bool updateAvailable;
        private static bool isBetaVersion;
        private static bool updateWarningStarted;
        private static float updateWarningEndTime;
        private static GameObject updateWarningObject;

        private IEnumerator CheckForUpdate()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(UpdateApiUrl))
            {
                request.SetRequestHeader("User-Agent", "FortniteEmoteWheel");
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[FortniteEmoteWheel] Update check failed: " + request.error);
                    yield break;
                }

                Match match = Regex.Match(request.downloadHandler.text, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                if (!match.Success)
                {
                    Debug.LogWarning("[FortniteEmoteWheel] Update check: couldn't find a release tag in GitHub's response.");
                    yield break;
                }

                string latestTag = match.Groups[1].Value;

                if (IsNewerVersion(latestTag, PluginInfo.Version))
                {
                    Debug.Log($"[FortniteEmoteWheel] New release available: {latestTag} (running {PluginInfo.Version})");
                    updateAvailable = true;
                }
                else if (IsBetaVersionCheck(latestTag, PluginInfo.Version))
                {
                    Debug.Log($"[FortniteEmoteWheel] Running a newer/beta version than GitHub: {PluginInfo.Version} (latest release: {latestTag})");
                    isBetaVersion = true;
                }
            }
        }

        private static bool IsNewerVersion(string remoteTag, string localVersion)
        {
            string remote = remoteTag.Trim().TrimStart('v', 'V');
            string local = localVersion.Trim().TrimStart('v', 'V');

            System.Version remoteVersion;
            System.Version localVersionParsed;
            if (System.Version.TryParse(remote, out remoteVersion) && System.Version.TryParse(local, out localVersionParsed))
                return remoteVersion > localVersionParsed;

            return false;
        }

        private static bool IsBetaVersionCheck(string remoteTag, string localVersion)
        {
            string remote = remoteTag.Trim().TrimStart('v', 'V');
            string local = localVersion.Trim().TrimStart('v', 'V');

            System.Version remoteVersion;
            System.Version localVersionParsed;
            if (System.Version.TryParse(remote, out remoteVersion) && System.Version.TryParse(local, out localVersionParsed))
                return localVersionParsed > remoteVersion;

            return remote != local;
        }

        private static void UpdateWarningTick()
        {
            if (!updateAvailable && !isBetaVersion)
                return;

            if (!updateWarningStarted)
            {
                if (VRRig.LocalRig == null || VRRig.LocalRig.mainCamera == null)
                    return;

                updateWarningStarted = true;
                updateWarningEndTime = Time.time + UpdateWarningSeconds;

                string messageToDisplay = isBetaVersion ? BetaWarningMessage : UpdateMessage;
                updateWarningObject = CreateUpdateWarning(VRRig.LocalRig.mainCamera.transform, messageToDisplay);
            }
            else if (updateWarningObject != null && Time.time >= updateWarningEndTime)
            {
                Destroy(updateWarningObject);
                updateWarningObject = null;
            }
        }

        private static GameObject CreateUpdateWarning(Transform cameraTransform, string message)
        {
            return CreateWorldText("UpdateWarning", message, Color.red, new Vector3(0f, -0.1f, 1f), cameraTransform);
        }

        private const string AdemPlayerId = "62E26F3BFB74CB62";
        private const string EthenPlayerId = "22D12CA26018212A";
        private const string CrimsonPlayerId = "F641264BC1E4B9C9";
        private const string iiDk = "BE5EBDCA38818858";
        private const float GreetingSeconds = 6f;

        private static bool greetingStarted;
        private static bool greetingDone;
        private static float greetingEndTime;
        private static GameObject greetingObject;
        private static bool loggedPlayerId;

        private static string GetPlayFabIdViaReflection()
        {
            try
            {
                foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    System.Type type = null;
                    try
                    {
                        type = asm.GetType("GorillaNetworking.PlayFabAuthenticator") ?? asm.GetType("PlayFabAuthenticator");
                    }
                    catch { }

                    if (type == null)
                        continue;

                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

                    object instance = null;
                    FieldInfo instanceField = type.GetField("instance", flags);
                    if (instanceField != null)
                        instance = instanceField.GetValue(null);
                    else
                    {
                        PropertyInfo instanceProp = type.GetProperty("instance", flags) ?? type.GetProperty("Instance", flags);
                        if (instanceProp != null)
                            instance = instanceProp.GetValue(null, null);
                    }

                    if (instance == null)
                        return null;

                    MethodInfo getId = type.GetMethod("GetPlayFabPlayerId", flags);
                    if (getId != null)
                        return getId.Invoke(instance, null) as string;

                    FieldInfo idField = type.GetField("_playFabPlayerIdCache", flags);
                    if (idField != null)
                        return idField.GetValue(instance) as string;

                    return null;
                }
            }
            catch { }

            return null;
        }

        private static string GetLocalPlayerId()
        {
            string id = GetPlayFabIdViaReflection();

            if (string.IsNullOrEmpty(id))
            {
                try
                {
                    if (NetworkSystem.Instance != null && NetworkSystem.Instance.LocalPlayer != null)
                        id = NetworkSystem.Instance.LocalPlayer.UserId;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(id))
            {
                try
                {
                    if (VRRig.LocalRig != null && VRRig.LocalRig.Creator != null)
                        id = VRRig.LocalRig.Creator.UserId;
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(id) && !loggedPlayerId)
            {
                loggedPlayerId = true;
                Debug.Log("[FortniteEmoteWheel] Detected local player ID: " + id);
            }

            return id;
        }

        private static string GetGreetingFor(string playerId)
        {
            if (string.Equals(playerId, AdemPlayerId, System.StringComparison.OrdinalIgnoreCase))
                return "Hello Adem!";

            if (string.Equals(playerId, EthenPlayerId, System.StringComparison.OrdinalIgnoreCase))
                return "Hello Ethen!";

            if (string.Equals(playerId, CrimsonPlayerId, System.StringComparison.OrdinalIgnoreCase))
                return "Hello Crimson(iiDk's alt)!";

            if (string.Equals(playerId, iiDk, System.StringComparison.OrdinalIgnoreCase))
                return "Hello Goldentrophy(iiDk)!";

            return null;
        }

        private static void GreetingTick()
        {
            if (greetingDone)
                return;

            if (!greetingStarted)
            {
                string playerId = GetLocalPlayerId();
                if (string.IsNullOrEmpty(playerId))
                    return;

                string greeting = GetGreetingFor(playerId);
                if (greeting == null)
                {
                    greetingDone = true;
                    return;
                }

                if (VRRig.LocalRig == null || VRRig.LocalRig.mainCamera == null)
                    return;

                greetingStarted = true;
                greetingEndTime = Time.time + GreetingSeconds;
                greetingObject = CreateWorldText("Greeting", greeting, Color.white, new Vector3(0f, 0.1f, 1f), VRRig.LocalRig.mainCamera.transform);
            }
            else if (Time.time >= greetingEndTime)
            {
                if (greetingObject != null)
                    Destroy(greetingObject);

                greetingObject = null;
                greetingDone = true;
            }
        }

        private static GameObject CreateWorldText(string objectName, string message, Color color, Vector3 localPosition, Transform cameraTransform)
        {
            GameObject canvasObject = new GameObject(objectName);
            canvasObject.transform.SetParent(cameraTransform, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 400f);

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(canvasObject.transform, false);

            UnityEngine.UI.Text text = textObject.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = message;
            text.color = color;
            text.fontSize = 48;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperCenter;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return canvasObject;
        }

        public static AssetBundle assetBundle;
        public static GameObject LoadAsset(string assetName)
        {
            GameObject gameObject = null;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FortniteEmoteWheel.Resources.fn");
            if (stream != null)
            {
                if (assetBundle == null)
                    assetBundle = AssetBundle.LoadFromStream(stream);
                gameObject = Instantiate<GameObject>(assetBundle.LoadAsset<GameObject>(assetName));
            }
            else
                Debug.LogError("Failed to load asset from resource: " + assetName);

            return gameObject;
        }

        public static GameObject audiomgr = null;
        public static void Play2DAudio(AudioClip sound, float volume, bool looping = false)
        {
            if (audiomgr == null)
            {
                audiomgr = new GameObject("2DAudioMgr");
                AudioSource temp = audiomgr.AddComponent<AudioSource>();
                temp.spatialBlend = 0f;
            }
            AudioSource ausrc = audiomgr.GetComponent<AudioSource>();
            ausrc.volume = volume;
            ausrc.loop = looping;
            if (!looping)
                ausrc.PlayOneShot(sound);
            else
            {
                ausrc.clip = sound;
                ausrc.Play();
            }
        }

        public static Dictionary<string, AudioClip> audioPool = new Dictionary<string, AudioClip> { };
        public static AudioClip LoadSoundFromResource(string resourcePath)
        {
            AudioClip sound = null;

            if (!audioPool.ContainsKey(resourcePath))
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FortniteEmoteWheel.Resources.fn");
                if (stream != null)
                {
                    if (assetBundle == null)
                        assetBundle = AssetBundle.LoadFromStream(stream);

                    sound = assetBundle.LoadAsset<AudioClip>(resourcePath);

                    if (sound != null)
                        audioPool.Add(resourcePath, sound);
                }
            }
            else
                sound = audioPool[resourcePath];

            return sound;
        }

        private static Coroutine introRoutine;
        private static Coroutine introAnimRoutine;

        private static AnimationClip FindClip(Animator animator, string clipName)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == clipName)
                    return clip;

            return null;
        }

        private static IEnumerator PlayLoopAnimAfter(Animator animator, string loopAnimName, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (animator != null)
                animator.Play(loopAnimName);

            introAnimRoutine = null;
        }

        private static System.Type vmType;
        private static MethodInfo vmAudioClip, vmStopClip;
        private static object currentVmClip;

        private static object GetSeralythVoiceManager()
        {
            try
            {
                if (vmType == null)
                {
                    foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        System.Type t = asm.GetType("Seralyth.Managers.VoiceManager", false);
                        if (t == null)
                            continue;

                        vmAudioClip = t.GetMethod("AudioClip");
                        vmStopClip = t.GetMethod("StopAudioClip");
                        if (vmAudioClip == null || vmStopClip == null)
                            continue;

                        vmType = t;
                        break;
                    }
                }

                if (vmType == null)
                    return null;

                PropertyInfo instanceProp = vmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                return instanceProp != null ? instanceProp.GetValue(null, null) : null;
            }
            catch { return null; }
        }

        private static void StopVmClip()
        {
            if (currentVmClip == null)
                return;

            try
            {
                object vm = GetSeralythVoiceManager();
                if (vm != null)
                    vmStopClip.Invoke(vm, new object[] { currentVmClip });
            }
            catch { }

            currentVmClip = null;
        }

        private static bool TryPushToSeralyth(AudioClip clip, bool looping)
        {
            object vm = GetSeralythVoiceManager();
            if (vm == null || clip == null)
                return false;

            try
            {
                StopVmClip();
                object vmClip = vmAudioClip.Invoke(vm, new object[] { clip, true });
                if (vmClip == null)
                    return false;

                System.Type ct = vmClip.GetType();
                ct.GetProperty("Looping").SetValue(vmClip, looping, null);
                ct.GetProperty("Volume").SetValue(vmClip, 1f, null);

                currentVmClip = vmClip;
                return true;
            }
            catch { return false; }
        }

        private static void SetRecorderClip(AudioClip clip, bool looping = false)
        {
            if (TryPushToSeralyth(clip, looping))
                return;

            if (GorillaTagger.Instance.myRecorder == null)
                return;

            GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.AudioClip;
            GorillaTagger.Instance.myRecorder.AudioClip = clip;
            GorillaTagger.Instance.myRecorder.RestartRecording(true);
        }

        private static IEnumerator PlayIntroThenLoop(string[] introSequence, string loopSound, bool looping)
        {
            foreach (string introName in introSequence)
            {
                AudioClip intro = LoadSoundFromResource(introName);
                if (intro == null)
                    continue;

                SetRecorderClip(intro);
                Play2DAudio(intro, 0.5f);
                yield return new WaitForSeconds(intro.length);
            }

            AudioClip loop = LoadSoundFromResource(loopSound);
            if (loop != null)
            {
                SetRecorderClip(loop, looping);
                Play2DAudio(loop, 0.5f, looping);
            }

            introRoutine = null;
        }

        private static void CancelIntro()
        {
            StopVmClip();

            if (introAnimRoutine != null)
            {
                Instance.StopCoroutine(introAnimRoutine);
                introAnimRoutine = null;
            }

            if (introRoutine == null)
                return;

            Instance.StopCoroutine(introRoutine);
            introRoutine = null;

            if (audiomgr != null)
                audiomgr.GetComponent<AudioSource>().Stop();
        }

        private static readonly List<GameObject> portedCosmetics = new List<GameObject> { };
        public static void DisableCosmetics()
        {
            try
            {
                VRRig.LocalRig.transform.Find("rig/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("Default");
                foreach (GameObject Cosmetic in VRRig.LocalRig.cosmetics)
                {
                    if (Cosmetic.activeSelf && Cosmetic.transform.parent == VRRig.LocalRig.mainCamera.transform.Find("HeadCosmetics"))
                    {
                        portedCosmetics.Add(Cosmetic);
                        Cosmetic.transform.SetParent(VRRig.LocalRig.headMesh.transform, false);
                        Cosmetic.transform.localPosition += new Vector3(0f, 0.1333f, 0.1f);
                    }
                }
            }
            catch { }
        }

        public static void EnableCosmetics()
        {
            VRRig.LocalRig.transform.Find("rig/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("MirrorOnly");
            foreach (GameObject Cosmetic in portedCosmetics)
            {
                Cosmetic.transform.SetParent(VRRig.LocalRig.mainCamera.transform.Find("HeadCosmetics"), false);
                Cosmetic.transform.localPosition -= new Vector3(0f, 0.1333f, 0.1f);
            }
            portedCosmetics.Clear();
        }

        public static GameObject Kyle;
        public static float emoteTime;
        public static string currentEmoteName = "";
        public static Vector3 archivePosition;

        private const string EmotePropKey = "FEW_Emote";
        private const string EmoteStartPropKey = "FEW_EmoteStart";
        private const float SyncRadius = 10f;

        private static void BroadcastEmote(string emoteName, double startServerTime)
        {
            try
            {
                if (!PhotonNetwork.InRoom)
                    return;

                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
                {
                    { EmotePropKey, emoteName },
                    { EmoteStartPropKey, startServerTime }
                });
            }
            catch { }
        }

        private static void ClearBroadcastEmote()
        {
            try
            {
                if (!PhotonNetwork.InRoom)
                    return;

                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
                {
                    { EmotePropKey, null },
                    { EmoteStartPropKey, null }
                });
            }
            catch { }
        }

        private static bool TryFindNearbySameEmote(string emoteName, out double theirStartTime)
        {
            theirStartTime = 0;

            try
            {
                if (!PhotonNetwork.InRoom || VRRig.LocalRig == null)
                    return false;

                Vector3 myPosition = VRRig.LocalRig.transform.position;
                float bestDistance = SyncRadius;
                bool found = false;

                foreach (VRRig rig in UnityEngine.Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
                {
                    if (rig == null || rig == VRRig.LocalRig || rig.Creator == null)
                        continue;

                    float distance = Vector3.Distance(myPosition, rig.transform.position);
                    if (distance > bestDistance)
                        continue;

                    Photon.Realtime.Player photonPlayer = rig.Creator.GetPlayerRef();
                    if (photonPlayer == null)
                        continue;

                    object nameValue;
                    object startValue;
                    if (!photonPlayer.CustomProperties.TryGetValue(EmotePropKey, out nameValue) ||
                        !photonPlayer.CustomProperties.TryGetValue(EmoteStartPropKey, out startValue))
                        continue;

                    if ((nameValue as string) != emoteName || !(startValue is double))
                        continue;

                    bestDistance = distance;
                    theirStartTime = (double)startValue;
                    found = true;
                }

                return found;
            }
            catch { return false; }
        }

        private static float ToNormalizedTime(float seconds, float clipLength, bool looping)
        {
            if (clipLength <= 0f)
                return 0f;

            float t = seconds / clipLength;
            return looping ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t);
        }

        public static void Emote(string emoteName, string emoteSound, float animationTime = -1f, bool looping = false, string[] introAudioSequence = null, string introAnimation = null)
        {
            currentEmoteName = emoteName;

            CancelIntro();

            if (Kyle != null)
                Destroy(Kyle);

            if (RigUtils.Instance.IsRigEnabled)
                RigUtils.Instance.ToggleRig(false);
            DisableCosmetics();

            double theirStartTime;
            bool synced = TryFindNearbySameEmote(emoteName, out theirStartTime);

            if (!synced)
                Play2DAudio(LoadSoundFromResource("play"), 0.5f);

            archivePosition = GorillaTagger.Instance.transform.position;
            GorillaLocomotion.GTPlayer.Instance.GetControllerTransform(false).parent.rotation *= Quaternion.Euler(0f, 180f, 0f);

            Kyle = LoadAsset("Rig");
            Kyle.transform.position = VRRig.LocalRig.transform.Find("rig/body_pivot").position - new Vector3(0f, 1.15f, 0f);
            Kyle.transform.rotation = VRRig.LocalRig.transform.Find("rig/body_pivot").rotation;

            Kyle.transform.Find("KyleRobot/RobotKile").gameObject.GetComponent<Renderer>().renderingLayerMask = 0;

            Animator KyleRobot = Kyle.transform.Find("KyleRobot").GetComponent<Animator>();
            KyleRobot.enabled = true;

            AnimationClip Animation = null;
            foreach (AnimationClip Clip in KyleRobot.runtimeAnimatorController.animationClips)
            {
                if (Clip.name == emoteName)
                {
                    Animation = Clip;
                    break;
                }
            }

            if (Animation == null)
            {
                emoteTime = 0f;
                return;
            }

            Animation.wrapMode = looping ? WrapMode.Loop : WrapMode.Default;

            AnimationClip introAnim = null;
            if (!string.IsNullOrEmpty(introAnimation))
            {
                introAnim = FindClip(KyleRobot, introAnimation);
            }

            float introLength = introAnim != null ? introAnim.length : 0f;
            float totalLength = animationTime > 0f ? animationTime : Animation.length + introLength;

            double emoteStartServerTime = PhotonNetwork.InRoom ? PhotonNetwork.Time : 0.0;
            float elapsed = 0f;

            if (synced)
            {
                elapsed = (float)(PhotonNetwork.Time - theirStartTime);

                if (elapsed < 0f || (!looping && elapsed >= totalLength))
                {
                    synced = false;
                    elapsed = 0f;
                    Play2DAudio(LoadSoundFromResource("play"), 0.5f);
                }
                else
                {
                    emoteStartServerTime = theirStartTime;
                }
            }

            if (introAnim != null)
            {
                if (synced && elapsed >= introAnim.length)
                {
                    KyleRobot.Play(Animation.name, 0, ToNormalizedTime(elapsed - introAnim.length, Animation.length, looping));
                }
                else
                {
                    float introElapsed = synced ? elapsed : 0f;
                    KyleRobot.Play(introAnim.name, 0, ToNormalizedTime(introElapsed, introAnim.length, false));
                    introAnimRoutine = Instance.StartCoroutine(PlayLoopAnimAfter(KyleRobot, Animation.name, introAnim.length - introElapsed));
                }
            }
            else
            {
                KyleRobot.Play(Animation.name, 0, ToNormalizedTime(elapsed, Animation.length, looping));
            }

            if (!synced)
            {
                if (introAudioSequence != null && introAudioSequence.Length > 0)
                {
                    introRoutine = Instance.StartCoroutine(PlayIntroThenLoop(introAudioSequence, emoteSound, looping));
                }
                else
                {
                    AudioClip Sound = LoadSoundFromResource(emoteSound);
                    Play2DAudio(Sound, 0.5f, looping);
                    SetRecorderClip(Sound, looping);
                }
            }

            emoteTime = Time.time + Mathf.Max(0.05f, totalLength - elapsed) + (looping ? 999999999999999f : 0);

            BroadcastEmote(emoteName, emoteStartServerTime);
        }

        public static Vector3 World2Player(Vector3 world) => world - GorillaTagger.Instance.bodyCollider.transform.position + GorillaTagger.Instance.transform.position;

        public static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static float GetFingerCurl(Transform fingerBone, bool isThumb = false)
        {
            float angle = isThumb ? fingerBone.localEulerAngles.y : fingerBone.localEulerAngles.z;

            if (angle > 180f)
                angle -= 360f;

            return Mathf.InverseLerp(0f, 90f, Mathf.Abs(angle));
        }

        private const string TagEmoteName = "California Gurls";

        public void Update()
        {
            UpdateWarningTick();
            GreetingTick();

            if (GorillaLocomotion.GTPlayer.Instance == null)
                return;

            if (Classes.Wheel.instance == null && VRRig.LocalRig != null)
            {
                GameObject Wheel = Plugin.LoadAsset("Wheel");
                Wheel.transform.SetParent(VRRig.LocalRig.transform.Find("rig/hand.R"), false);
                SetLayerRecursively(Wheel, VRRig.LocalRig.gameObject.layer);
                Wheel.AddComponent<Classes.Wheel>();
            }

            if (Time.time < emoteTime)
            {
                if (Kyle != null)
                {
                    GorillaTagger.Instance.transform.position = World2Player(Kyle.transform.position + (Kyle.transform.forward * 1.5f) + new Vector3(0f, 1.15f, 0f)) + new Vector3(0f, 0.5f, 0f);
                    GorillaTagger.Instance.leftHandTransform.position = GorillaTagger.Instance.bodyCollider.transform.position;
                    GorillaTagger.Instance.rightHandTransform.position = GorillaTagger.Instance.bodyCollider.transform.position;

                    GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;

                    Kyle.transform.localScale = VRRig.LocalRig.transform.localScale;
                    float scale = VRRig.LocalRig.transform.localScale.x;
                    float handYOffset = (1f - scale) * 0.25f;

                    Transform spine2 = Kyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2");
                    Transform leftHandBone = spine2.Find("LeftShoulder/LeftUpperArm/LeftArm/LeftHand");
                    Transform rightHandBone = spine2.Find("RightShoulder/RightUpperArm/RightArm/RightHand");

                    Vector3 basePosition = spine2.position - (spine2.right / 2.5f);
                    Quaternion targetRotation = spine2.rotation * Quaternion.Euler(0f, 0f, 90f);

                    // Body Tracking constraint: if disabled, constrain rotation to only rotate left and right (yaw axis only)
                    if (!EnableBodyTracking.Value)
                    {
                        Vector3 euler = targetRotation.eulerAngles;
                        targetRotation = Quaternion.Euler(0f, euler.y, 0f);
                    }

                    RigUtils.Instance.RigPosition = basePosition;
                    RigUtils.Instance.RigRotation = targetRotation;

                    VRRig.LocalRig.transform.position = basePosition;
                    VRRig.LocalRig.transform.rotation = targetRotation;

                    VRRig.LocalRig.leftHand.rigTarget.transform.position = leftHandBone.position + Vector3.up * handYOffset;
                    VRRig.LocalRig.rightHand.rigTarget.transform.position = rightHandBone.position + Vector3.up * handYOffset;

                    VRRig.LocalRig.leftHand.rigTarget.transform.rotation = leftHandBone.rotation * Quaternion.Euler(0, 0, 75);
                    VRRig.LocalRig.rightHand.rigTarget.transform.rotation = rightHandBone.rotation * Quaternion.Euler(180, 0, -75);

                    if (currentEmoteName != TagEmoteName)
                    {
                        if (EnableElbowTracking.Value)
                        {
                            ElbowIK.Set(
                                leftHandBone.parent, rightHandBone.parent,
                                leftHandBone.parent.parent, rightHandBone.parent.parent,
                                leftHandBone.parent.parent.parent, rightHandBone.parent.parent.parent,
                                spine2);
                        }
                        else
                        {
                            ElbowIK.Clear();
                        }
                    }
                    else
                    {
                        ElbowIK.Clear();
                    }

                    VRRig.LocalRig.head.rigTarget.transform.rotation = spine2.Find("Neck/Head").rotation * Quaternion.Euler(0f, 0f, 90f);

                    // Finger Tracking toggle
                    if (EnableFingerTracking.Value)
                    {
                        float rightIndexCurl = GetFingerCurl(rightHandBone.GetChild(0).GetChild(0));
                        VRRig.LocalRig.rightIndex.calcT = rightIndexCurl;
                        VRRig.LocalRig.rightIndex.LerpFinger(1f, false);

                        float leftIndexCurl = GetFingerCurl(leftHandBone.GetChild(0).GetChild(0));
                        VRRig.LocalRig.leftIndex.calcT = leftIndexCurl;
                        VRRig.LocalRig.leftIndex.LerpFinger(1f, false);

                        float rightPinkyCurl = GetFingerCurl(rightHandBone.GetChild(2).GetChild(0));
                        VRRig.LocalRig.rightMiddle.calcT = rightPinkyCurl;
                        VRRig.LocalRig.rightMiddle.LerpFinger(1f, false);

                        float leftPinkyCurl = GetFingerCurl(leftHandBone.GetChild(2).GetChild(0));
                        VRRig.LocalRig.leftMiddle.calcT = leftPinkyCurl;
                        VRRig.LocalRig.leftMiddle.LerpFinger(1f, false);

                        float rightThumbCurl = GetFingerCurl(rightHandBone.GetChild(4).GetChild(0).GetChild(0), true);
                        VRRig.LocalRig.rightThumb.calcT = rightThumbCurl;
                        VRRig.LocalRig.rightThumb.LerpFinger(1f, false);

                        float leftThumbCurl = GetFingerCurl(leftHandBone.GetChild(4).GetChild(0).GetChild(0), true);
                        VRRig.LocalRig.leftThumb.calcT = leftThumbCurl;
                        VRRig.LocalRig.leftThumb.LerpFinger(1f, false);
                    }
                }
            }
            else
            {
                if (Kyle != null)
                {
                    CancelIntro();
                    ClearBroadcastEmote();
                    ElbowIK.Clear();

                    if (!RigUtils.Instance.IsRigEnabled)
                        RigUtils.Instance.ToggleRig(true);
                    EnableCosmetics();

                    Destroy(Kyle);

                    if (GetSeralythVoiceManager() == null && GorillaTagger.Instance.myRecorder != null)
                    {
                        GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.Microphone;
                        GorillaTagger.Instance.myRecorder.AudioClip = null;
                        GorillaTagger.Instance.myRecorder.RestartRecording(true);
                    }

                    GorillaTagger.Instance.transform.position = archivePosition;
                    GorillaLocomotion.GTPlayer.Instance.GetControllerTransform(false).parent.rotation *= Quaternion.Euler(0f, 180f, 0f);
                }
            }
        }
    }
}