using System;
using System.Collections.Generic;
using GDEngine.Core;
using GDEngine.Core.Audio;
using GDEngine.Core.Collections;
using GDEngine.Core.Components;
using GDEngine.Core.Components.Controllers;
using GDEngine.Core.Debug;
using GDEngine.Core.Entities;
using GDEngine.Core.Events;
using GDEngine.Core.Extensions;
using GDEngine.Core.Factories;
using GDEngine.Core.Gameplay;
using GDEngine.Core.Impulses;
using GDEngine.Core.Input.Data;
using GDEngine.Core.Input.Devices;
using GDEngine.Core.Managers;
using GDEngine.Core.Orchestration;
using GDEngine.Core.Rendering;
using GDEngine.Core.Rendering.Base;
using GDEngine.Core.Rendering.UI;
using GDEngine.Core.Screen;
using GDEngine.Core.Serialization;
using GDEngine.Core.Services;
using GDEngine.Core.Systems;
using GDEngine.Core.Timing;
using GDEngine.Core.Utilities;
using GDGame.Demos.Controllers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct2D1;
using SharpDX.Direct2D1.Effects;
using SharpDX.Direct3D9;
using SharpDX.XInput;
using The_Depths_of_Elune;
using The_Depths_of_Elune.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Color = Microsoft.Xna.Framework.Color;
using Effect = Microsoft.Xna.Framework.Graphics.Effect;
using Material = GDEngine.Core.Rendering.Material;
using SamplerState = Microsoft.Xna.Framework.Graphics.SamplerState;
using Time = GDEngine.Core.Timing.Time;
using Transform = GDEngine.Core.Components.Transform;


namespace The_Depths_of_Elune
{
    public class Main : Game
    {
        #region Core Fields (Common to all games)     
        private GraphicsDeviceManager _graphics;
        private ContentDictionary<Texture2D> _textureDictionary;
        private ContentDictionary<Model> _modelDictionary;
        private ContentDictionary<SpriteFont> _fontDictionary;
        private ContentDictionary<SoundEffect> _soundDictionary;
        private ContentDictionary<Effect> _effectsDictionary;
        private Scene _scene;
        private Camera _camera;
        private bool _disposed = false;
        private OrchestrationSystem _orchestrationSystem;
        private Material _matBasicUnlit, _matBasicLit, _matAlphaCutout, _matBasicUnlitGround;
        #endregion

        #region Demo Fields (remove in the game)
        private AnimationCurve3D _animationPositionCurve, _animationRotationCurve;
        private AnimationCurve _animationCurve;
        private GameObject _cameraGO;
        private SceneManager _sceneManager;
        private int _dummyHealth;
        private KeyboardState _newKBState, _oldKBState;
        private int _damageAmount;
        private SoundEffectInstance _soundEffectInstance;
        private SoundEffect _soundEffect;
        private Material _char, _matt;
        private DialogueBox _dialogueBox;
        private DialogueManager _dialogueManager;
        private GameObject playerParent;
        private CharacterController celesteController;
        private CharacterController khaslanaController;
        private ChestController mimicController;
        private MenuManager _menuManager;
        #endregion

        #region Core Methods (Common to all games)     
        public Main()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            #region Core

            // Give the game a name
            Window.Title = "The Depths of Elune";

            // Set resolution and centering (by monitor index)
            InitializeGraphics(ScreenResolution.R_HD_16_9_1280x720);

            // Center and hide the mouse!
            InitializeMouse();

            // Shared data across entities
            InitializeContext();

            // Assets from string names in JSON
            var relativeFilePathAndName = "assets/data/asset_manifest.json";
            LoadAssetsFromJSON(relativeFilePathAndName);

            // All effects used in game
            InitializeEffects();

            InitializeSceneManager();
            // Scene to hold game objects
            InitializeScene();

            // Camera, UI, Menu, Physics, Rendering etc.
            InitializeSystems();

            // Setup player
            InitializePlayer();

            // All cameras we want in the game are loaded now and one set as active
            InitializeCameras();

            InitializeCameraManagers();
            //game manager, camera changer, FSM, AI
            //InitializeManagers();

            // Setup world
            int scale = 300;
            InitializeSkyParent();
            InitializeSkyBox(scale);
            InitializeCollidableGround(scale);

            //Initialize UI
            InitializeUI();

            //Initialize Menu Manager
            InitializeMenuManager();

            // Set the active scene
            _sceneManager.SetActiveScene(AppData.LEVEL_1_NAME);

            // Camera-demos
            InitializeAnimationCurves();

            //Load in assets
            LoadFromJSON();

            //Build Characters / Chests
            InitializeCharacters();

            //Build Room
            InitializeRoom();

            //Set Game state conditions
            SetWinConditions();

            #endregion

            base.Initialize();
        }

        //Unsure if needed for rn
        private void SetPauseShowMenu()
        {
            // Give scenemanager the events reference so that it can publish the pause event
            _sceneManager.EventBus = EngineContext.Instance.Events;
            // Set paused and publish pause event
            _sceneManager.Paused = true;

            // Put all components that should be paused to sleep
            EngineContext.Instance.Events.Subscribe<GamePauseChangedEvent>(e =>
            {
                bool paused = e.IsPaused;

                _sceneManager.ActiveScene.GetSystem<PhysicsSystem>()?.SetPaused(paused);
                _sceneManager.ActiveScene.GetSystem<PhysicsDebugSystem>()?.SetPaused(paused);
                _sceneManager.ActiveScene.GetSystem<GameStateSystem>()?.SetPaused(paused);
            });
        }

        private void InitializeSceneManager()
        {
            _sceneManager = new SceneManager(this);
            Components.Add(_sceneManager);
        }

        private void InitializeCameraManagers()
        {
            //inside scene
            var go = new GameObject("Camera Manager");
            go.AddComponent<CameraEventListener>();
            _sceneManager.ActiveScene.Add(go);
        }

        private void InitializeMenuManager()
        {
            _menuManager = new MenuManager(this, _sceneManager);
            Components.Add(_menuManager);
            Time.Pause();

            Texture2D button = _textureDictionary.Get("button_texture");
            Texture2D trackTex = _textureDictionary.Get("volume_slider");
            Texture2D handleTex = _textureDictionary.Get("star");
            Texture2D controlsTx = _textureDictionary.Get("celeste_texture");
            SpriteFont uiFont = _fontDictionary.Get("dialoguefont");
            Texture2D main_menu = _textureDictionary.Get("main_menu");
            Texture2D settings_screen = _textureDictionary.Get("settings_screen");
            Texture2D controls_menu = _textureDictionary.Get("sky");
            Texture2D death_screen = _textureDictionary.Get("death_screen");
            Texture2D win_screen = _textureDictionary.Get("win_screen");

            // Wire UIManager to the menu scene
            _menuManager.Initialize(_sceneManager.ActiveScene,
                button, trackTex, handleTex, controlsTx, uiFont,main_menu,settings_screen,controls_menu,death_screen,win_screen);

            // Subscribe to high-level events
            _menuManager.PlayRequested += () =>
            {
                _sceneManager.Paused = false;
                Time.Resume();
                _menuManager.HideMenus();

                //fade out menu sound
            };

            _menuManager.ExitRequested += () =>
            {
                Exit();
            };

            _menuManager.MusicVolumeChanged += v =>
            {
                // Forward to audio manager
                System.Diagnostics.Debug.WriteLine("MusicVolumeChanged");

                //raise event to set sound
                // EngineContext.Instance.Events.Publish(new PlaySfxEvent)
            };

            _menuManager.SfxVolumeChanged += v =>
            {
                // Forward to audio manager
                System.Diagnostics.Debug.WriteLine("SfxVolumeChanged");

                //raise event to set sound
            };

            //Event for wanting to retry level
            _menuManager.RetryRequested += () =>
            {
                _sceneManager.Paused = false;
                //Hide the menu
                _menuManager.HideMenus();
                //TO DO add restart
            };

            //Subscribe to event to show the death and win screens
            EngineContext.Instance.Events.Subscribe<showLostEvent>(e =>
            {
                _menuManager.ShowDeathMenu();
                _sceneManager.Paused = true;
            });

            EngineContext.Instance.Events.Subscribe<showWonEvent>(e =>
            {
                _menuManager.ShowWonMenu();
                _sceneManager.Paused = true;
            });
        }

