using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GDEngine.Core.Collections;
using GDEngine.Core.Entities;
using GDEngine.Core.Factories;
using GDEngine.Core.Input.Data;
using GDEngine.Core.Rendering;
using GDEngine.Core.Services;
using GDEngine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace GDEngine.Core.Components.Controllers
{
    public sealed class ChestController : Component
    {
        #region Fields
        private Transform? _transform;
        private MeshRenderer? _mesh;
        private Scene? _scene;
        private bool _playerNearby = false;
        private bool _isOpened = false;

        private Transform? _playerTransform;
        #endregion

        #region Events

        public Action<ChestController>? OnReplaceModel;

        #endregion

        #region Properties

        //to store the original pos/rotation/scale so when switching models it will be same
        public Vector3 OriginalPosition;
        public Vector3 OriginalRotation;
        public Vector3 OriginalScale;
        public string ChestID { get; set; } = ""; 

        //If true this is the real chest that gives a sigil.
        //If false this chest is a mimic and kills the player.
        public bool IsReal { get; set; } = false;
        
        //to check chest if it just opened
        public bool HasJustOpened { get; set; } = false;

        public bool gameLost { get; set; } = false;

        //temp boolean hardcoding to be able to unlock door.
        public static bool RecievedSigil = false;

        //range for interacting with chest
        public float Range { get; set; } = 5.0f;

        public Scene? Scene { get => _scene; set => _scene = value; }

        #endregion

        #region Awake
        protected override void Awake()
        {
           

            base.Awake();
        }
        #endregion

        #region Update
        protected override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            //TODO : possibly change it from active camera to player object in future
            //players transform from active camera for distance checks 
            _transform = GameObject.Transform;
            _mesh = GameObject.GetComponent<MeshRenderer>();
            var cameraGO = _scene.GetActiveCamera().GameObject;
            _playerTransform = cameraGO.Transform;

            //handling errors if chest or player transforms are not assigned yet
            if (_transform == null || _playerTransform == null)
                return;

            //getting the distance between chest and camera so when player is near it will only open then
            //TODO: change the keys to E after the demo code is dealt with (E gives a weapon type into inventory and breaks)
            CheckPlayerDistance();
            var inputSystem = _scene.GetSystem<InputSystem>();
            if (!_isOpened && _playerNearby && Keyboard.GetState().IsKeyDown(Keys.F))
            {
                OpenChest();
            }


        }
        #endregion

        #region methods
        private void CheckPlayerDistance()
        {
            //checks if they are assigned
            if (_playerTransform == null || _transform == null)
            {
                return;
            }

            //calculates distannce between these points
            float distance = Vector3.Distance(_transform.Position, _playerTransform.Position);

            //if the distance is less than or = to the range the player is nearby 
            _playerNearby = distance <= Range;
        }

        private void OpenChest()
        {
            //makes sure the chest is not open already
            if (_isOpened)
                return;

            _isOpened = true;
            HasJustOpened = true;

            //test REMOVE LATER
            System.Diagnostics.Debug.WriteLine("Chest opened");


            if (IsReal)
            {
                GiveSigil();
                //test REMOVE LATER
                //System.Diagnostics.Debug.WriteLine("YOU GOT A SIGIL");
            }
            else
            {
                TriggerDeath();
                //test REMOVE LATER
                //System.Diagnostics.Debug.WriteLine("MIMIC YOU DIE");
            }

            ChangeToOpenedModel();
        }
        #endregion



        #region Helpers
        private void GiveSigil()
        {

            RecievedSigil = true;
            System.Diagnostics.Debug.WriteLine("Sigil recieved!");
            
        }

        private void TriggerDeath()
        {
            // TODO: death
            gameLost = true;
            System.Diagnostics.Debug.WriteLine("ahhhh");
        }

        private void ChangeToOpenedModel()
        {
            //triggers the OnReplaceModel event to replace the model
            OnReplaceModel?.Invoke(this);
        }
            #endregion
     }
}