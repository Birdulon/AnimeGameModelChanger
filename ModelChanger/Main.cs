using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnhollowerRuntimeLib;
using UnityEngine;
using miHoYoEmotion;

namespace ModelChanger
{
    public enum ModelSource
    {
        None,
        Unknown,
        Avatar,
        NPC,
        Monster
    }

    public class Model
    {
        public GameObject avatar;
        public GameObject body;
        public GameObject modelParent;
        public Animator animator;

        public GameObject weaponRoot;
        public GameObject weaponL;
        public GameObject weaponR;
        public GameObject headBone;
        public GameObject eyeR;
        public GameObject eyeL;
        public GameObject toothD;
        public GameObject toothU;
        public GameObject glider;
        public GameObject gliderRoot;  // Used for retexturing
        public List<GameObject> bodyParts = new();
    }

    public class Main : MonoBehaviour
    {
        public Main(IntPtr ptr) : base(ptr)
        {
            _gliderTexIndex = 0;
        }

        public Main() : base(ClassInjector.DerivedConstructorPointer<Main>())
        {
            _gliderTexIndex = 0;
            ClassInjector.DerivedConstructorBody(this);
        }

        #region Properties

        private Model _clipboard;
        private Model _activeAvatar;
        private GameObject avatarRoot;
        private GameObject npcRoot;
        private GameObject monsterRoot;
        private GameObject _npcBodyParent;
        public static GameObject EntityBip;
        private List<GameObject> _searchResults = new();
        private string _avatarSearch;
        private ModelSource _npcType = ModelSource.None;
        private string[] _files;
        private string _filePath = Path.Combine(Application.dataPath, "tex_test");
        private bool _showMainPanel;
        private bool _showAvatarPanel;
        private bool _showGliderPanel;
        private int _avatarTexIndex;
        private int _gliderTexIndex;

        private Rect _mainRect = new(200, 250, 150, 100);
        private Rect _avatarRect = new(370, 250, 200, 100);
        private Rect _gliderRect = new(590, 250, 200, 100);
        private GUILayoutOption[] _buttonSize;

        #endregion

        public void OnGUI()
        {
            if (!_showMainPanel) return;
            _mainRect = GUILayout.Window(4, _mainRect, (GUI.WindowFunction) TexWindow, "Model Changer",
                new GUILayoutOption[0]);
            if (_showAvatarPanel)
                _avatarRect = GUILayout.Window(5, _avatarRect, (GUI.WindowFunction) TexWindow, "Character Texture",
                    new GUILayoutOption[0]);
            if (_showGliderPanel)
                _gliderRect = GUILayout.Window(6, _gliderRect, (GUI.WindowFunction) TexWindow, "Glider Texture",
                    new GUILayoutOption[0]);
        }

        public void TexWindow(int id)
        {
            _buttonSize = new[]
            {
                GUILayout.Width(45),
                GUILayout.Height(20)
            };
            switch (id)
            {
                case 4:
                {
                    GUILayout.Label("Texture", new GUILayoutOption[0]);
                    if (GUILayout.Button("Character Texture", new GUILayoutOption[0]))
                        _showAvatarPanel = !_showAvatarPanel;
                    if (GUILayout.Button("Glider Texture", new GUILayoutOption[0]))
                        _showGliderPanel = !_showGliderPanel;
                    GUILayout.Space(10);
                    GUILayout.Label("Model", new GUILayoutOption[0]);
                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    if (GUILayout.Button("Cut", new GUILayoutOption[0]))
                        CutAvatarBody();
                    if (GUILayout.Button("Paste", new GUILayoutOption[0]))
                        PasteAvatarBody();
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Search", new GUILayoutOption[0]);
                    _avatarSearch = GUILayout.TextField(_avatarSearch, new GUILayoutOption[0]);
                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    if (GUILayout.Button("Search", new GUILayoutOption[0]))
                        SearchObjects();
                    if (GUILayout.Button("Clear", new GUILayoutOption[0]))
                    {
                        _searchResults.Clear();
                        _avatarSearch = "";
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);

                    if (_searchResults.Count > 0)
                    {
                        foreach (var result in _searchResults)
                        {
                            if (!GUILayout.Button($"{result.transform.name}", new GUILayoutOption[0])) continue;
                            NpcAvatarChanger(result.gameObject);
                        }
                    }

                    break;
                }
                case 5:
                    if (GUILayout.Button("Scan", new GUILayoutOption[0]))
                        _files = Directory.GetFiles(_filePath);
                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    if (GUILayout.Button("-", _buttonSize))
                        _avatarTexIndex -= 1;
                    if (GUILayout.Button("+", _buttonSize))
                        _avatarTexIndex += 1;
                    GUILayout.Label($"Array Index: {_avatarTexIndex}", new GUILayoutOption[0]);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    if (_files is not null)
                    {
                        foreach (var file in _files)
                        {
                            if (GUILayout.Button($"{Path.GetFileName(file)}", new GUILayoutOption[0]))
                                ApplyAvatarTexture(file);
                        }
                    }

                    break;
                case 6:
                    if (GUILayout.Button("Scan", new GUILayoutOption[0]))
                        _files = Directory.GetFiles(_filePath);
                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    if (GUILayout.Button("-", _buttonSize))
                        _gliderTexIndex -= 1;
                    if (GUILayout.Button("+", _buttonSize))
                        _gliderTexIndex += 1;
                    GUILayout.Label($"Array Index: {_gliderTexIndex}", new GUILayoutOption[0]);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    if (_files is not null)
                    {
                        foreach (var file in _files)
                        {
                            if (GUILayout.Button($"{Path.GetFileName(file)}", new GUILayoutOption[0]))
                                ApplyGliderTexture(file);
                        }
                    }

                    break;
            }

            GUI.DragWindow();
        }