        private void InitializePlayer()
        {
            playerParent = new GameObject("PlayerParent");
            playerParent.AddComponent<KeyboardWASDController>();    
            playerParent.AddComponent<MouseYawPitchController>(); 
            playerParent.Transform.TranslateTo(new Vector3(0, 0, 0)); 
            
            // Listen for damage events on the player
            playerParent.AddComponent<DamageEventListener>(); 
            
            // Adds an inventory to the player
            playerParent.AddComponent<InventoryComponent>(); 

            //GameObject playerModel = InitializeModel(new Vector3(0, 0, 0), new Vector3(-90, 0, 0), new Vector3(1, 1, 1), "celeste_texture", "celeste", AppData.PLAYER_NAME); 
            
            var playerCollider = playerParent.AddComponent<BoxCollider>(); 
            playerCollider.Size = new Vector3(3, 4.5f, 3); 

            var playerRb = playerParent.AddComponent<RigidBody>(); 
            playerRb.BodyType = BodyType.Kinematic; playerRb.Mass = 1f; 

            //playerModel.Transform.SetParent(playerParent); 
            _sceneManager.ActiveScene.Add(playerParent);
        }



        private void InitializeAnimationCurves()
        {
            //1D animation curve demo (e.g. scale, audio volume, lerp factor for color, etc)
            _animationCurve = new AnimationCurve(CurveLoopType.Cycle);
            _animationCurve.AddKey(0f, 10);
            _animationCurve.AddKey(2f, 11); //up
            _animationCurve.AddKey(0f, 12); //down
            _animationCurve.AddKey(8f, 13); //up further
            _animationCurve.AddKey(0f, 13.5f); //down

            //3D animation curve demo
            _animationPositionCurve = new AnimationCurve3D(CurveLoopType.Oscillate);
            _animationPositionCurve.AddKey(new Vector3(0, 4, 0), 0);
            _animationPositionCurve.AddKey(new Vector3(5, 8, 2), 1);
            _animationPositionCurve.AddKey(new Vector3(10, 12, 4), 2);
            _animationPositionCurve.AddKey(new Vector3(0, 4, 0), 3);

            // Absolute yaw/pitch/roll angles (radians) over time
            _animationRotationCurve = new AnimationCurve3D(CurveLoopType.Oscillate);
            _animationRotationCurve.AddKey(new Vector3(0, 0, 0), 0);              // yaw, pitch, roll
            _animationRotationCurve.AddKey(new Vector3(0, MathHelper.PiOver2, 0), 1);
            _animationRotationCurve.AddKey(new Vector3(0, MathHelper.Pi, 0), 2);
            _animationRotationCurve.AddKey(new Vector3(0, 0, 0), 3);
        }

        private void InitializeGraphics(Integer2 resolution)
        {
            // Enable per-monitor DPI awareness so the window/UI scales crisply on multi-monitor setups with different DPIs (avoids blurriness when moving between screens).
            System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);

            // Set preferred resolution
            ScreenResolution.SetResolution(_graphics, resolution);

