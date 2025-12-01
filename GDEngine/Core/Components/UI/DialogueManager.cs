using System;
using System.Collections.Generic;
using GDEngine.Core.Components;
using Microsoft.Xna.Framework.Input;
using The_Depths_of_Elune.UI;
namespace The_Depths_of_Elune.UI
{
    //Manages how dialogue occurs, progresses and ends
    //https://www.reddit.com/r/gamedev/comments/1gqk7nx/what_is_the_correct_method_to_handle_dialogue/
    public class DialogueManager : Component
    {
        #region Fields
        //A custom UI component to display dialogue
        private DialogueBox _dialogueBox;
        //Use a queue to load up dialogue that will be said in a sequence
        private Queue<DialogueLine> _currentDialogue = new Queue<DialogueLine>();
        private bool _isDialogueActive = false;
        //Store keyboard states to allow moving through dialogue
        private KeyboardState _oldKeyState;
        private KeyboardState _newKeyState;
        #endregion

        #region Properties
        //Check if the dialogue is currently appearing
        public bool IsDialogueActive => _isDialogueActive;
        #endregion

        #region Constructor
        public DialogueManager(DialogueBox dialogueBox)
        {
            _dialogueBox = dialogueBox;
            _oldKeyState = Keyboard.GetState();
        }
        #endregion

        #region Update
        protected override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            //Store keyboard state
            _newKeyState = Keyboard.GetState();

            //If the dialogue is already appearing then dont do anything
            //update keyboard state
            if (!_isDialogueActive)
            {
                _oldKeyState = _newKeyState;
                return;
            }
            //Check if the player is pressing the key
            if (_newKeyState.IsKeyDown(Keys.F) && !_oldKeyState.IsKeyDown(Keys.F))
            {
                //Make sure there is dialogue imported into the queue, then start showing it
                if (_currentDialogue.Count > 0)
                {
                    ShowNextLine();
                }
                //If there is no more dialogue then stop showing the dialogue box
                else
                {
                    EndDialogue();
                }
            }
            //Update keyboard state
            _oldKeyState = _newKeyState;
        }
        #endregion

        #region Methods

        public void StartDialogue(List<DialogueLine> dialogue)
        {
            //If there was no dialogue imported then return
            if (dialogue == null || dialogue.Count == 0)
            {
                return;
            }

            //Make sure there is no dialogue currently being displayed
            _currentDialogue.Clear();

            //Load in the dialogue (from the character controller)
            foreach (var line in dialogue)
            {
                _currentDialogue.Enqueue(line);
            }
            _isDialogueActive = true;
            //Then move to show the dialogue line
            ShowNextLine();
        }

        //If reached the end of the dialogue
        public void EndDialogue()
        {
            _isDialogueActive = false;
            _currentDialogue.Clear();
            _dialogueBox.Hide();
        }

        //Moving from line to line
        private void ShowNextLine()
        {
            //If reached the end of the dialogue
            if (_currentDialogue.Count == 0)
            {
                EndDialogue();
                return;
            }

            //Remove the previous line and then show the text (keeping track of speaker -> text)
            var line = _currentDialogue.Dequeue();
            _dialogueBox.Show(line.Character, line.Text);

        }
        #endregion
    }

    //A single line of dialogue
    public class DialogueLine
    {
        //Who is speaking
        public string Character { get; set; }
        //What they are saying
        public string Text { get; set; }
        //Contructor
        public DialogueLine(string character, string text)
        {
            Character = character;
            Text = text;
        }
    }
}