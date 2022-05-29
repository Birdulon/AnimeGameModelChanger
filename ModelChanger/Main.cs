using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnhollowerRuntimeLib;
using UnityEngine;
using miHoYoEmotion;

namespace ModelChanger
{
    public class Model
    {
        //public GameObject gliderRoot;
        //public GameObject eyeR;
        //public GameObject eyeL;
        //public GameObject toothD;
        //public GameObject toothU;
        //public GameObject npcAvatarModelParent;
        //public GameObject npcBodyParent;

        public GameObject weaponRoot;
        public GameObject weaponL;
        public GameObject weaponR;
        public GameObject headBone;
        public GameObject glider;
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
        private GameObject _avatarRoot;
        private GameObject _npcRoot;
        private GameObject _monsterRoot;
        private GameObject _activeAvatar;
        private GameObject _activeAvatarBody;
        private GameObject _activeAvatarModelParent;
        private GameObject _prevAvatarModelParent;
        private GameObject _gliderRoot;
        private GameObject _weaponRoot;
        private GameObject _weaponL;
        private GameObject _weaponR;
        private GameObject _eyeR;
        private GameObject _eyeL;
        private GameObject _toothD;
        private GameObject _toothU;
        private GameObject _glider;
        private GameObject _npcBodyParent;
        public static GameObject EntityBip;
        private List<GameObject> _searchResults = new List<GameObject>();
        private List<GameObject> _npcContainer = new List<GameObject>();
        private string _avatarSearch;
        private string _npcType;
        private string[] _files;
        private string _filePath = Path.Combine(Application.dataPath, "tex_test");
        private byte[] _fileData;
        private Texture2D _tex;
        private Animator _activeAvatarAnimator;
        private bool _showMainPanel;
        private bool _showAvatarPanel;
        private bool _showGliderPanel;
        private int _avatarTexIndex;
        private int _gliderTexIndex;
        private Vector3 _npcOffset;

        private Rect _mainRect = new Rect(200, 250, 150, 100);
        private Rect _avatarRect = new Rect(370, 250, 200, 100);
        private Rect _gliderRect = new Rect(590, 250, 200, 100);
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

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
                _showMainPanel = !_showMainPanel;

            if (_showMainPanel)
            {
                Focused = false;
                if (_activeAvatarAnimator)
                    _activeAvatarAnimator.isAnimationPaused = true;
            }
            else
            {
                if (_activeAvatarAnimator)
                    _activeAvatarAnimator.isAnimationPaused = false;
            }

            if (_avatarRoot is null)
                _avatarRoot = GameObject.Find("/EntityRoot/AvatarRoot");
            if (_npcRoot is null)
                _npcRoot = GameObject.Find("/EntityRoot/NPCRoot");
            if (_monsterRoot is null)
                _monsterRoot = GameObject.Find("/EntityRoot/MonsterRoot");

            if (!_avatarRoot) return;
            try
            {
                if (_activeAvatar is null)
                    FindActiveAvatar();
                if (!_activeAvatar.activeInHierarchy)
                    FindActiveAvatar();
            }
            catch
            {
            }

            _searchResults = _searchResults.Where(item => item is not null).ToList();
            _npcContainer = _npcContainer.Where(item => item is not null).ToList();

            if (_npcContainer is null) return;
            foreach (var entity in _npcContainer)
            {
                if (entity is null) continue;
                entity.transform.position = _activeAvatar.transform.position + new Vector3(5, 0, 0);
            }

            //Rin
            if (_npcBodyParent is not null)
            {
                GetNpcOffset();
                _npcBodyParent.transform.position = _activeAvatarBody.transform.position + _npcOffset;
            }
            //Rin
        }

        #region MainFunctions

