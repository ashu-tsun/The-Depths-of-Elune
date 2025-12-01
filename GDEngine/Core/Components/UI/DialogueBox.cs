using System;
using System.Text;
using GDEngine.Core.Components;
using GDEngine.Core.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace The_Depths_of_Elune.UI
{
    //UI Element to draw an onscreen dialogue box
    //https://github.com/Andrux51/MonoGame-Tutorial-DialogBox/blob/master/MonoGame-Tutorial-DialogBox/DialogBox.cs
    public class DialogueBox : UIRenderer
    {
        #region Fields
        //Different fonts for name and dialogue
        private SpriteFont _nameFont;
        private SpriteFont _dialogueFont;
        private Texture2D _portraitTexture;
        private Rectangle _portraitRect;
        private Dictionary<string, Texture2D> _portraits;
        private bool _isVisible;
        //Initialize with nothing in it
        private string _currentText = "";
        private string _speakerName = "";
        //Box Texture
        private Texture2D _backgroundTexture;
        //Box Size
        private Rectangle _boxSize;
        #endregion

        #region Properties
        //Keep a reference of if it is visible for testing

        public bool isVisible { get; set; } = false;
        #endregion

        #region Constructor
        public DialogueBox(SpriteFont nameFont, SpriteFont dialogueFont, Texture2D dialogueTexture, GraphicsDevice graphicsDevice, Rectangle boxSize, Dictionary<string, Texture2D> portraits)
        {
            _nameFont = nameFont;
            _dialogueFont = dialogueFont;
            _boxSize = boxSize;
            _graphicsDevice = graphicsDevice;
            _backgroundTexture = dialogueTexture;

            _portraits = portraits;

            LayerDepth = UILayer.HUD;
        }
        #endregion

        #region Methods
        //Initialize
        public void Show(string speakerName, string text)
        {
            _speakerName = speakerName;
            _currentText = text;
            _isVisible = true;

            //Look for the characters name in the portraits
            if (_portraits != null && _portraits.ContainsKey(speakerName))
            {
                //Assign the right portrait
                _portraitTexture = _portraits[speakerName];

                int portraitSize = 120;
                //Create a rectangle for where the portrait will be 
                _portraitRect = new Rectangle(
                    _boxSize.X + 20,
                    _boxSize.Y + _boxSize.Height - portraitSize - 20,
                    portraitSize,
                    portraitSize
                );
            }
            else
            {
                _portraitTexture = null;
            }
        }

        //To hide everything when not speaking
        public void Hide()
        {
            _isVisible = false;
            _currentText = "";
            _speakerName = "";
        }
        #endregion

        #region Draw
        public override void Draw(GraphicsDevice device, Camera camera)
        {
            if (!_isVisible)
            {
                return;
            }

            if (_spriteBatch == null)
            {
                return;
            }


            //Dialogue box
            _spriteBatch.Draw(_backgroundTexture, _boxSize, null, Color.White,
                0f, Vector2.Zero, SpriteEffects.None, UILayer.Background);

            //Add the picture in bottom left
            if (_portraitTexture != null)
            {
                _spriteBatch.Draw(_portraitTexture, _portraitRect, null, Color.White,
                    0f, Vector2.Zero, SpriteEffects.None, UILayer.HUD);
            }

            //Hint message
            _spriteBatch.DrawString(_nameFont, "Press F to continue", new Vector2(_boxSize.X +_boxSize.Width-250,_boxSize.Y + _boxSize.Height), Color.White,
                0f, Vector2.Zero, 1f, SpriteEffects.None, UILayer.HUD);

            //NPC name
            if (!string.IsNullOrEmpty(_speakerName))
            {
                Vector2 namePos = new Vector2(_boxSize.X + 40, _boxSize.Y + 10);
                if(_speakerName.Equals("Celeste"))
                {
                    _spriteBatch.DrawString(_nameFont, _speakerName, namePos, Color.LimeGreen,
                    0f, Vector2.Zero, 1f, SpriteEffects.None, Before(LayerDepth));
                }
                else if(_speakerName.Equals("Khaslana"))
                {
                    _spriteBatch.DrawString(_nameFont, _speakerName, namePos, Color.Gold,
                   0f, Vector2.Zero, 1f, SpriteEffects.None, UILayer.HUD);
                }
                else if(_speakerName.Equals("Elysia"))
                {
                    _spriteBatch.DrawString(_nameFont, _speakerName, namePos, Color.Lavender,
                   0f, Vector2.Zero, 1f, SpriteEffects.None, UILayer.HUD);
                }
            }

            string _wrappedText = WrapText(_dialogueFont, _currentText, _boxSize.Width - 200);
            // Dialogue text
            if (!string.IsNullOrEmpty(_currentText))
            {
                Vector2 textPos = new Vector2(_boxSize.X + 170, _boxSize.Y + 60);
                
                _spriteBatch.DrawString(_dialogueFont, _wrappedText, textPos, Color.White,
                    0f, Vector2.Zero, 1f, SpriteEffects.None, UILayer.HUD);
            }
        }

        //https://stackoverflow.com/questions/15986473/how-do-i-implement-word-wrap
        public string WrapText(SpriteFont spriteFont, string text, float maxLineWidth)
        {
            //Split the line into words
            string[] words = text.Split(' ');
            StringBuilder sb = new StringBuilder();
            //Line length
            float lineWidth = 0f;
            //Space width
            float spaceWidth = spriteFont.MeasureString(" ").X;

            foreach (string word in words)
            {
                //Measure the size of the word using the specific font
                Vector2 size = spriteFont.MeasureString(word);

                //Check if adding the word to the current line would fit in the box
                if (lineWidth + size.X < maxLineWidth)
                {
                    //Add the word and adjust the line width
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    //If it doesnt fit then move it to the next line.
                    sb.Append("\n" + word + " ");
                    //Reset the line width
                    lineWidth = size.X + spaceWidth;
                }
            }

            return sb.ToString();
        }
                #endregion

    }
}