            // Center on primary display (set to index of the preferred monitor)
            WindowUtility.CenterOnMonitor(this, 1);
        }

        private void InitializeMouse()
        {
            Mouse.SetPosition(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);

            // Set old state at start so its not null for comparison with new state in Update
            _oldKBState = Keyboard.GetState();
        }

        private void InitializeContext()
        {
            EngineContext.Initialize(GraphicsDevice, Content);
        }

        /// <summary>
        /// New asset loading from JSON using AssetEntry and ContentDictionary::LoadFromManifest
        /// </summary>
        /// <param name="relativeFilePathAndName"></param>
        /// <see cref="AssetEntry"/>
        /// <see cref="ContentDictionary{T}"/>
        private void LoadAssetsFromJSON(string relativeFilePathAndName)
        {
            // Make dictionaries to store assets
            _textureDictionary = new ContentDictionary<Texture2D>();
            _modelDictionary = new ContentDictionary<Model>();
            _fontDictionary = new ContentDictionary<SpriteFont>();
            _soundDictionary = new ContentDictionary<SoundEffect>();
            _effectsDictionary = new ContentDictionary<Effect>();
            //TODO - Add dictionary loading for other assets - song, other?

            var manifests = JSONSerializationUtility.LoadData<AssetManifest>(Content, relativeFilePathAndName); // single or array
            if (manifests.Count > 0)
            {
                foreach (var m in manifests)
                {
                    _modelDictionary.LoadFromManifest(m.Models, e => e.Name, e => e.ContentPath, overwrite: true);
                    _textureDictionary.LoadFromManifest(m.Textures, e => e.Name, e => e.ContentPath, overwrite: true);
                    _fontDictionary.LoadFromManifest(m.Fonts, e => e.Name, e => e.ContentPath, overwrite: true);
                    _soundDictionary.LoadFromManifest(m.Sounds, e => e.Name, e => e.ContentPath, overwrite: true);
                    _effectsDictionary.LoadFromManifest(m.Effects, e => e.Name, e => e.ContentPath, overwrite: true);
                    //TODO - Add dictionary loading for other assets - song, other?
                }
            }
        }

        private void InitializeEffects()
        {
            #region Unlit Textured BasicEffect 
            var unlitBasicEffect = new BasicEffect(_graphics.GraphicsDevice)
            {
                TextureEnabled = true,
                LightingEnabled = false,
                VertexColorEnabled = false
            };

            _matBasicUnlit = new Material(unlitBasicEffect);
            _matBasicUnlit.StateBlock = RenderStates.Opaque3D();      // depth on, cull CCW
            _matBasicUnlit.SamplerState = Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp;   // helps avoid texture seams on sky

            //ground texture where UVs above [0,0]-[1,1]
            _matBasicUnlitGround = new Material(unlitBasicEffect.Clone());
            _matBasicUnlitGround.StateBlock = RenderStates.Opaque3D();      // depth on, cull CCW
            _matBasicUnlitGround.SamplerState = SamplerState.AnisotropicWrap;   // wrap texture based on UV values

            #endregion

            #region Lit Textured BasicEffect 
            var litBasicEffect = new BasicEffect(_graphics.GraphicsDevice)
            {
                TextureEnabled = true,
                LightingEnabled = true,
                PreferPerPixelLighting = true,
                VertexColorEnabled = false
            };
            litBasicEffect.EnableDefaultLighting();
            _matBasicLit = new Material(litBasicEffect);
            _matBasicLit.StateBlock = RenderStates.Opaque3D();
            #endregion

            #region Character Material (Without Culling for complex models)

            var characterNoCull = new BasicEffect(_graphics.GraphicsDevice)
            {
                TextureEnabled = true,
                LightingEnabled = false,
                PreferPerPixelLighting = false,
                VertexColorEnabled = false
            };

            _char = new Material(litBasicEffect);
            _char.StateBlock = RenderStates.Opaque3D().WithRaster(new RasterizerState { CullMode = CullMode.None });
            _char.SamplerState = Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp;

            #endregion

            #region Matt Material (Without Culling for complex models)

            var materialNoCull = new BasicEffect(_graphics.GraphicsDevice)
            {
                TextureEnabled = true,
                LightingEnabled = true,
                PreferPerPixelLighting = false,
                VertexColorEnabled = false
            };

            _matt = new Material(unlitBasicEffect);
            _matt.StateBlock = RenderStates.Opaque3D().WithRaster(new RasterizerState { CullMode = CullMode.None });
            _matt.SamplerState = Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp;

            #endregion
        }

        private void InitializeScene()
        {
            // Make a scene that will store all drawn objects and systems for that level
            var scene = new Scene(EngineContext.Instance, "dungeon - main level");

            // Add each new scene into the manager
            _sceneManager.AddScene(AppData.LEVEL_1_NAME, scene);

            // Set the active scene before anything that uses ActiveScene
            _sceneManager.SetActiveScene(AppData.LEVEL_1_NAME);
        }

        private void InitializeSystems()
        {
            InitializePhysicsSystem();
            InitializePhysicsDebugSystem(true);
            InitializeEventSystem();  //propagate events
            InitializeInputSystem();  //input
            InitializeCameraAndRenderSystems(); //update cameras, draw renderable game objects, draw ui and menu
            InitializeAudioSystem();
            InitializeOrchestrationSystem();
            InitializeGameStateSystem();
            InitializeUIEventSystem();
        }

        private void InitializeGameStateSystem()
        {
            // Add game state system
            _sceneManager.ActiveScene.AddSystem(new GameStateSystem());
        }

        private void InitializeUIEventSystem()
        {
            _sceneManager.ActiveScene.AddSystem(new UIEventSystem());
        }

        private void InitializeOrchestrationSystem()
        {
            var orchestrationSystem = new OrchestrationSystem();
            orchestrationSystem.Configure(options =>
            {
                options.Time = Orchestrator.OrchestrationTime.Unscaled;
                options.LocalScale = 1;
                options.Paused = false;
            });
            _sceneManager.ActiveScene.Add(orchestrationSystem);

        }

        private void InitializeAudioSystem()
        {
            _sceneManager.ActiveScene.Add(new AudioSystem(_soundDictionary));
        }

        private void InitializePhysicsDebugSystem(bool isEnabled)
        {
            var physicsDebugRenderer = _sceneManager.ActiveScene.AddSystem(new PhysicsDebugSystem());

            // Toggle debug rendering on/off
            physicsDebugRenderer.Enabled = isEnabled; // or false to hide

            // Optional: Customize colors
            physicsDebugRenderer.StaticColor = Color.Green;      // Immovable objects
            physicsDebugRenderer.KinematicColor = Color.Blue;    // Animated objects
            physicsDebugRenderer.DynamicColor = Color.Yellow;    // Physics-driven objects
            physicsDebugRenderer.TriggerColor = Color.Red;       // Trigger volumes

        }

        private void InitializePhysicsSystem()
        {
            // 1. add physics
            var physicsSystem = _sceneManager.ActiveScene.AddSystem(new PhysicsSystem());
            physicsSystem.Gravity = AppData.GRAVITY;
        }

        private void InitializeEventSystem()
        {
            _sceneManager.ActiveScene.Add(new EventSystem(EngineContext.Instance.Events));
        }

        private void InitializeCameraAndRenderSystems()
        {
            //manages camera
            var cameraSystem = new CameraSystem(_graphics.GraphicsDevice, -100);
            _sceneManager.ActiveScene.Add(cameraSystem);

            //3d
            var renderSystem = new RenderSystem(-100);
            _sceneManager.ActiveScene.Add(renderSystem);

            //2d
            var uiRenderSystem = new UIRenderSystem(-100);
            _sceneManager.ActiveScene.Add(uiRenderSystem);
        }

        private void InitializeInputSystem()
        {
            //set mouse, keyboard binding keys (e.g. WASD)
            var bindings = InputBindings.Default;
            // optional tuning
            bindings.MouseSensitivity = 0.12f;  // mouse look scale
            bindings.DebounceMs = 60;           // key/mouse debounce in ms
            bindings.EnableKeyRepeat = true;    // hold-to-repeat
            bindings.KeyRepeatMs = 300;         // repeat rate in ms

            // Create the input system 
            var inputSystem = new InputSystem();

            //register all the devices, you dont have to, but its for the demo
            inputSystem.Add(new GDKeyboardInput(bindings));
            inputSystem.Add(new GDMouseInput(bindings));
            inputSystem.Add(new GDGamepadInput(PlayerIndex.One, "Gamepad P1"));

            _sceneManager.ActiveScene.Add(inputSystem);
        }

        private void InitializeCameras()
        {
            Scene scene = _sceneManager.ActiveScene;

            GameObject cameraGO = null;
            Camera camera = null;
            #region Static birds-eye camera
            cameraGO = new GameObject(AppData.CAMERA_NAME_STATIC_BIRDS_EYE);
            camera = cameraGO.AddComponent<Camera>();
            camera.FieldOfView = MathHelper.ToRadians(80);
            //ISRoT
            cameraGO.Transform.RotateEulerBy(new Vector3(MathHelper.ToRadians(-90), 0, 0));
            cameraGO.Transform.TranslateTo(Vector3.UnitY * 50);

            // _cameraGO.AddComponent<MouseYawPitchController>();

            scene.Add(cameraGO);

            // _camera.FieldOfView
            //TODO - add camera
            #endregion

            #region Third-person camera
            cameraGO = new GameObject(AppData.CAMERA_NAME_THIRD_PERSON);
            camera = cameraGO.AddComponent<Camera>();

            var thirdPersonController = new ThirdPersonController();
            thirdPersonController.TargetName = AppData.PLAYER_NAME;
            thirdPersonController.ShoulderOffset = 0;
            thirdPersonController.FollowDistance = 50;
            thirdPersonController.RotationDamping = 20;
            cameraGO.AddComponent(thirdPersonController);
            scene.Add(cameraGO);
            #endregion



            #region First-person camera
            cameraGO = new GameObject(AppData.CAMERA_NAME_FIRST_PERSON);

            cameraGO.Transform.SetParent(playerParent);
            cameraGO.Transform.TranslateTo(new Vector3(0, 4.5f, 3));

            camera = cameraGO.AddComponent<Camera>();
            camera.FarPlane = 1000;
            camera.AspectRatio = (float)_graphics.PreferredBackBufferWidth / _graphics.PreferredBackBufferHeight;
            cameraGO.AddComponent<CameraImpulseListener>();
            //cameraGO.AddComponent<MouseYawPitchController>();


            // Add it to the scene
            scene.Add(cameraGO);
            #endregion

            scene.SetActiveCamera(AppData.CAMERA_NAME_FIRST_PERSON);

        }

        /// <summary>
        /// Add parent root at origin to rotate the sky
        /// </summary>
        private void InitializeSkyParent()
        {
            var _skyParent = new GameObject("SkyParent");
            var rot = _skyParent.AddComponent<RotationController>();

            // Turntable spin around local +Y
            rot._rotationAxisNormalized = Vector3.Up;

            // Dramatised fast drift at 2 deg/sec. 
            rot._rotationSpeedInRadiansPerSecond = MathHelper.ToRadians(2f);
            _sceneManager.ActiveScene.Add(_skyParent);
        }

        private void InitializeSkyBox(int scale = 50)
        {
            Scene scene = _sceneManager.ActiveScene;
            GameObject gameObject = null;
            MeshFilter meshFilter = null;
            MeshRenderer meshRenderer = null;

            // Find the sky parent object to attach sky to so sky rotates
            GameObject skyParent = scene.Find((GameObject go) => go.Name.Equals("SkyParent"));

            // back
            gameObject = new GameObject("back");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.TranslateTo(new Vector3(0, 0, -scale / 2));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("sky");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);

            // left
            gameObject = new GameObject("left");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(0, MathHelper.ToRadians(90), 0), true);
            gameObject.Transform.TranslateTo(new Vector3(-scale / 2, 0, 0));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("sky");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);


            // right
            gameObject = new GameObject("right");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(0, MathHelper.ToRadians(-90), 0), true);
            gameObject.Transform.TranslateTo(new Vector3(scale / 2, 0, 0));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("sky");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);

            // front
            gameObject = new GameObject("front");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(0, MathHelper.ToRadians(180), 0), true);
            gameObject.Transform.TranslateTo(new Vector3(0, 0, scale / 2));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("sky");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);

            // sky (top)
            gameObject = new GameObject("sky");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(MathHelper.ToRadians(90), 0, MathHelper.ToRadians(90)), true);
            gameObject.Transform.TranslateTo(new Vector3(0, scale / 2, 0));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicLit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("sky");
            scene.Add(gameObject);

            //set parent to allow rotation
            gameObject.Transform.SetParent(skyParent.Transform);

        }

        private void InitializeCollidableGround(int scale = 300)
        {
            GameObject gameObject = null;
            MeshFilter meshFilter = null;
            MeshRenderer meshRenderer = null;

            gameObject = new GameObject("ground");
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);

            meshFilter = MeshFilterFactory.CreateQuadGridTexturedUnlit(_graphics.GraphicsDevice,
                 1,
                 1,
                 1,
                 1,
                 20,
                 20);


            gameObject.Transform.ScaleBy(new Vector3(scale * 3, scale * 3, 1));
            gameObject.Transform.RotateEulerBy(new Vector3(MathHelper.ToRadians(-90), 0, 0), true);
            gameObject.Transform.TranslateTo(new Vector3(0, -0.5f, 0));

            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlitGround;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("ground");

            // Add a box collider matching the ground size
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.Size = new Vector3(scale, scale, 0.025f);
            collider.Center = new Vector3(0, 0, -0.0125f);

            // Add rigidbody as Static (immovable)
            var rigidBody = gameObject.AddComponent<RigidBody>();
            rigidBody.BodyType = BodyType.Static;
            gameObject.IsStatic = true;

            _sceneManager.ActiveScene.Add(gameObject);
        }

        private void InitializeUI()
        {
            InitializeUIReticleRenderer();
            InitializeUIDialogue();
        }



        private void InitializeUIReticleRenderer()
        {
            var uiGO = new GameObject("HUD");

            var reticleAtlas = _textureDictionary.Get("star");
            var uiFont = _fontDictionary.Get("mouse_reticle_font");

            // Reticle (cursor): always on top
            var reticle = new UIReticle(reticleAtlas);
            reticle.Origin = reticleAtlas.GetCenter();
            reticle.SourceRectangle = null;
            reticle.Scale = new Vector2(0.1f, 0.1f);
            reticle.RotationSpeedDegPerSec = 10;
            reticle.LayerDepth = UILayer.Cursor;
            uiGO.AddComponent(reticle);

            _sceneManager.ActiveScene.Add(uiGO);

            // Hide mouse since reticle will take its place
            IsMouseVisible = false;
        }

        #region UI Dialogue
        private void InitializeUIDialogue()
        {
            Scene _scene = _sceneManager.ActiveScene;
            var dialogueFont = _fontDictionary.Get("menuFont");
            var nameFont = _fontDictionary.Get("dialoguefont");

            int boxHeight = 200;
            int boxWidth = _graphics.PreferredBackBufferWidth - 100;

            //Create a rectangle to store the size of the dialogue box
            Rectangle dialogueBox = new Rectangle(
                50,
                _graphics.PreferredBackBufferHeight - boxHeight - 50,
                boxWidth,
                boxHeight
            );
            //Background image
            Texture2D dialogueTexture = _textureDictionary.Get("dialogue_texture");

            //Store the character pictures and their names
            var portraits = new Dictionary<string, Texture2D>()
{
            { "Elysia", _textureDictionary.Get("elysia_portrait") },
            { "Celeste", _textureDictionary.Get("celeste_portrait") },
            { "Khaslana", _textureDictionary.Get("khaslana_portrait") }
};

            //Create the dialogue box
            _dialogueBox = new DialogueBox(nameFont,dialogueFont, dialogueTexture, GraphicsDevice, dialogueBox,portraits);

            var dialogueGO = new GameObject("dialogueBox");
            dialogueGO.AddComponent(_dialogueBox);

            _dialogueManager = new DialogueManager(_dialogueBox);
            dialogueGO.AddComponent(_dialogueManager);
            _scene.Add(dialogueGO);
        }
        #endregion

        /// <summary>
        /// Adds a single-part FBX model into the scene.
        /// </summary>
        private GameObject InitializeModel(Vector3 position,
            Vector3 eulerRotationDegrees, Vector3 scale,
            string textureName, string modelName, string objectName)
        {
            GameObject gameObject = null;

            gameObject = new GameObject(objectName);
            gameObject.Transform.TranslateTo(position);
            gameObject.Transform.RotateEulerBy(eulerRotationDegrees * MathHelper.Pi / 180f);
            gameObject.Transform.ScaleTo(scale);

            var model = _modelDictionary.Get(modelName);
            var texture = _textureDictionary.Get(textureName);
            var meshFilter = MeshFilterFactory.CreateFromModel(model, _graphics.GraphicsDevice, 0, 0);
            gameObject.AddComponent(meshFilter);

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshRenderer.Material = _matBasicLit;
            meshRenderer.Overrides.MainTexture = texture;

            _sceneManager.ActiveScene.Add(gameObject);

            return gameObject;
        }

        protected override void Update(GameTime gameTime)
        {
            //call time update
            #region Core
            Time.Update(gameTime);

            #endregion
            _newKBState = Keyboard.GetState();

            //Check for these
            ToggleFullscreen();
            CameraSwitch();

            checkDialogue();
            SigilPickup();
            
            _oldKBState = _newKBState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);


            base.Draw(gameTime);
        }

        /// <summary>
        /// Override Dispose to clean up engine resources.
        /// MonoGame's Game class already implements IDisposable, so we override its Dispose method.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                System.Diagnostics.Debug.WriteLine("Disposing Main...");

                // 1. Dispose Scene (which will cascade to GameObjects and Components)
                System.Diagnostics.Debug.WriteLine("Disposing Scene");
                _scene?.Dispose();
                _scene = null;

                // 2. Dispose Materials (which may own Effects)
                System.Diagnostics.Debug.WriteLine("Disposing Materials");
                _matBasicUnlit?.Dispose();
                _matBasicUnlit = null;

                _matBasicLit?.Dispose();
                _matBasicLit = null;

                _matAlphaCutout?.Dispose();
                _matAlphaCutout = null;

                // 3. Clear cached MeshFilters in factory registry
                System.Diagnostics.Debug.WriteLine("Clearing MeshFilter Registry");
                MeshFilterFactory.ClearRegistry();

                // 4. Dispose content dictionaries (now they implement IDisposable!)
                System.Diagnostics.Debug.WriteLine("Disposing Content Dictionaries");
                _textureDictionary?.Dispose();
                _textureDictionary = null;

                _modelDictionary?.Dispose();
                _modelDictionary = null;

                _fontDictionary?.Dispose();
                _fontDictionary = null;

                // 5. Dispose EngineContext (which owns SpriteBatch and Content)
                System.Diagnostics.Debug.WriteLine("Disposing EngineContext");
                EngineContext.Instance?.Dispose();

                // 6. Clear references to help GC
                System.Diagnostics.Debug.WriteLine("Clearing References");
                _animationCurve = null;
                _animationPositionCurve = null;
                _animationRotationCurve = null;

                System.Diagnostics.Debug.WriteLine("Main disposal complete");
            }

            _disposed = true;

            // Always call base.Dispose
            base.Dispose(disposing);
        }

        #endregion    }

        #region Other Methods

        private void SetWinConditions()
        {
            var gameStateSystem = _sceneManager.ActiveScene.GetSystem<GameStateSystem>();

            // Lose condition: health < 10 AND time > 60
            IGameCondition loseCondition =
                GameConditions.FromPredicate("died to mimic", CheckDeath);

            IGameCondition winCondition =
            GameConditions.FromPredicate("reached khaslana", CheckWon);

            // Configure GameStateSystem (no win condition yet)
            gameStateSystem.ConfigureConditions(winCondition, loseCondition);
            gameStateSystem.StateChanged += HandleGameStateChange;
        }

        private bool CheckDeath()
        {
            return mimicController.gameLost;
        }

        private bool CheckWon()
        {
            return khaslanaController.gameWon;
        }

        private void HandleGameStateChange(GameOutcomeState oldState, GameOutcomeState newState)
        {
            System.Diagnostics.Debug.WriteLine($"Old state was {oldState} and new state is {newState}");

            //If the player has interacted with a mimic
            if (newState == GameOutcomeState.Lost)
            {
                var orchestrator = _sceneManager.ActiveScene.GetSystem<OrchestrationSystem>().Orchestrator;
                orchestrator.Build("death sequence")
                    //Dont do it immediately
                    .WaitSeconds(2.5f)
                    //Use an event to place the death screen
                    .Publish(new showLostEvent())
                    .Register();

                orchestrator.Start("death sequence", _sceneManager.ActiveScene, EngineContext.Instance);
                System.Diagnostics.Debug.WriteLine("You lost!");
            }
            else if (newState == GameOutcomeState.Won)
            {
                System.Diagnostics.Debug.WriteLine("You win!");

                if (newState == GameOutcomeState.Won)
                {
                    var orchestrator = _sceneManager.ActiveScene.GetSystem<OrchestrationSystem>().Orchestrator;
                    orchestrator.Build("win sequence")
                        .WaitSeconds(1f)
                        .Publish(new showWonEvent())
                        .Register();

                    orchestrator.Start("win sequence", _sceneManager.ActiveScene, EngineContext.Instance);

                }
            }
        }

        private void DemoAudioSystem()
        {
            var events = EngineContext.Instance.Events;

            //TODO - Exercise
            bool isD3Pressed = _newKBState.IsKeyDown(Keys.D3) && !_oldKBState.IsKeyDown(Keys.D3);
            if (isD3Pressed)
            {
                events.Publish(new PlaySfxEvent("SFX_UI_Click_Designed_Pop_Generic_1",
                    1, false, null));
            }

            bool isD4Pressed = _newKBState.IsKeyDown(Keys.D4) && !_oldKBState.IsKeyDown(Keys.D4);
            if (isD4Pressed)
            {
                events.Publish(new PlayMusicEvent("secret_door", 1, 8));
            }

            bool isD5Pressed = _newKBState.IsKeyDown(Keys.D5) && !_oldKBState.IsKeyDown(Keys.D5);
            if (isD5Pressed)
            {
                events.Publish(new StopMusicEvent(4));
            }

            bool isD6Pressed = _newKBState.IsKeyDown(Keys.D6) && !_oldKBState.IsKeyDown(Keys.D6);
            if (isD6Pressed)
            {
                events.Publish(new FadeChannelEvent(AudioMixer.AudioChannel.Master,
                    0.1f, 4));
            }

            bool isD7Pressed = _newKBState.IsKeyDown(Keys.D7) && !_oldKBState.IsKeyDown(Keys.D7);
            if (isD7Pressed)
            {
                //expensive and crude => move to Component::Start()
                var go = _scene.Find(go => go.Name.Equals(AppData.PLAYER_NAME));
                Transform emitterTransform = go.Transform;

                events.Publish(new PlaySfxEvent("hand_gun1",
                    1, true, emitterTransform));
            }
        }

        private void ToggleFullscreen()
        {
            bool togglePressed = _newKBState.IsKeyDown(Keys.F5) && !_oldKBState.IsKeyDown(Keys.F5);
            if (togglePressed)
                _graphics.ToggleFullScreen();
        }

        private void CameraSwitch()
        {
            var events = EngineContext.Instance.Events;

            bool isFirst = _newKBState.IsKeyDown(Keys.D1) && !_oldKBState.IsKeyDown(Keys.D1);
            if (isFirst)
            {
                events.Post(new CameraEvent(AppData.CAMERA_NAME_FIRST_PERSON));
                events.Publish(new PlaySfxEvent("SFX_UI_Click_Designed_Pop_Generic_1",
                  1, false, null));
            }

            bool isThird = _newKBState.IsKeyDown(Keys.D2) && !_oldKBState.IsKeyDown(Keys.D2);
            if (isThird)
            {
                events.Post(new CameraEvent(AppData.CAMERA_NAME_THIRD_PERSON));
                events.Publish(new PlaySfxEvent("SFX_UI_Click_Designed_Pop_Mallet_Open_1",
                1, false, null));
            }
        }

        private void LoadFromJSON()
        {
            var relativeFilePathAndName = "assets/data/single_model_spawn.json";
            List<ModelSpawnData> mList = JSONSerializationUtility.LoadData<ModelSpawnData>(Content, relativeFilePathAndName);

            relativeFilePathAndName = "assets/data/multi_model_spawn.json";
            //load multiple models
            foreach (var d in JSONSerializationUtility.LoadData<ModelSpawnData>(Content, relativeFilePathAndName))
                InitializeModel(d.Position, d.RotationDegrees, d.Scale, d.TextureName, d.ModelName, d.ObjectName);
        }

        private void InitializeCharacters()
        {
            Scene _scene = _sceneManager.ActiveScene;
            //celeste
            GameObject celeste = new GameObject("celeste");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            celeste = InitializeModel(new Vector3(16, -0.5f, -15), new Vector3(-90, -180, 0), new Vector3(1, 1, 1), "celeste_texture", "celeste", "celeste");


            celesteController = celeste.AddComponent<CharacterController>();
            celesteController.Scene = _scene;
            celesteController.CharID = "celeste";
            celesteController.DialogueManager = _dialogueManager;

            var textureRenderer = celeste.AddComponent<MeshRenderer>();
            textureRenderer.Material = _char;

            // Per-object properties via the overrides block
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("celeste_texture");
            _sceneManager.ActiveScene.Add(celeste);
            //Khaslana
            GameObject khaslana = new GameObject("khaslana");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            khaslana = InitializeModel(new Vector3(0, 3f, -60), new Vector3(-90, 0, 0), new Vector3(1, 0.8f, 2), "khaslana_texture", "khaslana", "khaslana");

            khaslanaController = khaslana.AddComponent<CharacterController>();
            khaslanaController.Scene = _scene;
            khaslanaController.CharID = "khaslana";
            khaslanaController.DialogueManager = _dialogueManager;

            textureRenderer = khaslana.AddComponent<MeshRenderer>();
            textureRenderer.Material = _char;

            // Per-object properties via the overrides block
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("khaslana_texture");
            _sceneManager.ActiveScene.Add(khaslana);

            //mimic         
            GameObject mimic = new GameObject("mimic");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            //mimic = InitializeModel(new Vector3(-50, -0.5f, 23), new Vector3(-90, 0, 0), new Vector3(3, 3, 3), "chest_texture", "Mimic", "mimic");

            textureRenderer = mimic.AddComponent<MeshRenderer>();
            textureRenderer.Material = _char;

            // Per-object properties via the overrides block
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("chest_texture");
            _sceneManager.ActiveScene.Add(mimic);

            //chest        
            GameObject chest = new GameObject("chest");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            //chest = InitializeModel(new Vector3(-50, -0.5f, 18), new Vector3(-90, 0, -30), new Vector3(3, 3, 3), "chest_texture", "Chest", "chest");

            textureRenderer = chest.AddComponent<MeshRenderer>();
            textureRenderer.Material = _char;

            // Per-object properties via the overrides block
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("chest_texture");
            _sceneManager.ActiveScene.Add(chest);

            //chest closed      
            GameObject chestClosed = new GameObject("chestClosed");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            // defines a set of closed chests with ids and weather its a mimic or not
            var chestsClosed = new[]
           {
                new { Position = new Vector3(-45, -0.5f, 3), Rotation = new Vector3(-90, -135, 0), Scale = new Vector3(3,3,3), ID = "Chest_01", IsMimic = false, HasJustOpened = false },
                new { Position = new Vector3(-56, -0.5f, 8), Rotation = new Vector3(-90, 80, 0), Scale = new Vector3(3,3,3), ID = "Chest_02", IsMimic = true , HasJustOpened = false},
                new { Position = new Vector3(-32, -0.5f, 22), Rotation = new Vector3(-90, 20, 0), Scale = new Vector3(3,3,3), ID = "Chest_03", IsMimic = true, HasJustOpened = false},
                new { Position = new Vector3(-39, -0.5f, 11), Rotation = new Vector3(-90, -45, 0), Scale = new Vector3(3,3,3), ID = "Chest_04", IsMimic = true, HasJustOpened = false},
                new { Position = new Vector3(-30, -0.5f, 6), Rotation = new Vector3(-90, 260, 0), Scale = new Vector3(3,3,3), ID = "Chest_05", IsMimic = true, HasJustOpened = false},
                new { Position = new Vector3(-46, -0.5f, 14), Rotation = new Vector3(-90, 0, 0), Scale = new Vector3(3,3,3), ID = "Chest_06", IsMimic = true, HasJustOpened = false},
                new { Position = new Vector3(-44, -0.5f, 23), Rotation = new Vector3(-90, 90, 0), Scale = new Vector3(3,3,3), ID = "Chest_07", IsMimic = true, HasJustOpened = false},
                new { Position = new Vector3(-54, -0.5f, 22), Rotation = new Vector3(-90, -50, 0), Scale = new Vector3(3,3,3), ID = "Chest_08", IsMimic = true, HasJustOpened = false}
           };

            foreach (var c in chestsClosed)
            {
                //initialize the chest GO with the specified transform and model
                GameObject chestGO = InitializeModel(c.Position, c.Rotation, c.Scale, "chest_texture", "ClosedChest", c.ID);

               

                //set the material and texture for the chest
                var chestMesh = chestGO.GetComponent<MeshRenderer>();
                chestMesh.Material = _char;
                chestMesh.Overrides.MainTexture = _textureDictionary.Get("chest_texture");

                //add the ChestController 
                var chestController = chestGO.AddComponent<ChestController>();
                chestController.ChestID = c.ID;
                chestController.IsReal = !c.IsMimic; //real if not mimic
                chestController.Scene = _scene;
                chestController.DialogueManager = _dialogueManager;

                //stores original transform so we can rebuild it later
                chestController.OriginalPosition = c.Position;
                chestController.OriginalRotation = c.Rotation;
                chestController.OriginalScale = c.Scale;

                //assigns method for model replacement 
                chestController.OnReplaceModel = ReplaceChestModel;

                //if the chest is set to open
                if (c.HasJustOpened)
                {
                    //calls the method to replace the model
                    ReplaceChestModel(chestController);
                    

                }

                _sceneManager.ActiveScene.Add(chestGO);
                //temp to initialize
                mimicController = chestController;

            }


            // set of closed doors 
            //TODO: add collisions to closed doors
            //TODO: move this code to where the room will be initialized 
            var DoorsClosed = new[]
          {
                //mimic room door (First door on the left)
                new { Position = new Vector3(-26f, -1, 12.5f), Rotation = new Vector3(-90, -90, 0), Scale = new Vector3(0.30f,0.35f,0.35f), ID = "Door_01", IsLocked = false, HasJustOpened = false, IsKhasDoor = false },
                //Second door on the left 
                new { Position = new Vector3(-26f, -1, -12.5f), Rotation = new Vector3(-90, -90, 0), Scale = new Vector3(0.30f,0.35f,0.35f), ID = "Door_02", IsLocked = true, HasJustOpened = false, IsKhasDoor = false },
                //first door to the right
                new { Position = new Vector3(26f, -1, 12.5f), Rotation = new Vector3(-90, -270, 0), Scale = new Vector3(0.30f,0.35f,0.35f), ID = "Door_03", IsLocked = true, HasJustOpened = false, IsKhasDoor = false},
                 //first door to the right
                new { Position = new Vector3(26f, -1, -12.5f), Rotation = new Vector3(-90, -270, 0), Scale = new Vector3(0.30f,0.35f,0.35f), ID = "Door_04", IsLocked = true, HasJustOpened = false, IsKhasDoor = false },
                //Khaslana Door
                new { Position = new Vector3(0, -1, -26f), Rotation = new Vector3(-90, -180, 0), Scale = new Vector3(0.35f,0.35f,0.35f), ID = "KhasDoor", IsLocked = true, HasJustOpened = false, IsKhasDoor = true }



           };
          

            foreach (var d in DoorsClosed)
            {
                if(d.IsKhasDoor)
                {
                    //initialize the door GO with the specified transform and model
                    GameObject doorGO = InitializeModel(d.Position, d.Rotation, d.Scale, "DoorTexture", "KhasDoorClosed", d.ID);



                    //set the material and texture for the door
                    var doorMesh = doorGO.GetComponent<MeshRenderer>();
                    doorMesh.Material = _matt;
                    doorMesh.Overrides.MainTexture = _textureDictionary.Get("DoorTexture");

                    //add the doorController 
                    var doorController = doorGO.AddComponent<DoorController>();
                    doorController.DoorID = d.ID;
                    doorController.IsLocked = d.IsLocked;
                    doorController.IsKhasDoor = d.IsKhasDoor;
                    doorController.Scene = _scene;
                    doorController.DialogueManager = _dialogueManager;

                    //stores original transform so we can rebuild it later
                    doorController.OriginalPosition = d.Position;
                    doorController.OriginalRotation = d.Rotation;
                    doorController.OriginalScale = d.Scale;

                    //assigns method for model replacement 
                    doorController.OnReplaceModel = ReplaceDoorModel;

                    //if the door is set to open
                    if (d.HasJustOpened)
                    {
                        //calls the method to replace the model
                        ReplaceDoorModel(doorController);

                    }

                    _sceneManager.ActiveScene.Add(doorGO);
                }
            
                else
                {
                    //initialize the door GO with the specified transform and model
                    GameObject doorGO = InitializeModel(d.Position, d.Rotation, d.Scale, "DoorTexture", "ClosedDoors", d.ID);



                    //set the material and texture for the door
                    var doorMesh = doorGO.GetComponent<MeshRenderer>();
                    doorMesh.Material = _matt;
                    doorMesh.Overrides.MainTexture = _textureDictionary.Get("DoorTexture");

                    //add the doorController 
                    var doorController = doorGO.AddComponent<DoorController>();
                    doorController.DoorID = d.ID;
                    doorController.IsLocked = d.IsLocked;
                    doorController.IsKhasDoor = d.IsKhasDoor;
                    doorController.Scene = _scene;
                    doorController.DialogueManager = _dialogueManager;

                    //stores original transform so we can rebuild it later
                    doorController.OriginalPosition = d.Position;
                    doorController.OriginalRotation = d.Rotation;
                    doorController.OriginalScale = d.Scale;

                    //assigns method for model replacement 
                    doorController.OnReplaceModel = ReplaceDoorModel;

                    //if the door is set to open
                    if (d.HasJustOpened)
                    {
                        //calls the method to replace the model
                        ReplaceDoorModel(doorController);

                    }

                    _sceneManager.ActiveScene.Add(doorGO);
                }
            }
        }

        private void InitializeRoom()
        {
            //Main Room Back Wall
            GameObject MainBackWall = new GameObject("BigDoorWall");
            MainBackWall = InitializeModel(new Vector3(0, -0.5f, -25), new Vector3(270, 0, 0), new Vector3(0.3065f, 0.3065f, 0.3065f), "DoorWall", "BigDoorWall", "MainBackWall");
            
            //set the material and texture for the door
            var textureRenderer = MainBackWall.GetComponent<MeshRenderer>();
            textureRenderer.Material = _char;
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("WallBrick");

            _sceneManager.ActiveScene.Add(MainBackWall);


            //Ceiling Main
            GameObject CeilingMainRoom = new GameObject("CeilingMainRoom");
            CeilingMainRoom = InitializeModel(new Vector3(0, 29.89f, 0), new Vector3(-90, 0, 0), new Vector3(15, 15, 0.5f), "BlankWall", "CeilingMainRoom", "CeilingMainRoom");
            
            //set the material and texture for the door
            textureRenderer = CeilingMainRoom.GetComponent<MeshRenderer>();
            textureRenderer.Material = _char;
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("WallBrick");

            _sceneManager.ActiveScene.Add(CeilingMainRoom);


            var SideDoorWalls = new[]
            {
                //Main Room Left Wall
                new { Position = new Vector3(-24.7f, -0.5f, 0), Rotation = new Vector3(270, 0, 90), Scale = new Vector3(0.3065f,0.3065f,0.3065f), ID = "MainLeftWall"},
                //Main Room Right Wall
                new { Position = new Vector3(24.75f, -0.5f, 0), Rotation = new Vector3(-90, 270, 0), Scale = new Vector3(0.3065f,0.3065f,0.3065f), ID = "MainRightWall"}
               

           };

            foreach (var w in SideDoorWalls)
            {
                GameObject wallGO = InitializeModel(w.Position, w.Rotation, w.Scale, "WallBrick", "DoorWall", w.ID);

                //set the material and texture for the door
                var wallMesh = wallGO.GetComponent<MeshRenderer>();
                wallMesh.Material = _char;
                wallMesh.Overrides.MainTexture = _textureDictionary.Get("WallBrick");

                // Add a box collider matching the ground size
                var collider = wallGO.AddComponent<BoxCollider>();
                collider.Size = new Vector3(0.3065f, 0.3065f, 0.3065f);
                collider.Center = new Vector3(0, 0, -0.0125f);

                // Add rigidbody as Static (immovable)
                var rigidBody = wallGO.AddComponent<RigidBody>();
                rigidBody.BodyType = BodyType.Static;
                wallGO.IsStatic = true;

                _sceneManager.ActiveScene.Add(wallGO);
            }

            var Walls = new[]
            {
                //Main Room Front Wall
                new { Position = new Vector3(0, 14.5f, 25), Rotation = new Vector3(180, 180, 0), Scale = new Vector3(25.1f, 14.9f, 0.74f), ID = "MainFrontWall"},
                
                //Khaslana Room Left Wall
                new { Position = new Vector3(-25.75f, 14.5f, -50), Rotation = new Vector3(0, -90, 0), Scale = new Vector3(24.26f, 14.9f, 0.74f), ID = "KhaslanaLeftWall"},
                //Khaslana Room Right Wall
                new { Position = new Vector3(25.75f, 14.5f, -50), Rotation = new Vector3(0, 90, 0), Scale = new Vector3(24.26f, 14.9f, 0.74f), ID = "KhaslanaRightWall"},
                //Khaslana Room Back Wall
                new { Position = new Vector3(0, 14.5f, -75), Rotation = new Vector3(0, 180, 0), Scale = new Vector3(25.1f, 14.9f, 0.74f), ID = "KhaslanaBackWall"},
                
                //Left Rooms Front Wall
                new { Position = new Vector3(-42.85f, 14.5f, 25), Rotation = new Vector3(180, 180, 0), Scale = new Vector3(16.4f, 14.9f, 0.74f), ID = "LeftRoomsFrontWall"},
                //Left Rooms Back Wall
                new { Position = new Vector3(-42.85f, 14.5f, -25), Rotation = new Vector3(0, 180, 0), Scale = new Vector3(16.4f, 14.9f, 0.74f), ID = "LeftRoomsBackWall"},
                //Left Rooms Left Wall
                new { Position = new Vector3(-60, 14.5f, 0), Rotation = new Vector3(0, -90, 0), Scale = new Vector3(25.1f, 14.9f, 0.74f), ID = "LeftRoomsLeftWall"},
                //Left Rooms Divider Wall
                new { Position = new Vector3(-42.85f, 14.5f, 0), Rotation = new Vector3(0, 180, 0), Scale = new Vector3(16.4f, 14.9f, 0.74f), ID = "LeftRoomsDividerWall"},
                
                //Right Rooms Front Wall
                new { Position = new Vector3(42.85f, 14.5f, 25), Rotation = new Vector3(180, 180, 0), Scale = new Vector3(16.4f, 14.9f, 0.74f), ID = "RightRoomsFrontWall"},
                //Right Rooms Back Wall
                new { Position = new Vector3(42.85f, 14.5f, -25), Rotation = new Vector3(0, 180, 0), Scale = new Vector3(16.4f, 14.9f, 0.74f), ID = "RightRoomsBackWall"},
                //Right Rooms Left Wall
                new { Position = new Vector3(60, 14.5f, 0), Rotation = new Vector3(0, -90, 0), Scale = new Vector3(25.1f, 14.9f, 0.74f), ID = "RightRoomsLeftWall"},
                //Right Rooms Divider Wall
                new { Position = new Vector3(42.85f, 14.5f, 0), Rotation = new Vector3(0, 180, 0), Scale = new Vector3(16.4f, 14.9f, 0.74f), ID = "RightRoomsDividerWall"}

           };

            foreach (var w in Walls)
            {
                GameObject wallGO = InitializeModel(w.Position, w.Rotation, w.Scale, "WallBrick", "BlankWall", w.ID);

                //set the material and texture for the door
                var wallMesh = wallGO.GetComponent<MeshRenderer>();
                wallMesh.Material = _char;
                wallMesh.Overrides.MainTexture = _textureDictionary.Get("WallBrick");



                _sceneManager.ActiveScene.Add(wallGO);
            }

            var Ceilings = new[]
            {
                //Ceiling Left
                new { Position = new Vector3(-43.5f, 29.45f, 0), Rotation = new Vector3(-90, 0, 0), Scale = new Vector3(0.34f, 0.5f, 0.5f), ID = "CeilingLeft"},
                //Ceiling Right
                new { Position = new Vector3(43.5f, 29.45f, 0), Rotation = new Vector3(-90, 0, 0), Scale = new Vector3(0.34f, 0.5f, 0.5f), ID = "CeilingRight"},
                //Ceiling Khaslana
                new { Position = new Vector3(0, 29.4f, -50), Rotation = new Vector3(-90, 0, 0), Scale = new Vector3(0.5f, 0.48f, 0.5f), ID = "CeilingKhaslana"}

           };

            foreach (var w in Ceilings)
            {
                GameObject wallGO = InitializeModel(w.Position, w.Rotation, w.Scale, "WallBrick", "CeilingRooms", w.ID);

                //set the material and texture for the door
                var wallMesh = wallGO.GetComponent<MeshRenderer>();
                wallMesh.Material = _char;
                wallMesh.Overrides.MainTexture = _textureDictionary.Get("WallBrick");



                _sceneManager.ActiveScene.Add(wallGO);
            }

            //Spawn sigils Main Room
            spawnSigil(new Vector3(0, 40, 0), new Vector3(-90, 0, 0), new Vector3(0.5f, 0.5f, 0.5f), "FullMoon", "Sigil_Full");
            spawnSigil(new Vector3(20, 0, 20), new Vector3(-90, 0, 0), new Vector3(0.3f, 0.3f, 0.3f), "GibSigil", "Sigil_Full");


        }
        private void ReplaceChestModel(ChestController controller)
        {
            Scene _scene = _sceneManager.ActiveScene;
            mimicController = controller;
            //removing the chest from the scene for replacement
            _scene.Remove(controller.GameObject);
            //building the new chest with same pos/rot/scale and ID but change model based on if its normal or mimic
            if (controller.IsReal)
            {
                var newChest = InitializeModel(controller.OriginalPosition, controller.OriginalRotation, controller.OriginalScale, "chest_texture", "Chest", controller.ChestID);
               
                // give it renderer
                var renderer = newChest.AddComponent<MeshRenderer>();
                renderer.Material = _char;

                _sceneManager.ActiveScene.Add(newChest);

                spawnSigilChest();
            }
            else
            {
                var events = EngineContext.Instance.Events;
                events.Publish(new PlaySfxEvent("celeste_sound",1, false, null));
                var newChest = InitializeModel(controller.OriginalPosition, controller.OriginalRotation, controller.OriginalScale, "chest_texture", "Mimic", controller.ChestID);
               
                // give it renderer
                var renderer = newChest.AddComponent<MeshRenderer>();
                renderer.Material = _char;
                _sceneManager.ActiveScene.Add(newChest);
            }

        }
        #endregion

        private void ReplaceDoorModel(DoorController controller)
        {
            Scene _scene = _sceneManager.ActiveScene;
            //remove the object from scene
            _scene.Remove(controller.GameObject);

            //checks weather its the khaslana door or a regular door so it replaces the correct model
            if (controller.IsKhasDoor)
            {
                if (!controller.IsLocked)
                {
                    var newDoor = InitializeModel(controller.OriginalPosition, controller.OriginalRotation, controller.OriginalScale, "DoorTexture", "KhasDoorOpen", controller.DoorID);
                    // give it renderer
                    var rend = newDoor.AddComponent<MeshRenderer>();
                    rend.Material = _matt;
                    _sceneManager.ActiveScene.Add(newDoor);
                }
                else
                {
                    controller.DialogueManager = _dialogueManager;
                    return;
                }
            }
            else
            {
                if (!controller.IsLocked)
                {
                    var newDoor = InitializeModel(controller.OriginalPosition, controller.OriginalRotation, controller.OriginalScale, "DoorTexture", "OpenDoors", controller.DoorID);
                    // give it renderer
                    var rend = newDoor.AddComponent<MeshRenderer>();
                    rend.Material = _matt;
                    _sceneManager.ActiveScene.Add(newDoor);
                }
                else
                {
                    return;
                }
            }
        }
        private void spawnSigil(Vector3 position, Vector3 eulerRotationDegrees, Vector3 scale, string modelName, string sigilName)
        {
            var go = new GameObject(sigilName);
            go.Transform.TranslateTo(position);
            go.Transform.RotateEulerBy(eulerRotationDegrees * MathHelper.Pi / 180f);
            go.Transform.ScaleTo(scale);

            var model = _modelDictionary.Get(modelName);
            var texture = _textureDictionary.Get("sigil_texture");
            var meshFilter = MeshFilterFactory.CreateFromModel(model, _graphics.GraphicsDevice, 0, 0);
            go.AddComponent(meshFilter);

            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicLit;
            meshRenderer.Overrides.MainTexture = texture;
            _sceneManager.ActiveScene.Add(go);


            // Add box collider (1x1x1 cube)
            var collider = go.AddComponent<SphereCollider>();
            collider.Diameter = scale.Length();

            // Add rigidbody (Dynamic so it falls)
            var rigidBody = go.AddComponent<RigidBody>();
            rigidBody.BodyType = BodyType.Dynamic;
            rigidBody.Mass = 1.0f;
        }

        private void checkDialogue()
        {
            var events = EngineContext.Instance.Events;
            if (celesteController.hasSpoken)
            {
                //Play her dialogue sound
                events.Publish(new PlaySfxEvent("celeste_sound", 1, false, null));
                celesteController.hasSpoken = false;
            }
            if (khaslanaController.hasSpoken)
            {
                events.Publish(new PlaySfxEvent("celeste_sound", 1, false, null));
                khaslanaController.hasSpoken = false;
            }
        }

        private void spawnSigilChest()
        {
            //Mimic Room
            spawnSigil(new Vector3(-45.2f, 0, 3.2f), new Vector3(-90, 135, 0), new Vector3(0.5f, 0.5f, 0.5f), "DarkMoon", "Sigil_Dark");
        }

        private void SigilPickup()
        {
            
            // press E to pick up
            if (_newKBState.IsKeyDown(Keys.E) && !_oldKBState.IsKeyDown(Keys.E))
            {
                //gets the current camera and its pos to track range
                var scene = _sceneManager.ActiveScene;
                var CameraGO = scene.GetActiveCamera().GameObject;
                var CameraPos = CameraGO.Transform.Position;

                //checks through all the game objects in the scene
                foreach (var go in scene.GameObjects)
                {
                    //if the game object is a sigil
                    if (!go.Name.StartsWith("Sigil"))
                        continue;

                    //gets the distance between sigil and camera 
                    float range = Vector3.Distance(go.Transform.Position, CameraPos);

                    //if camera is within this range of sigil
                    if (range < 10.0f) 
                    {
                        //add sigil to inventory
                        var inventoryEvent = new GDEngine.Core.Components.InventoryEvent();
                        inventoryEvent.ItemType = ItemType.Sigil;
                        inventoryEvent.Value = 1;
                        EngineContext.Instance.Events.Publish(inventoryEvent);

                        System.Diagnostics.Debug.WriteLine("Picked up a sigil!");

                        // remove it from scene
                        scene.Remove(go);
                        break;
                    }
                }
            }
        }
    }    
}