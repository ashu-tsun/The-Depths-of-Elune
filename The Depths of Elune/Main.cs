using System;
using System.Collections.Generic;
using GDEngine.Core;
using GDEngine.Core.Audio;
using GDEngine.Core.Audio.Events;
using GDEngine.Core.Collections;
using GDEngine.Core.Components;
using GDEngine.Core.Components.Controllers;
using GDEngine.Core.Debug;
using GDEngine.Core.Entities;
using GDEngine.Core.Events;
using GDEngine.Core.Events.Types.Camera;
using GDEngine.Core.Extensions;
using GDEngine.Core.Factories;
using GDEngine.Core.Input.Data;
using GDEngine.Core.Input.Devices;
using GDEngine.Core.Orchestration;
using GDEngine.Core.Rendering;
using GDEngine.Core.Rendering.UI;
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
using The_Depths_of_Elune;
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
        private UIStatsRenderer _uiStatsRenderer;
        private int _dummyHealth;
        private KeyboardState _newKBState, _oldKBState;
        private int _damageAmount;
        private SoundEffectInstance _soundEffectInstance;
        private SoundEffect _soundEffect;
        private Material _char;
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

            // Scene to hold game objects
            InitializeScene();

            // Camera, UI, Menu, Physics, Rendering etc.
            InitializeSystems();

            // All cameras we want in the game are loaded now and one set as active
            InitializeCameras();

            //game manager, camera changer, FSM, AI
            InitializeManagers();

            // Setup world
            int scale = 300;
            InitializeSkyParent();
            InitializeSkyBox(scale);
            InitializeCollidableGround(scale);

            // Setup player
            InitializePlayer();

            #region Demos
            DemoPlaySoundEffect();

            // Camera-demos
            InitializeAnimationCurves();
            LoadFromJSON();
            InitializeCharacters();
            DemoOrchestrationSystem();
            #endregion


            // Setup renderers after all game objects added since ui text may use a gameobject as target
            InitializeUI();

            // Setup menu
            //InitializeMenu();

            #endregion

            base.Initialize();
        }

        private void InitializeManagers()
        {
            var go = new GameObject("Camera Manager");
            go.AddComponent<CameraChangeEventListener>();
            _scene.Add(go);
        }

        private void DemoPlaySoundEffect()
        {
            _soundEffect = _soundDictionary.Get("secret_door");
        }



        private void InitializePlayer()
        {
            GameObject player = InitializeModel(new Vector3(0, 0, 0),
                new Vector3(-90, 0, 0),
                new Vector3(1, 1, 1), "sky", "celeste", AppData.PLAYER_NAME);

            var simpleDriveController = new SimpleDriveController();
            player.AddComponent(simpleDriveController);

            // Listen for damage events on the player
            player.AddComponent<DamageEventListener>();

            // Adds an inventory to the player
            player.AddComponent<InventoryComponent>();
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
                LightingEnabled = true,
                PreferPerPixelLighting = true,
                VertexColorEnabled = false
            };

            _char = new Material(litBasicEffect);
            _char.StateBlock = RenderStates.Opaque3D().WithRaster(new RasterizerState { CullMode = CullMode.None });
            _char.SamplerState = Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp;

            #endregion
        }

        private void InitializeScene()
        {
            // Make a scene that will store all drawn objects and systems for that level
            _scene = new Scene(EngineContext.Instance, "Main Level");
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
            _scene.Add(orchestrationSystem);

        }

        private void InitializeAudioSystem()
        {
            _scene.Add(new AudioSystem(_soundDictionary));
        }

        private void InitializePhysicsDebugSystem(bool isEnabled)
        {
            var physicsDebugRenderer = _scene.AddSystem(new PhysicsDebugRenderer());

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
            var physicsSystem = _scene.AddSystem(new PhysicsSystem());
            physicsSystem.Gravity = AppData.GRAVITY;
        }

        private void InitializeEventSystem()
        {
            _scene.Add(new EventSystem(EngineContext.Instance.Events));
        }

        private void InitializeCameraAndRenderSystems()
        {
            var cameraSystem = new CameraSystem(_graphics.GraphicsDevice, -100);
            _scene.Add(cameraSystem);

            var renderSystem = new RenderSystem(-100);
            _scene.Add(renderSystem);

            var uiRenderSystem = new UIRenderSystem(100);
            _scene.Add(uiRenderSystem); // draws in PostRender after RenderingSystem (order = -100)
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

            _scene.Add(inputSystem);
        }

        private void InitializeCameras()
        {
            #region Static birds-eye camera
            _cameraGO = new GameObject(AppData.CAMERA_NAME_STATIC_BIRDS_EYE);
            _camera = _cameraGO.AddComponent<Camera>();
            _camera.FieldOfView = MathHelper.ToRadians(80);
            //ISRoT
            _cameraGO.Transform.RotateEulerBy(new Vector3(MathHelper.ToRadians(-90), 0, 0));
            _cameraGO.Transform.TranslateTo(Vector3.UnitY * 50);

            // _cameraGO.AddComponent<MouseYawPitchController>();

            _scene.Add(_cameraGO);

            // _camera.FieldOfView
            //TODO - add camera
            #endregion

            #region Third-person camera
            _cameraGO = new GameObject(AppData.CAMERA_NAME_THIRD_PERSON);
            _camera = _cameraGO.AddComponent<Camera>();

            var thirdPersonController = new ThirdPersonController();
            thirdPersonController.TargetName = AppData.PLAYER_NAME;
            thirdPersonController.ShoulderOffset = 0;
            thirdPersonController.FollowDistance = 50;
            thirdPersonController.RotationDamping = 20;
            _cameraGO.AddComponent(thirdPersonController);
            _scene.Add(_cameraGO);
            #endregion

            #region First-person camera
            var position = new Vector3(0, 5, 25);

            //camera GO
            _cameraGO = new GameObject(AppData.CAMERA_NAME_FIRST_PERSON);
            //set position 
            _cameraGO.Transform.TranslateTo(position);
            //add camera component to the GO
            _camera = _cameraGO.AddComponent<Camera>();
            _camera.FarPlane = 1000;
            ////feed off whatever screen dimensions you set InitializeGraphics
            _camera.AspectRatio = (float)_graphics.PreferredBackBufferWidth / _graphics.PreferredBackBufferHeight;
            _cameraGO.AddComponent<KeyboardWASDController>();
            _cameraGO.AddComponent<MouseYawPitchController>();

            // Add it to the scene
            _scene.Add(_cameraGO);
            #endregion

            // Set the active camera by finding and getting its camera component
            var theCamera = _scene.Find(go => go.Name.Equals(AppData.CAMERA_NAME_FIRST_PERSON)).GetComponent<Camera>();
            ////Obviously, since we have _camera we could also just use the line below

            _scene.SetActiveCamera(theCamera);
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
            _scene.Add(_skyParent);
        }

        private void InitializeSkyBox(int scale = 50)
        {
            GameObject gameObject = null;
            MeshFilter meshFilter = null;
            MeshRenderer meshRenderer = null;

            // Find the sky parent object to attach sky to so sky rotates
            GameObject skyParent = _scene.Find((GameObject go) => go.Name.Equals("SkyParent"));

            // back
            gameObject = new GameObject("back");
            gameObject.Transform.ScaleTo(new Vector3(scale, scale, 1));
            gameObject.Transform.TranslateTo(new Vector3(0, 0, -scale / 2));
            meshFilter = MeshFilterFactory.CreateQuadTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicUnlit;
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("sky");
            _scene.Add(gameObject);

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
            _scene.Add(gameObject);

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
            _scene.Add(gameObject);

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
            _scene.Add(gameObject);

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
            _scene.Add(gameObject);

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

            _scene.Add(gameObject);
        }

        private void InitializeUI()
        {
            InitializeUIReticleRenderer();
        }



        private void InitializeUIReticleRenderer()
        {
            var uiGO = new GameObject("HUD");

            var reticleAtlas = _textureDictionary.Get("star");
            var uiFont = _fontDictionary.Get("mouse_reticle_font");

            // Reticle (cursor): always on top
            var reticle = new UIReticleRenderer(reticleAtlas);
            reticle.Origin = reticleAtlas.GetCenter();
            reticle.SourceRectangle = null;
            reticle.Scale = new Vector2(0.1f, 0.1f);
            reticle.RotationSpeedDegPerSec = 55;
            reticle.LayerDepth = UILayer.Cursor;
            uiGO.AddComponent(reticle);

            // Distance/health lines under the cursor
            //var waypointObject = _scene.Find((go) => go.Name.Equals("test crate textured cube"));
            //var cameraObject = _scene.Find(go => go.Name.Equals("First person camera"));

            ////no first person camera
            //if (cameraObject != null)
            //{

            //    Func<IEnumerable<string>> linesProvider = () =>
            //    {
            //        var distToWaypoint = Vector3.Distance(
            //            cameraObject.Transform.Position,
            //            waypointObject.Transform.Position);
            //        var hp = _dummyHealth;
            //        return new[]
            //        {
            //        $"Dist: {distToWaypoint:F2} m",
            //        $"Health:   {hp}"
            //        };
            //    };

            //    // Text anchored at mouse, slightly below the reticle
            //    var text = new UITextRenderer(uiFont);
            //    //  text.PositionProvider = () => Mouse.GetState().Position.ToVector2();

            //    text.PositionProvider = () => new Vector2(_graphics.PreferredBackBufferWidth / 2,
            //                                              _graphics.PreferredBackBufferHeight / 2);

            //    text.Anchor = TextAnchor.Center;
            //    text.Offset = new Vector2(0, 50);
            //    text.FallbackColor = Color.White;
            //    text.DropShadow = true;
            //    text.ShadowColor = Color.Black;

            //    // Place HUD text below the cursor in the same pass
            //    text.LayerDepth = UILayer.HUD;

            //    text.TextProvider = () => string.Join("\n", linesProvider());

            //    uiGO.AddComponent(text);
            //}
            _scene.Add(uiGO);

            // Hide mouse since reticle will take its place
            IsMouseVisible = false;
        }


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

            _scene.Add(gameObject);

            return gameObject;
        }

        protected override void Update(GameTime gameTime)
        {
            //call time update
            #region Core
            Time.Update(gameTime);

            //Time.TimeScale = 0;

            //update Scene
            _scene.Update(Time.DeltaTimeSecs);

            #endregion

            DemoStuff();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);

            //just as called update, we now have to call draw to call the draw in the renderingsystem
            _scene.Draw(Time.DeltaTimeSecs);

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

        #region Demo Methods (remove in the game)

        //Keep this for reference to stuff we wanna add
        private void DemoStuff()
        {
            _newKBState = Keyboard.GetState();
            DemoEventPublish();
            DemoCameraSwitch();
            DemoToggleFullscreen();
            DemoAudioSystem();
            DemoOrchestrationSystem();
            _oldKBState = _newKBState;
        }

        private void DemoOrchestrationSystem()
        {
            var orchestrator = _scene.GetSystem<OrchestrationSystem>().Orchestrator;

            bool isPressed = _newKBState.IsKeyDown(Keys.O) && !_oldKBState.IsKeyDown(Keys.O);
            if (isPressed)
            {
                orchestrator.Build("my first sequence")
                    .WaitSeconds(2)
                    .Publish(new CameraChangeEvent(AppData.CAMERA_NAME_FIRST_PERSON))
                    .WaitSeconds(2)
                    .Publish(new PlaySfxEvent("SFX_UI_Click_Designed_Pop_Generic_1", 1, false, null))
                    .Register();

                orchestrator.Start("my first sequence", _scene, EngineContext.Instance);
            }

            bool isIPressed = _newKBState.IsKeyDown(Keys.I) && !_oldKBState.IsKeyDown(Keys.I);
            if (isIPressed)
                orchestrator.Pause("my first sequence");

            bool isPPressed = _newKBState.IsKeyDown(Keys.P) && !_oldKBState.IsKeyDown(Keys.P);
            if (isPPressed)
                orchestrator.Resume("my first sequence");
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

        private void DemoToggleFullscreen()
        {
            bool togglePressed = _newKBState.IsKeyDown(Keys.F5) && !_oldKBState.IsKeyDown(Keys.F5);
            if (togglePressed)
                _graphics.ToggleFullScreen();
        }

        private void DemoCameraSwitch()
        {
            var events = EngineContext.Instance.Events;

            bool isFirst = _newKBState.IsKeyDown(Keys.D1) && !_oldKBState.IsKeyDown(Keys.D1);
            if (isFirst)
            {
                events.Post(new CameraChangeEvent(AppData.CAMERA_NAME_FIRST_PERSON));
                events.Publish(new PlaySfxEvent("SFX_UI_Click_Designed_Pop_Generic_1",
                  1, false, null));
            }

            bool isThird = _newKBState.IsKeyDown(Keys.D2) && !_oldKBState.IsKeyDown(Keys.D2);
            if (isThird)
            {
                events.Post(new CameraChangeEvent(AppData.CAMERA_NAME_THIRD_PERSON));
                events.Publish(new PlaySfxEvent("SFX_UI_Click_Designed_Pop_Mallet_Open_1",
                1, false, null));
            }
        }

        private void DemoEventPublish()
        {
            // F2: publish a test DamageEvent
            if (_newKBState.IsKeyDown(Keys.F6) && !_oldKBState.IsKeyDown(Keys.F6))
            {
                // Simple “debug” damage example
                var hitPos = new Vector3(0, 5, 0); //some fake position
                _damageAmount++;

                var damageEvent = new DamageEvent(_damageAmount, DamageEvent.DamageType.Strength,
                    "Plasma rifle", AppData.PLAYER_NAME, hitPos, false);

                EngineContext.Instance.Events.Post(damageEvent);
            }

            // Raise inventory event
            if (_newKBState.IsKeyDown(Keys.E) && !_oldKBState.IsKeyDown(Keys.E))
            {
                var inventoryEvent = new InventoryEvent();
                inventoryEvent.ItemType = ItemType.Weapon;
                inventoryEvent.Value = 10;
                EngineContext.Instance.Events.Publish(inventoryEvent);
            }

            if (_newKBState.IsKeyDown(Keys.L) && !_oldKBState.IsKeyDown(Keys.L))
            {
                var inventoryEvent = new InventoryEvent();
                inventoryEvent.ItemType = ItemType.Lore;
                inventoryEvent.Value = 0;
                EngineContext.Instance.Events.Publish(inventoryEvent);
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

        private void DemoCollidablePrimitiveObject(Vector3 position, Vector3 scale)
        {
            GameObject gameObject = null;
            MeshFilter meshFilter = null;
            MeshRenderer meshRenderer = null;

            gameObject = new GameObject("test crate textured cube");
            gameObject.Transform.TranslateTo(position);
            gameObject.Transform.ScaleTo(scale);

            meshFilter = MeshFilterFactory.CreateCubeTexturedLit(_graphics.GraphicsDevice);
            gameObject.AddComponent(meshFilter);

            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Material = _matBasicLit; //enable lighting for the crate
            meshRenderer.Overrides.MainTexture = _textureDictionary.Get("crate1");

            _scene.Add(gameObject);

            // Add box collider (1x1x1 cube)
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.Size = scale;
            collider.Center = new Vector3(0, 0, 0);

            // Add rigidbody (Dynamic so it falls)
            var rigidBody = gameObject.AddComponent<RigidBody>();
            rigidBody.BodyType = BodyType.Dynamic;
            rigidBody.Mass = 1.0f;
            rigidBody.UseGravity = true;

            //#region Demo - Curve and Input
            //var posRotController = new PositionRotationController
            //{
            //    RotationCurve = _animationRotationCurve,
            //    PositionCurve = _animationPositionCurve
            //};
            //gameObject.AddComponent(posRotController);

            ////demo the new input system support for keyboard, mouse and gamepad
            //gameObject.AddComponent(new InputReceiverComponent());

            //#endregion

            //  testCrateGO.Layer = LayerMask.World;
        }

        private void InitializeCharacters()
        {
            //celeste
            GameObject celeste = new GameObject("celeste");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            celeste = InitializeModel(new Vector3(16, -0.5f, -15), new Vector3(-90, -180, 0), new Vector3(1, 1, 1), "celeste_texture", "celeste", "celeste");


            var celesteController = celeste.AddComponent<CharacterController>();
            celesteController.HasJustSpoken = false;
            celesteController.Scene = _scene;
            celesteController.CharID = "celeste";

            var textureRenderer = celeste.AddComponent<MeshRenderer>();
            textureRenderer.Material = _char;

            // Per-object properties via the overrides block
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("celeste_texture");
            _scene.Add(celeste);

            //Khaslana
            GameObject khaslana = new GameObject("khaslana");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            khaslana = InitializeModel(new Vector3(20, 3f, -10), new Vector3(-90, 0, 0), new Vector3(1, 0.8f, 2), "khaslana_texture", "khaslana", "khaslana");

            var khaslanaController = khaslana.AddComponent<CharacterController>();
            khaslanaController.HasJustSpoken = false;
            khaslanaController.Scene = _scene;
            khaslanaController.CharID = "khaslana";

            textureRenderer = khaslana.AddComponent<MeshRenderer>();
            textureRenderer.Material = _char;

            // Per-object properties via the overrides block
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("khaslana_texture");
            _scene.Add(khaslana);

            //mimic         
            GameObject mimic = new GameObject("mimic");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            mimic = InitializeModel(new Vector3(-50, -0.5f, 23), new Vector3(-90, 0, 0), new Vector3(3, 3, 3), "chest_texture", "Mimic", "mimic");

            textureRenderer = mimic.AddComponent<MeshRenderer>();
            textureRenderer.Material = _char;

            // Per-object properties via the overrides block
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("chest_texture");
            _scene.Add(mimic);

            //chest        
            GameObject chest = new GameObject("chest");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            chest = InitializeModel(new Vector3(-50, -0.5f, 18), new Vector3(-90, 0, -30), new Vector3(3, 3, 3), "chest_texture", "Chest", "chest");

            textureRenderer = chest.AddComponent<MeshRenderer>();
            textureRenderer.Material = _char;

            // Per-object properties via the overrides block
            textureRenderer.Overrides.MainTexture = _textureDictionary.Get("chest_texture");
            _scene.Add(chest);

            //chest closed      
            GameObject chestClosed = new GameObject("chestClosed");

            // A unit quad facing +Z (the factory already supplies lit quad with UVs)
            // defines a set of closed chests with ids and weather its a mimic or not
            var chestsClosed = new[]
           {
                new { Position = new Vector3(-40, -0.5f, 13), Rotation = new Vector3(-90, 0, 0), Scale = new Vector3(3,3,3), ID = "Chest_01", IsMimic = false, HasJustOpened = false },
                new { Position = new Vector3(-40, -0.5f, 18), Rotation = new Vector3(-90, 0, 0), Scale = new Vector3(3,3,3), ID = "Chest_02", IsMimic = true , HasJustOpened = false},
                new { Position = new Vector3(-40, -0.5f, 23), Rotation = new Vector3(-90, 0, 0), Scale = new Vector3(3,3,3), ID = "Chest_03", IsMimic = true, HasJustOpened = false}
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

                _scene.Add(chestGO);

                // Per-object properties via the overrides block
                textureRenderer.Overrides.MainTexture = _textureDictionary.Get("chest_texture");

            }
        }
        private void ReplaceChestModel(ChestController controller)
        {
            //removing the chest from the scene for replacement
            _scene.Remove(controller.GameObject);

            //building the new chest with same pos/rot/scale and ID but change model based on if its normal or mimic
            if (controller.IsReal)
            {
                var newChest = InitializeModel(controller.OriginalPosition, controller.OriginalRotation, controller.OriginalScale, "chest_texture", "Chest", controller.ChestID);
               
                // give it renderer
                var renderer = newChest.AddComponent<MeshRenderer>();
                renderer.Material = _char;

                _scene.Add(newChest);
            }
            else
            {
                var events = EngineContext.Instance.Events;
                events.Publish(new PlaySfxEvent("Bad Ending",1, false, null));
                var newChest = InitializeModel(controller.OriginalPosition, controller.OriginalRotation, controller.OriginalScale, "chest_texture", "Mimic", controller.ChestID);
               
                // give it renderer
                var renderer = newChest.AddComponent<MeshRenderer>();
                renderer.Material = _char;

                _scene.Add(newChest);
            }

        }
        #endregion
    }

    
}