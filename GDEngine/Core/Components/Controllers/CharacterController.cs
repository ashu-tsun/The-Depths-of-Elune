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
using The_Depths_of_Elune.UI;
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
        private DialogueManager? _dialogueManager;
        private KeyboardState _oldKeyState;
        #endregion



        #region Properties
        //Value to track who is being interacted with
        public string CharID { get; set; } = "";

        //Range for interaction
        public float Range { get; set; } = 5.0f;
        //Reference to the scene
        public Scene? Scene { get => _scene; set => _scene = value; }
        //The dialogue manager, to allow different dialogue to play and render
        public DialogueManager? DialogueManager { get; set; }

        #endregion

        #region Awake
        protected override void Awake()
        {
            base.Awake();
            _oldKeyState = Keyboard.GetState();
        }
        #endregion

        #region Update
        protected override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            //Initialize properties
            _transform = GameObject.Transform;
            _mesh = GameObject.GetComponent<MeshRenderer>();
            var cameraGO = _scene.GetActiveCamera().GameObject;
            _playerTransform = cameraGO.Transform;

            //Handling errors if character or player transforms are not assigned yet
            if (_transform == null || _playerTransform == null)
                return;

            //Get the distance between player and character
            //TODO: change the keys to E after the demo code is dealt with (E gives a weapon type into inventory and breaks)
            CheckPlayerDistance();
            var inputSystem = _scene.GetSystem<InputSystem>();
            //Store keyboard state and check if the right key is pressed
            var currentKeyState = Keyboard.GetState();
            bool fPressed = currentKeyState.IsKeyDown(Keys.F) && !_oldKeyState.IsKeyDown(Keys.F);

            //Check if the player is near a character, the dialogueManager isnt null and that dialogue isnt already active
            if (_playerNearby && fPressed && DialogueManager != null && !DialogueManager.IsDialogueActive)
            {
                talk();
            }

            _oldKeyState = currentKeyState;
        }
        #endregion

        #region methods
        private void CheckPlayerDistance()
        {
            //Checks if they are assigned
            if (_playerTransform == null || _transform == null)
            {
                return;
            }

            //Calculates distance between these points
            float distance = Vector3.Distance(_transform.Position, _playerTransform.Position);

            //if the distance is less than or = to the range the player is nearby 
            _playerNearby = distance <= Range;
        }

        private void talk()
        {
            //If the dialogue is already active do nothing
            if (DialogueManager.IsDialogueActive)
            {
                return;
            }

            //Create a list to store the dialogue
            var dialogue = new List<DialogueLine>();
            if (CharID.Equals("celeste"))
            {
                //Debug
                System.Diagnostics.Debug.WriteLine("Spoken to Celeste");

                //If this is the first time speaking to the character
                if (timesSpoken == 0)
                {
                    //Create a new dialogue Line and add the dialogue to the list
                    System.Diagnostics.Debug.WriteLine("Talked to Celeste for the " + timesSpoken + " time");
                    dialogue.Add(new DialogueLine("Celeste", "Well, hello there tall creature! This place sure is busy today..."));
                    dialogue.Add(new DialogueLine("Elysia", "Umm hello miss...frog-"));
                    dialogue.Add(new DialogueLine("Celeste", "I'm Celeste! And you are?"));
                    dialogue.Add(new DialogueLine("Elysia", "Oh sorry... I'm Elysia- Wait did you say busy? Did you possibly see a brown-haired man pass by?"));
                    dialogue.Add(new DialogueLine("Celeste", "Not a clue. But that door beside me sure is making a lot of noises..."));
                    dialogue.Add(new DialogueLine("Elysia", "Do you know how to open it? It seems to be locked by something magical."));
                    dialogue.Add(new DialogueLine("Celeste", "Not a clue. But I did find these three mysterious moon puzzle piece things. Maybe they'll help you."));
                    dialogue.Add(new DialogueLine("Elysia", "Thank you, Celeste"));
                    dialogue.Add(new DialogueLine("Celeste", "Oh wait- I haven't entered the door with the whole dark moon... I can sense some monstrous energy there. Be on guard!"));
                }
                //If this is the not the first time speaking to the character, say a line to not be repeating all the
                //dialogue again.
                //Important!! Since the dialogue manager will progress with a key, you have to press the key again to 
                //close the dialogue box. This will be taken as a new "timeSpoken", so to avoid merging interactions, 
                //every odd interaction (apart from the first) will close the dialogue, every even will show the other
                //set of dialogue
                else if (timesSpoken%2 == 0) 
                {
                    System.Diagnostics.Debug.WriteLine("Talked to Celeste for the " +timesSpoken+ " time");
                    dialogue.Add(new DialogueLine("Celeste", "I don't know how to open the door, but I haven't entered the door with the whole dark moon... I can sense some monstrous energy there. Be on guard!"));

                }
                else
                {
                    DialogueManager.EndDialogue();
                }
                DialogueManager.StartDialogue(dialogue);
                timesSpoken++;
            }


            else if (CharID.Equals("khaslana"))
            {
                //Debug
                System.Diagnostics.Debug.WriteLine("Spoken to Khaslana");


                if (timesSpoken == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Talked to Khaslana for the " + timesSpoken + " time");
                    dialogue.Add(new DialogueLine("Khaslana", "Elysia? Is that you? "));
                    dialogue.Add(new DialogueLine("Khaslana", "I can't believe you found me"));
                }
                else if (timesSpoken % 2 == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Talked to Khaslana for the " + timesSpoken + " time");
                    dialogue.Add(new DialogueLine("Khaslana", "Let's get out of here!"));

                }
                else
                {
                    DialogueManager.EndDialogue();
                }
                DialogueManager.StartDialogue(dialogue);
                timesSpoken++;
            }

        }
        #endregion

    }
}