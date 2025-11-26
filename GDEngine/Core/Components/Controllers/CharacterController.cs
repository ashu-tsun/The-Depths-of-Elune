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
    public sealed class CharacterController : Component
    {
        #region Fields
        private Transform? _transform;
        private MeshRenderer? _mesh;
        private Scene? _scene;
        private bool _playerNearby = false;
        private bool _hasSpoken = false;
        private int timesSpoken = 0;
        private Transform? _playerTransform;
        #endregion

   

        #region Properties
        //to check chest if it just opened
        public bool HasJustSpoken { get; set; } = false;

        public string CharID { get; set; } = "";

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
            if (!_hasSpoken && _playerNearby && Keyboard.GetState().IsKeyDown(Keys.F))
            {
                talk();
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

        private void talk()
        {
            _hasSpoken = true;
            HasJustSpoken = true;
            if (CharID.Equals("celeste"))
            {
                //test REMOVE LATER
                System.Diagnostics.Debug.WriteLine("Spoken to Celeste");


                if (timesSpoken == 0)
                {
                    timesSpoken++;
                    //show dialogue
                    System.Diagnostics.Debug.WriteLine("Hi i'm celeste");
                }
            }

            else if (CharID.Equals("khaslana"))
            {
                //test REMOVE LATER
                System.Diagnostics.Debug.WriteLine("Spoken to Khaslana");


                if (timesSpoken == 0)
                {
                    timesSpoken++;
                    //show dialogue
                    System.Diagnostics.Debug.WriteLine("Elysia?");
                }
            }

        }
        #endregion



        #region Helpers
        private void showDialogue()
        {

        }
        #endregion
    }
}