        private void CutAvatarBody()
        {
            Model clipboard = new();

            _prevAvatarModelParent = _activeAvatarModelParent;
            foreach (var o in _activeAvatarModelParent.transform)
            {
                var bodypart = o.Cast<Transform>();
                clipboard.bodyParts.Add(bodypart.gameObject);
                Loader.Msg($"Added {bodypart.name} of {_activeAvatarModelParent.transform.name} to the list.");
            }

            foreach (var o in _activeAvatarModelParent.GetComponentsInChildren<Transform>())
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
            var activeAvatarAnimator = _activeAvatarModelParent.GetComponent<Animator>();
            Loader.Msg($"Animator_Load in {_activeAvatarModelParent}.");
            var prevAvatarAnimator = _prevAvatarModelParent.GetComponent<Animator>();
            Loader.Msg($"Animator_Load in {prevAvatarAnimator}.");

            DestroyModelTransform(_activeAvatarModelParent.transform);

            foreach (var part in source.bodyParts)
            {
                if (part.name != "Bip001") continue;
                foreach (var bone in part.GetComponentsInChildren<Transform>())
                {
                    switch (bone.name)
                    {
                        case "WeaponL":
                            Destroy(bone.gameObject);
                            break;
                        case "WeaponR":
                            Destroy(bone.gameObject);
                            break;
                        case "+EyeBone L A01":
                            bone.gameObject.SetActive(false);
                            break;
                        case "+EyeBone R A01":
                            bone.gameObject.SetActive(false);
                            break;
                        case "+ToothBone D A01":
                            bone.gameObject.SetActive(false);
                            break;
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
                part.transform.parent = _activeAvatarModelParent.transform;
                part.transform.parentInternal = _activeAvatarModelParent.transform;
                part.transform.SetSiblingIndex(0);
                Loader.Msg($"{part.name} moved to {_activeAvatarModelParent.name}");
            }

            activeAvatarAnimator.avatar = prevAvatarAnimator.avatar;
            prevAvatarAnimator.avatar = null;

            _weaponRoot.transform.parent = source.weaponRoot.transform;
            _weaponL.transform.parent = source.weaponL.transform;
            _weaponR.transform.parent = source.weaponR.transform;
            _weaponRoot.transform.SetSiblingIndex(0);
            _weaponL.transform.SetSiblingIndex(0);
            _weaponR.transform.SetSiblingIndex(0);

            SetClip(_prevAvatarModelParent, _activeAvatarModelParent);
            SetEyeKey(_prevAvatarModelParent, _activeAvatarModelParent);

            _eyeL.transform.parent = source.headBone.transform;
            _eyeR.transform.parent = source.headBone.transform;
            _toothD.transform.parent = source.headBone.transform;
            _toothU.transform.parent = source.headBone.transform;
            _glider.transform.parent = source.glider.transform;
            _eyeL.transform.SetSiblingIndex(0);
            _eyeR.transform.SetSiblingIndex(0);
            _toothD.transform.SetSiblingIndex(0);
            _toothU.transform.SetSiblingIndex(0);
            _glider.transform.SetSiblingIndex(0);

            _activeAvatar.SetActive(false);
            _activeAvatar.SetActive(true);
            _activeAvatarBody.SetActive(false);
        }

        private void SearchObjects()
        {
            _searchResults = GetTransformChildren(_monsterRoot);
            _searchResults.AddRange(GetTransformChildren(_npcRoot));
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

        //by Rin
        private void NpcAvatarChanger(GameObject searchResult)
        {
            Model source = new();
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
            if (_npcAvatarModelParent is null) {
                return;
            }
            foreach (var o in _npcAvatarModelParent.transform)
            {
                var npcBodypart = o.Cast<Transform>();
                source.bodyParts.Add(npcBodypart.gameObject);
                Loader.Msg($"Added {npcBodypart.name} of {_npcAvatarModelParent.transform.name} to the list.");
            }

            var activeAvatarAnimator = _activeAvatarModelParent.GetComponent<Animator>();
            var npcAnimator = _npcAvatarModelParent.GetComponent<Animator>();
            Loader.Msg($"Animator_Load in {_npcAvatarModelParent}.");

            var npcBodyParent = _npcAvatarModelParent.gameObject;
            while (npcBodyParent.transform.parent.transform.parent.gameObject.name != "EntityRoot")
            {
                npcBodyParent = npcBodyParent.transform.parent.gameObject;
            }
            
            var activeCharacterBodyParent = _activeAvatarBody.gameObject;
            while (activeCharacterBodyParent.transform.parent.transform.parent.gameObject.name != "EntityRoot")
            {
                activeCharacterBodyParent = activeCharacterBodyParent.transform.parent.gameObject;
            }

            Loader.Msg($"{npcBodyParent.name}");
            Loader.Msg($"{activeCharacterBodyParent.name}");
            if (npcBodyParent.transform.parent.gameObject.name == "MonsterRoot")
                _npcType = "Monster";
            else if (npcBodyParent.transform.parent.gameObject.name == "AvatarRoot")
                _npcType = "Avatar";
            else if (npcBodyParent.transform.parent.gameObject.name == "NPCRoot")
                _npcType = "Npc";
            else
                _npcType = "null";

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
                        o.gameObject.SetActive(false);
                        break;
                    case "WeaponR":
                        o.gameObject.SetActive(false);
                        break;
                }

                if (o.name.Contains("WeaponRoot"))
                {
                    o.gameObject.SetActive(false);
                }
            }

            DestroyModelTransform(_activeAvatarModelParent.transform);
            
            foreach (var a in _activeAvatarModelParent.GetComponentsInChildren<Transform>())
            {
                if (a.name == "+FlycloakRootB CB A01")
                    a.gameObject.SetActive(false);
            }

            foreach (var part in source.bodyParts)
            {
                part.transform.parent = _activeAvatarModelParent.transform;
                part.transform.parentInternal = _activeAvatarModelParent.transform;
                part.transform.SetSiblingIndex(0);
                Loader.Msg($"Moved {part.name} to {_activeAvatarModelParent.name}");
            }

            _weaponRoot.transform.parent = source.weaponRoot.transform;
            _weaponL.transform.parent = source.weaponL.transform;
            _weaponR.transform.parent = source.weaponR.transform;
            _weaponRoot.transform.SetSiblingIndex(0);
            _weaponL.transform.SetSiblingIndex(0);
            _weaponR.transform.SetSiblingIndex(0);

            if (_npcType == "Monster")
            {
                _npcAvatarModelParent.GetComponent<Behaviour>().enabled = false;
                npcBodyParent.GetComponent<Rigidbody>().collisionDetectionMode =
                    CollisionDetectionMode.ContinuousDynamic;
                npcAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                activeAvatarAnimator.avatar = npcAnimator.avatar;
                npcAnimator.avatar = null;
                npcAnimator.runtimeAnimatorController = null;
                npcAnimator.runtimeAnimatorController = activeAvatarAnimator.runtimeAnimatorController;
                npcBodyParent.transform.Find("Collider").gameObject.SetActive(false);
                npcBodyParent.transform.parent = _activeAvatarModelParent.transform.parent;
                npcBodyParent.transform.parentInternal = _activeAvatarModelParent.transform.parent;
                npcAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                _npcBodyParent = npcBodyParent;
            }

            if (_npcType == "Npc")
            {
                activeAvatarAnimator.avatar = npcAnimator.avatar;
                npcAnimator.avatar = null;
                npcAnimator.gameObject.SetActive(false);
                npcAnimator.runtimeAnimatorController = activeAvatarAnimator.runtimeAnimatorController;
                searchResult.transform.position = _activeAvatarModelParent.transform.position;
                Destroy(npcBodyParent.GetComponent<Rigidbody>());
                npcBodyParent.transform.Find("Collider").gameObject.SetActive(false);
                npcBodyParent.transform.parent = _activeAvatarModelParent.transform.parent;
                npcBodyParent.transform.parentInternal = _activeAvatarModelParent.transform.parent;
                npcBodyParent.SetActive(false);
                Destroy(npcBodyParent);
                _npcBodyParent = npcBodyParent;
            }
            
            _activeAvatarBody.SetActive(false);
        }
        //by Rin

        private void ApplyAvatarTexture(string filePath)
        {
            if (_activeAvatarBody is null) return;

            _fileData = File.ReadAllBytes(filePath);
            _tex = new Texture2D(1024, 1024);
            ImageConversion.LoadImage(_tex, _fileData);
            _activeAvatarBody.GetComponent<SkinnedMeshRenderer>().materials[_avatarTexIndex].mainTexture = _tex;
        }

        private void ApplyGliderTexture(string filePath)
        {
            if (_gliderRoot is null) return;
            var glider = _gliderRoot.transform.GetChild(0).gameObject;
            Loader.Msg($"Found {glider.name}");
            glider.SetActive(true);

            _fileData = File.ReadAllBytes(filePath);
            _tex = new Texture2D(1024, 1024);
            ImageConversion.LoadImage(_tex, _fileData);

            foreach (var renderer in glider.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.materials[_gliderTexIndex].mainTexture = _tex;
            }

            glider.SetActive(false);
        }

        #endregion

        #region HelperFunctions

        private static void DestroyModelTransform(Transform transform)
        {
            foreach (var a in transform)
            {
                Transform bodypart = a.Cast<Transform>();
                switch (bodypart.name)
                {
                    case "Brow":
                        Destroy(bodypart.gameObject);
                        Loader.Msg($"Destroyed {bodypart.name}");
                        break;
                    case "Face":
                        Destroy(bodypart.gameObject);
                        Loader.Msg($"Destroyed {bodypart.name}");
                        break;
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

        private void GetNpcOffset()
        {
            _npcOffset = _npcBodyParent.transform.parent.transform.position - _activeAvatarBody.transform.position;
        }

        private void FindActiveAvatar()
        {
            if (_avatarRoot.transform.childCount == 0) return;
            foreach (var a in _avatarRoot.transform)
            {
                var active = a.Cast<Transform>();
                if (!active.gameObject.activeInHierarchy) continue;
                _activeAvatar = active.gameObject;
                FindBody();
            }
        }

        private void FindBody()
        {
            foreach (var a in _activeAvatar.GetComponentsInChildren<Transform>())
            {
                switch (a.name)
                {
                    case "Body":
                        _activeAvatarBody = a.gameObject;
                        break;
                    case "OffsetDummy":
                        _activeAvatarModelParent = a.GetChild(0).gameObject;
                        Loader.Msg($"{_activeAvatarModelParent.transform.name}");
                        _activeAvatarAnimator = _activeAvatarModelParent.GetComponent<Animator>();
                        break;
                    case "WeaponL":
                        _weaponL = a.gameObject;
                        Loader.Msg($"Found {_weaponL.name}");
                        break;
                    case "WeaponR":
                        _weaponR = a.gameObject;
                        Loader.Msg($"Found {_weaponR.name}");
                        break;
                    case "+EyeBone L A01":
                        _eyeL = a.gameObject;
                        Loader.Msg($"Found {_eyeL.name}");
                        break;
                    case "+EyeBone R A01":
                        _eyeR = a.gameObject;
                        Loader.Msg($"Found {_eyeR.name}");
                        break;
                    case "+ToothBone D A01":
                        _toothD = a.gameObject;
                        Loader.Msg($"Found {_toothD.name}");
                        break;
                    case "+ToothBone U A01":
                        _toothU = a.gameObject;
                        Loader.Msg($"Found {_toothU.name}");
                        break;
                    case "+FlycloakRootB CB A01":
                        _glider = a.gameObject;
                        Loader.Msg($"Found {_glider.name}");
                        break;
                }

                if (a.name.Contains("WeaponRoot"))
                {
                    _weaponRoot = a.gameObject;
                    Loader.Msg($"Found {_weaponRoot.name}");
                }

                if (a.name.Contains("FlycloakRoot"))
                {
                    _gliderRoot = a.gameObject;
                    Loader.Msg($"Found {_gliderRoot.name}");
                }
            }
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