        private bool UpdateRoots()
        {
            this.avatarRoot = GameObject.Find("/EntityRoot/AvatarRoot");
            this.npcRoot = GameObject.Find("/EntityRoot/NPCRoot");
            this.monsterRoot = GameObject.Find("/EntityRoot/MonsterRoot");
            return ((this.avatarRoot != null) && (this.npcRoot != null) && (this.monsterRoot != null));
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12)){
                _showMainPanel = !_showMainPanel;
                Loader.Msg("Toggling main panel.");}
            if (_showMainPanel)
                Focused = false;
            if (!UpdateRoots()) return;
            if (!FindActiveAvatar()) return;
            if (!_activeAvatar.avatar.activeInHierarchy) FindActiveAvatar();

            if (_activeAvatar.animator is not null)
                _activeAvatar.animator.isAnimationPaused = _showMainPanel;

            _searchResults = _searchResults.Where(item => item is not null).ToList();
            if ((this._npcBodyParent is not null) && (_activeAvatar is not null))
            {
                this._npcBodyParent.transform.position = this._npcBodyParent.transform.parent.transform.position;
            }
        }

        #region MainFunctions

        private void CutAvatarBody()
        {
            Model source = _activeAvatar;
            Model clipboard = new(){modelParent = source.modelParent};
            foreach (var o in source.modelParent.transform)
            {
                var bodypart = o.Cast<Transform>();
                clipboard.bodyParts.Add(bodypart.gameObject);
                Loader.Msg($"Added {bodypart.name} of {source.modelParent.transform.name} to the list.");
            }

            foreach (var o in source.modelParent.GetComponentsInChildren<Transform>())
            {
                // Loader.Msg($"Found {o.gameObject.name}.");
                Loader.Msg($"Found {o.name}.");
                switch (o.name)
                {
                    case "Bip001":
                        EntityBip = o.gameObject;
                        break;
                    case "Bip001 Spine1":
                        clipboard.weaponRoot = o.gameObject;
                        break;
                    case "Bip001 L Hand":
                        clipboard.weaponL = o.gameObject;
                        break;
                    case "Bip001 R Hand":
                        clipboard.weaponR = o.gameObject;
                        break;
                    case "Bip001 Head":
                        clipboard.headBone = o.gameObject;
                        break;
                    case "Bip001 Spine2":
                        clipboard.glider = o.gameObject;
                        break;
                }
            }
            _clipboard = clipboard;
        }

        private void PasteAvatarBody()
        {
            Model source = _clipboard;
            Model dest = _activeAvatar;
            dest.animator = dest.modelParent.GetComponent<Animator>();
            source.animator = source.modelParent.GetComponent<Animator>();

            DestroyModelChildren(_activeAvatar.modelParent);

            foreach (var part in source.bodyParts)
            {
                if (part.name != "Bip001") continue;
                foreach (var bone in part.GetComponentsInChildren<Transform>())
                {
                    switch (bone.name)
                    {
                        case "WeaponL":
                        case "WeaponR":
                            Destroy(bone.gameObject);
                            break;
                        case "+EyeBone L A01":
                        case "+EyeBone R A01":
                        case "+ToothBone D A01":
                        case "+ToothBone U A01":
                            bone.gameObject.SetActive(false);
                            break;
                    }

                    if (bone.name.Contains("WeaponRoot"))
                    {
                        Destroy(bone.gameObject);
                    }
                }
            }
            foreach (var part in source.bodyParts)
            {
                part.transform.parent = dest.modelParent.transform;
                part.transform.parentInternal = dest.modelParent.transform;
                part.transform.SetSiblingIndex(0);
                Loader.Msg($"{part.name} moved to {dest.modelParent.name}");
            }

            dest.animator.avatar = source.animator.avatar;
            source.animator.avatar = null;

            dest.weaponRoot.transform.parent = source.weaponRoot.transform;
            dest.weaponL.transform.parent = source.weaponL.transform;
            dest.weaponR.transform.parent = source.weaponR.transform;
            dest.weaponRoot.transform.SetSiblingIndex(0);
            dest.weaponL.transform.SetSiblingIndex(0);
            dest.weaponR.transform.SetSiblingIndex(0);

            SetClip(source.modelParent, dest.modelParent);
            SetEyeKey(source.modelParent, dest.modelParent);

            dest.eyeL.transform.parent = source.headBone.transform;
            dest.eyeR.transform.parent = source.headBone.transform;
            dest.toothD.transform.parent = source.headBone.transform;
            dest.toothU.transform.parent = source.headBone.transform;
            dest.glider.transform.parent = source.glider.transform;
            dest.eyeL.transform.SetSiblingIndex(0);
            dest.eyeR.transform.SetSiblingIndex(0);
            dest.toothD.transform.SetSiblingIndex(0);
            dest.toothU.transform.SetSiblingIndex(0);
            dest.glider.transform.SetSiblingIndex(0);

            dest.avatar.SetActive(false);
            dest.avatar.SetActive(true);
            dest.body.SetActive(false);
        }

        private void SearchObjects()
        {
            _searchResults = GetTransformChildren(this.monsterRoot);
            _searchResults.AddRange(GetTransformChildren(this.npcRoot));
        }

        private List<GameObject> GetTransformChildren(GameObject parent)
        {
            List<GameObject> output = new();
            if ((parent) && (parent.transform.childCount > 0))
            {
                foreach (var a in parent.transform)
                {
                    Transform obj = a.Cast<Transform>();
                    if (obj.name.Contains(_avatarSearch, StringComparison.OrdinalIgnoreCase))
                        output.Add(obj.gameObject);
                }
            }
            return output;
        }

        private void NpcAvatarChanger(GameObject searchResult)
        {
            GameObject _npcAvatarModelParent = null;
            foreach (var a in searchResult.GetComponentsInChildren<Transform>())
            {
                if (a.name == "OffsetDummy")
                {
                    _npcAvatarModelParent = a.GetChild(0).gameObject;
                    Loader.Msg($"{_npcAvatarModelParent.transform.name}");
                }
                if (a.name.Contains("Body"))
                {
                    _npcAvatarModelParent = a.gameObject.transform.parent.gameObject;
                    Loader.Msg($"{_npcAvatarModelParent.transform.name}");
                }
            }
            if (_npcAvatarModelParent is null) return;

            Model source = new();
            Model dest = _activeAvatar;

            foreach (var o in _npcAvatarModelParent.transform)
            {
                var npcBodypart = o.Cast<Transform>();
                source.bodyParts.Add(npcBodypart.gameObject);
                Loader.Msg($"Added {npcBodypart.name} of {_npcAvatarModelParent.transform.name} to the list.");
            }

            var activeAvatarAnimator = dest.modelParent.GetComponent<Animator>();
            source.animator = _npcAvatarModelParent.GetComponent<Animator>();
            Loader.Msg($"Animator_Load in {_npcAvatarModelParent}.");

            source.modelParent = _npcAvatarModelParent.gameObject;
            while (source.modelParent.transform.parent.transform.parent.gameObject.name != "EntityRoot")
            {
                source.modelParent = source.modelParent.transform.parent.gameObject;
            }
            
            var activeCharacterBodyParent = dest.body.gameObject;
            while (activeCharacterBodyParent.transform.parent.transform.parent.gameObject.name != "EntityRoot")
            {
                activeCharacterBodyParent = activeCharacterBodyParent.transform.parent.gameObject;
            }

            Loader.Msg($"{source.modelParent.name}");
            Loader.Msg($"{activeCharacterBodyParent.name}");
            _npcType = source.modelParent.transform.parent.gameObject.name switch
            {
                "AvatarRoot" => ModelSource.Avatar,
                "MonsterRoot" => ModelSource.Monster,
                "NPCRoot" => ModelSource.NPC,
                _ => ModelSource.Unknown
            };

            foreach (var o in _npcAvatarModelParent.GetComponentsInChildren<Transform>())
            {
                switch (o.name)
                {
                    case "Bip001":
                        EntityBip = o.gameObject;
                        break;
                    case "Bip001 Spine1":
                        source.weaponRoot = o.gameObject;
                        Loader.Msg($"Found {source.weaponRoot.name}.");
                        break;
                    case "Bip001 L Hand":
                        source.weaponL = o.gameObject;
                        Loader.Msg($"Found {source.weaponL.name}");
                        break;
                    case "Bip001 R Hand":
                        source.weaponR = o.gameObject;
                        Loader.Msg($"Found {source.weaponR.name}");
                        break;
                    case "WeaponL":
                    case "WeaponR":
                        o.gameObject.SetActive(false);
                        break;
                }

                if (o.name.Contains("WeaponRoot"))
                {
                    o.gameObject.SetActive(false);
                }
            }

            DestroyModelChildren(dest.modelParent);
            
            foreach (var a in dest.modelParent.GetComponentsInChildren<Transform>())
            {
                if (a.name == "+FlycloakRootB CB A01")
                    a.gameObject.SetActive(false);
            }

            foreach (var part in source.bodyParts)
            {
                part.transform.parent = dest.modelParent.transform;
                part.transform.parentInternal = dest.modelParent.transform;
                part.transform.SetSiblingIndex(0);
                Loader.Msg($"Moved {part.name} to {dest.modelParent.name}");
            }

            dest.weaponRoot.transform.parent = source.weaponRoot.transform;
            dest.weaponL.transform.parent = source.weaponL.transform;
            dest.weaponR.transform.parent = source.weaponR.transform;
            dest.weaponRoot.transform.SetSiblingIndex(0);
            dest.weaponL.transform.SetSiblingIndex(0);
            dest.weaponR.transform.SetSiblingIndex(0);

            if (_npcType == ModelSource.Monster)
            {
                _npcAvatarModelParent.GetComponent<Behaviour>().enabled = false;
                source.modelParent.GetComponent<Rigidbody>().collisionDetectionMode =
                    CollisionDetectionMode.ContinuousDynamic;
                source.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                activeAvatarAnimator.avatar = source.animator.avatar;
                source.animator.avatar = null;
                source.animator.runtimeAnimatorController = null;
                source.animator.runtimeAnimatorController = activeAvatarAnimator.runtimeAnimatorController;
                source.modelParent.transform.Find("Collider").gameObject.SetActive(false);
                source.modelParent.transform.parent = dest.modelParent.transform.parent;
                source.modelParent.transform.parentInternal = dest.modelParent.transform.parent;
                source.animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            else if (_npcType == ModelSource.NPC)
            {
                activeAvatarAnimator.avatar = source.animator.avatar;
                source.animator.avatar = null;
                source.animator.gameObject.SetActive(false);
                source.animator.runtimeAnimatorController = activeAvatarAnimator.runtimeAnimatorController;
                searchResult.transform.position = dest.modelParent.transform.position;
                Destroy(source.modelParent.GetComponent<Rigidbody>());
                source.modelParent.transform.Find("Collider").gameObject.SetActive(false);
                source.modelParent.transform.parent = dest.modelParent.transform.parent;
                source.modelParent.transform.parentInternal = dest.modelParent.transform.parent;
                source.modelParent.SetActive(false);
                Destroy(source.modelParent);
            }
            this._npcBodyParent = source.modelParent;
            dest.body.SetActive(false);
        }

        private void ApplyAvatarTexture(string filePath)
        {
            if (_activeAvatar.body is null) return;
            var tex = LoadTexture(filePath);
            _activeAvatar.body.GetComponent<SkinnedMeshRenderer>().materials[_avatarTexIndex].mainTexture = tex;
        }

        private void ApplyGliderTexture(string filePath)
        {
            if (_activeAvatar.gliderRoot is null) return;
            var glider = _activeAvatar.gliderRoot.transform.GetChild(0).gameObject;
            Loader.Msg($"Found {glider.name}");
            var tex = LoadTexture(filePath);

            glider.SetActive(true);
            foreach (var renderer in glider.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.materials[_gliderTexIndex].mainTexture = tex;
            }
            glider.SetActive(false);
        }

        #endregion

        #region HelperFunctions

        private static Texture2D LoadTexture(string filePath)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D tex = new(1024, 1024);
            ImageConversion.LoadImage(tex, fileData);
            return tex;
        }

        private static void DestroyModelChildren(GameObject model)
        {
            foreach (var a in model.transform)
            {
                Transform bodypart = a.Cast<Transform>();
                switch (bodypart.name)
                {
                    case "Brow":
                    case "Face":
                    case "Face_Eye":
                        Destroy(bodypart.gameObject);
                        Loader.Msg($"Destroyed {bodypart.name}");
                        break;
                    case "Bip001":
                        bodypart.gameObject.AddComponent<RotationController>();
                        break;
                    default:
                        bodypart.gameObject.SetActive(false);
                        break;
                }
            }
        }

        private static void SetClip(GameObject origin, GameObject target)
        {
            var originClip = origin.GetComponent<ClipShapeManager>();
            var targetClip = target.GetComponent<ClipShapeManager>();
            targetClip.currModelBindingList = originClip.currModelBindingList;
            targetClip.faceMaterial = originClip.faceMaterial;
        }

        private static void SetEyeKey(GameObject origin, GameObject target)
        {
            var originEyeKey = origin.GetComponent<EyeKey>();
            var targetEyeKey = target.GetComponent<EyeKey>();
            targetEyeKey._leftEyeBoneHash = originEyeKey._leftEyeBoneHash;
            targetEyeKey._leftEyeBallScaleTarget = originEyeKey._leftEyeBallScaleTarget;
            targetEyeKey._leftEyeBoneHash = originEyeKey._leftEyeBoneHash;
            targetEyeKey._leftEyeRotTarget = originEyeKey._leftEyeRotTarget;
            targetEyeKey._originDownTeethPos = originEyeKey._originDownTeethPos;
            targetEyeKey._originDownTeethRot = originEyeKey._originDownTeethRot;
            targetEyeKey._originDownTeethScale = originEyeKey._originDownTeethScale;
            targetEyeKey._originLeftEyeBallRot = originEyeKey._originLeftEyeBallRot;
            targetEyeKey._originLeftEyeBallScale = originEyeKey._originLeftEyeBallScale;
            targetEyeKey._originLeftEyeRot = originEyeKey._originLeftEyeRot;
            targetEyeKey._originLeftEyeScale = originEyeKey._originLeftEyeScale;
            targetEyeKey._originRightEyeBallRot = originEyeKey._originRightEyeBallRot;
            targetEyeKey._originRightEyeBallScale = originEyeKey._originRightEyeBallScale;
            targetEyeKey._originRightEyeRot = originEyeKey._originRightEyeRot;
            targetEyeKey._originRightEyeScale = originEyeKey._originRightEyeScale;
            targetEyeKey._originUpTeethRot = originEyeKey._originUpTeethRot;
            targetEyeKey._originUpTeethScale = originEyeKey._originUpTeethScale;
            targetEyeKey._rightEyeBallBoneHash = originEyeKey._rightEyeBallBoneHash;
            targetEyeKey._rightEyeBallScaleTarget = originEyeKey._rightEyeBallScaleTarget;
            targetEyeKey._rightEyeBoneHash = originEyeKey._rightEyeBoneHash;
            targetEyeKey._rightEyeRotTarget = originEyeKey._rightEyeRotTarget;
            targetEyeKey._rotDuration = originEyeKey._rotDuration;
            targetEyeKey._rotTargetCurrtime = originEyeKey._rotTargetCurrtime;
            targetEyeKey._scaleDuration = originEyeKey._scaleDuration;
            targetEyeKey._scaleTargetCurrtime = originEyeKey._scaleTargetCurrtime;
            targetEyeKey._teethDownHash = originEyeKey._teethDownHash;
            targetEyeKey._teethUpHash = originEyeKey._teethUpHash;
            targetEyeKey.currentController = originEyeKey.currentController;
            targetEyeKey.leftEyeBallBone = originEyeKey.leftEyeBallBone;
            targetEyeKey.leftEyeBallRot = originEyeKey.leftEyeBallRot;
            targetEyeKey.leftEyeBallScale = originEyeKey.leftEyeBallScale;
            targetEyeKey.leftEyeBone = originEyeKey.leftEyeBone;
            targetEyeKey.leftEyeRot = originEyeKey.leftEyeRot;
            targetEyeKey.leftEyeScale = originEyeKey.leftEyeScale;
            targetEyeKey.rightEyeBallBone = originEyeKey.rightEyeBallBone;
            targetEyeKey.rightEyeBallRot = originEyeKey.rightEyeBallRot;
            targetEyeKey.rightEyeBallScale = originEyeKey.rightEyeBallScale;
            targetEyeKey.rightEyeBone = originEyeKey.rightEyeBone;
            targetEyeKey.rightEyeRot = originEyeKey.rightEyeRot;
            targetEyeKey.rightEyeScale = originEyeKey.rightEyeScale;
            targetEyeKey.teethDownBone = originEyeKey.teethDownBone;
            targetEyeKey.teethDownPos = originEyeKey.teethDownPos;
            targetEyeKey.teethDownRot = originEyeKey.teethDownRot;
            targetEyeKey.teethDownScale = originEyeKey.teethDownScale;
            targetEyeKey.teethUpBone = originEyeKey.teethUpBone;
            targetEyeKey.teethUpRot = originEyeKey.teethUpRot;
            targetEyeKey.teethUpScale = originEyeKey.teethUpScale;
        }

        private bool FindActiveAvatar()
        {
            this._activeAvatar = null;
            if (this.avatarRoot.transform.childCount == 0) return false;
            foreach (var a in this.avatarRoot.transform)
            {
                var active = a.Cast<Transform>();
                if (!active.gameObject.activeInHierarchy) continue;
                this._activeAvatar = FromAvatar(active.gameObject);
            }
            return (this._activeAvatar is not null);
        }

        private static Model FromAvatar(GameObject avatar)
        {
            Model m = new(){avatar = avatar};
            foreach (var a in avatar.GetComponentsInChildren<Transform>())
            {
                switch (a.name)
                {
                    case "Body":
                        m.body = a.gameObject;
                        break;
                    case "OffsetDummy":
                        m.modelParent = a.GetChild(0).gameObject;
                        Loader.Msg($"{m.modelParent.transform.name}");
                        m.animator = m.modelParent.GetComponent<Animator>();
                        break;
                    case "WeaponL":
                        m.weaponL = a.gameObject;
                        Loader.Msg($"Found {m.weaponL.name}");
                        break;
                    case "WeaponR":
                        m.weaponR = a.gameObject;
                        Loader.Msg($"Found {m.weaponR.name}");
                        break;
                    case "+EyeBone L A01":
                        m.eyeL = a.gameObject;
                        Loader.Msg($"Found {m.eyeL.name}");
                        break;
                    case "+EyeBone R A01":
                        m.eyeR = a.gameObject;
                        Loader.Msg($"Found {m.eyeR.name}");
                        break;
                    case "+ToothBone D A01":
                        m.toothD = a.gameObject;
                        Loader.Msg($"Found {m.toothD.name}");
                        break;
                    case "+ToothBone U A01":
                        m.toothU = a.gameObject;
                        Loader.Msg($"Found {m.toothU.name}");
                        break;
                    case "+FlycloakRootB CB A01":
                        m.glider = a.gameObject;
                        Loader.Msg($"Found {m.glider.name}");
                        break;
                }

                if (a.name.Contains("WeaponRoot"))
                {
                    m.weaponRoot = a.gameObject;
                    Loader.Msg($"Found {m.weaponRoot.name}");
                }

                if (a.name.Contains("FlycloakRoot"))
                {
                    m.gliderRoot = a.gameObject;
                    Loader.Msg($"Found {m.gliderRoot.name}");
                }
            }
            return m;
        }

        private static bool Focused
        {
            get => Cursor.lockState == CursorLockMode.Locked;
            set
            {
                Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = value == false;
            }
        }

        #endregion
    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}