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
    public sealed class DoorController : Component
    {
        #region Fields
        private Transform? _transform;
        private Transform? _playerTransform;
        private Scene? _scene;

        private bool _playerNearby = false;
        private bool _isOpened = false;

        //check if door is locked
        public bool IsLocked { get; set; } = false;

        //differenciates the regular doors and the khaslana room door
        public bool IsKhasDoor { get; set; } = false;
        #endregion

        #region Events

        public Action<DoorController>? OnReplaceModel;

        #endregion

        #region Properties

        //to store the original pos/rotation/scale so when switching models it will be same
        public Vector3 OriginalPosition;
        public Vector3 OriginalRotation;
        public Vector3 OriginalScale;

        public string DoorID { get; set; } = "";

        //to check door if it just opened
        public bool HasJustOpened { get; set; } = false;

        //range for interacting with door
        public float Range { get; set; } = 10.0f;

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

            //players transform from active camera for distance checks 
            _transform = GameObject.Transform;
            var cameraGO = _scene.GetActiveCamera().GameObject;
            _playerTransform = cameraGO.Transform;

            //handling errors if door or player transforms are not assigned yet
            if (_transform == null || _playerTransform == null)
                return;

            //getting the distance between door and camera so when player is near it will only open it
            CheckPlayerDistance();
            var inputSystem = _scene.GetSystem<InputSystem>();
            if (!_isOpened && _playerNearby && Keyboard.GetState().IsKeyDown(Keys.E))
            {
                OpenDoor();
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

        private void OpenDoor()
        {
            //makes sure the door is not open already
            if (_isOpened)
                return;

            //get a reference to the player game object with inventory so we can check the count of sigils recieved to deem door openable
            var playerGO = _scene.GameObjects.FirstOrDefault(go => go.Name == "PlayerParent");
            var inventory = playerGO?.GetComponent<InventoryComponent>();
            

            //keeps track of the sigil count in players inventory
            int sigilCount = 0;
            if (inventory != null)
            {
                sigilCount = inventory.GetCount(ItemType.Sigil);
            }         

            //now it will unlock once all 3 sigils are picked up
            if(IsLocked && sigilCount == 3 && IsKhasDoor)
            {
                _isOpened = true;
                HasJustOpened = true;

                //test REMOVE LATER
                System.Diagnostics.Debug.WriteLine("Door opened");


                ChangeToOpenedModel();
            }
            if (!IsLocked)
            {
                _isOpened = true;
                HasJustOpened = true;

                //test REMOVE LATER
                System.Diagnostics.Debug.WriteLine("Door opened");


                ChangeToOpenedModel();
            }
            else
            {
                if(IsKhasDoor)
                {
                    System.Diagnostics.Debug.WriteLine("Door is locked. You need 3 sigils, (you have " + sigilCount + " sigils)");
                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Door is locked.");
                }
               
            }
                
        }
        #endregion


        #region Helpers
        
        private void ChangeToOpenedModel()
        {
            //triggers the OnReplaceModel event to replace the model
            OnReplaceModel?.Invoke(this);
        }

        #endregion
    }
}