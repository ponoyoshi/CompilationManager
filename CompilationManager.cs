using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Subtitles;

namespace StorybrewScripts
{
    public class CompilationManager : StoryboardObjectGenerator
    {
        //CONFIGURATIONS
        //FOREGROUND
        [Configurable] public bool vignette;

        //BACKGROUND
        [Configurable] public string baseBackground = "bg.jpg";

        //TEXT
        [Configurable] public Color4 textColor = Color4.White;
        [Configurable] public string textFont = "Verdana";
        [Configurable] public int textSize = 70;
        [Configurable] public TextOrigin textOrigin;
        [Configurable] public float artistNameSize = 0.2f;
        [Configurable] public float songNameSize = 0.3f;
        [Configurable] public TextStyle artistStyle;
        [Configurable] public TextStyle songStyle;
        [Configurable] public Vector2 artistPosition = new Vector2(-40, 400);
        [Configurable] public Vector2 songPosition = new Vector2(-40, 420);
        [Configurable] public bool additiveGlow = false;
        [Configurable] public int glowRadius = 0;
        [Configurable] public Color4 glowColor = Color4.White;
        [Configurable] public int outlineThickness = 0;
        [Configurable] public Color4 outlineColor = Color4.White;
        [Configurable] public int shadowThickness = 0;
        [Configurable] public Color4 shadowColor = Color4.Black;

        //////////////
        private FontGenerator font;
        private string filePath;
        private List<Section> compilationSections = new List<Section>();
        public override void Generate()
        {
            GetLayer("").CreateSprite(baseBackground).Fade(0, 0);
            Log("Compilation Manager\n- PoNo\n");
            filePath = $"{ProjectPath}/compilation.txt";

            SetFont();
		    CreateDataFiles();
            MakeData();
            GenerateCompilation();
        }
        private void CreateDataFiles()
        {
            if(!File.Exists(filePath))
            {
                using(StreamWriter streamWriter = File.CreateText(filePath))
                    streamWriter.WriteLine("startTime;endTime;artistName;songName;backgroundID;backgroundStyle");
                
                Log("Config file not found, Creating a new one..");
                return;
            }
            else Log("Config File found!");

            if(!Directory.Exists(MapsetPath + "/sb/bg"))
            {
                Directory.CreateDirectory(MapsetPath + "/sb/bg");
                Log("Created background folder");
            }
            AddDependency(filePath);
        }
        private void MakeData()
        {
            var fileLines = File.ReadAllLines(filePath);
            foreach(var line in fileLines)
            {
                string[] lineValues = line.Split(';');
                if(lineValues[0] == "startTime")
                    continue;

                compilationSections.Add(new Section(
                    int.Parse(lineValues[0]),
                    int.Parse(lineValues[1]),
                    lineValues[2],
                    lineValues[3],
                    int.Parse(lineValues[4]),
                    (BackgroundStyle)int.Parse(lineValues[5])
                ));
            }
            Log($"Added {compilationSections.Count} compilation sections");
        }
        private void GenerateCompilation()
        {
            if(vignette)
                GenerateVignette();

            foreach(var section in compilationSections)
            {
                GenerateBackground(section);
                GenerateText(section.startTime, section.endTime, section.artistName, artistPosition, artistNameSize, artistStyle);
                GenerateText(section.startTime, section.endTime, section.songName, songPosition, songNameSize, songStyle);
            }
        }
        private void GenerateText(int startTime, int endTime, string text, Vector2 position, float scale, TextStyle style)
        {
            float lineWidth = 0f;
            float lineHeight = 0f;
            int delay = 0;
            foreach(var letter in text)
            {
                var texture = font.GetTexture(letter.ToString());
                lineWidth += texture.BaseWidth * scale;
                lineHeight = Math.Max(lineHeight, texture.BaseHeight) * scale;
            }
            float letterX = textOrigin == TextOrigin.CENTRE ? position.X - (lineWidth/2) : position.X;
            float letterY = position.Y - (lineHeight/2);

            if(textOrigin == TextOrigin.RIGHT)
                letterX = position.X - lineWidth;
            
            foreach(var letter in text)
            {
                var texture = font.GetTexture(letter.ToString());
                if(!texture.IsEmpty)
                {
                    Vector2 letterPosition = new Vector2(letterX, letterY)
                        + texture.OffsetFor(OsbOrigin.Centre) * scale;

                    var sprite = GetLayer("TEXT").CreateSprite(texture.Path, OsbOrigin.Centre, letterPosition);
                    switch(style)
                    {
                        case TextStyle.BASE:
                        sprite.Fade(startTime, startTime + 1000, 0, 1);
                        sprite.Fade(endTime, endTime + 1000, 1, 0);
                        sprite.Scale(startTime, scale);
                        break;

                        case TextStyle.ACCORDION_LEFT:
                        sprite.Fade(startTime + delay, startTime + delay + 1000, 0, 1);
                        sprite.Fade(endTime, endTime + 1000, 1, 0);
                        sprite.MoveX(OsbEasing.OutExpo, startTime + delay, startTime + delay + 1000, letterPosition.X + 50, letterPosition.X);
                        sprite.Scale(startTime, scale);
                        break;

                        case TextStyle.ACCORDION_RIGHT:
                        sprite.Fade(startTime + delay, startTime + delay + 1000, 0, 1);
                        sprite.Fade(endTime, endTime + 1000, 1, 0);
                        sprite.MoveX(OsbEasing.OutExpo, startTime + delay, startTime + delay + 1000, letterPosition.X - 50, letterPosition.X);
                        sprite.Scale(startTime, scale);
                        break;

                        case TextStyle.SCALE_DOWN:
                        sprite.Fade(startTime + delay, startTime + delay + 1000, 0, 1);
                        sprite.Fade(endTime, endTime + 1000, 1, 0);
                        sprite.Scale(OsbEasing.OutExpo, startTime + delay, startTime + delay + 1000, scale * 2, scale);
                        break;
                        
                        case TextStyle.SCALE_UP:
                        sprite.Fade(startTime + delay, startTime + delay + 1000, 0, 1);
                        sprite.Fade(endTime, endTime + 1000, 1, 0);
                        sprite.Scale(OsbEasing.OutExpo, startTime + delay, startTime + delay + 1000, 0, scale);
                        break;

                        case TextStyle.ELASTIC:
                        sprite.Fade(startTime + delay, startTime + delay + 1000, 0, 1);
                        sprite.Fade(endTime, endTime + 1000, 1, 0);
                        sprite.MoveY(OsbEasing.OutBack, startTime + delay, startTime + delay + 1000, letterPosition.Y + Random(-50, 50), letterPosition.Y);
                        sprite.Scale(startTime, scale);
                        break;
                    }
                    
                }
                letterX += texture.BaseWidth * scale;
                delay += 50;
            } 
        }
        private void GenerateBackground(Section section)
        {   
            var sprite = GetLayer("BACKGROUND").CreateSprite($"sb/bg/{section.backgroundID}.jpg");
            Image backgroundBitmap = Bitmap.FromFile($"{MapsetPath}/sb/bg/{section.backgroundID}.jpg");
            switch(section.backgroundStyle)
            {
                case BackgroundStyle.BASE:
                sprite.Scale(section.startTime, 480.0/backgroundBitmap.Height);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                break;

                case BackgroundStyle.UD:
                sprite.Scale(section.startTime, 480.0/backgroundBitmap.Height * 1.05);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                sprite.MoveY(OsbEasing.InOutSine, section.startTime, section.endTime + 1000, 250, 230);
                break;

                case BackgroundStyle.DU:
                sprite.Scale(section.startTime, 480.0/backgroundBitmap.Height * 1.05);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                sprite.MoveY(OsbEasing.InOutSine, section.startTime, section.endTime + 1000, 230, 250);
                break;

                case BackgroundStyle.LR:
                sprite.Scale(section.startTime, 480.0/backgroundBitmap.Height * 1.1);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                sprite.MoveX(OsbEasing.InOutSine, section.startTime, section.endTime + 1000, 310, 330);
                break;

                case BackgroundStyle.RL:
                sprite.Scale(section.startTime, 480.0/backgroundBitmap.Height * 1.1);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                sprite.MoveX(OsbEasing.InOutSine, section.startTime, section.endTime + 1000, 330, 310);
                break;

                case BackgroundStyle.SU:
                sprite.Scale(section.startTime, section.endTime + 1000, 480.0/backgroundBitmap.Height, 480.0/backgroundBitmap.Height * 1.1);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                break;

                case BackgroundStyle.SD:
                sprite.Scale(section.startTime, section.endTime + 1000, 480.0/backgroundBitmap.Height * 1.1, 480.0/backgroundBitmap.Height);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                break;

                case BackgroundStyle.SUR:
                sprite.Scale(section.startTime, section.endTime + 1000, 480.0/backgroundBitmap.Height * 1.1, 480.0/backgroundBitmap.Height * 1.2);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                sprite.Rotate(OsbEasing.OutSine, section.startTime, section.endTime, 0.05, -0.05);
                break;

                case BackgroundStyle.SDR:
                sprite.Scale(section.startTime, section.endTime + 1000, 480.0/backgroundBitmap.Height * 1.2, 480.0/backgroundBitmap.Height * 1.1);
                sprite.Fade(section.startTime, section.startTime + 1000, 0, 1);
                sprite.Fade(section.endTime, section.endTime + 1000, 1, 0);
                sprite.Rotate(OsbEasing.OutSine, section.startTime, section.endTime, -0.05, 0.05);
                break;
            }
            backgroundBitmap.Dispose();
        }
        private void GenerateVignette()
        {
            if(!File.Exists(MapsetPath + "/sb/v.png"))
            {
                using(var client = new WebClient())
                    client.DownloadFile("https://i.imgur.com/jXeKpkk.png", MapsetPath + "/sb/v.png");
            }


            var sprite = GetLayer("VIGNETTE").CreateSprite("sb/v.png");
            int startTime = compilationSections[0].startTime;
            int endTime = compilationSections[compilationSections.Count-1].endTime;
            sprite.Fade(startTime, startTime + 1000, 0, 1);
            sprite.Fade(endTime, endTime + 1000, 1, 0);
            sprite.Scale(startTime, 480.0/1080); 
        }
        private void SetFont()
        {
            font = LoadFont("sb/f", new FontDescription()
            {
                FontPath = textFont,
                FontSize = textSize,
                Color = textColor,
                FontStyle = FontStyle.Regular
            },
            new FontGlow()
            {
                Radius = additiveGlow ? 0 : glowRadius,
                Color = glowColor,
            },
            new FontOutline()
            {
                Thickness = outlineThickness,
                Color = outlineColor,
            },
            new FontShadow()
            {
                Thickness = shadowThickness,
                Color = shadowColor,
            });
        }
        private class Section
        {
            public int startTime;
            public int endTime;
            public string artistName;
            public string songName;
            public int backgroundID;
            public BackgroundStyle backgroundStyle;
            public int duration;
            public Section(int startTime, int endTime, string artistName, string songName, int backgroundID, BackgroundStyle backgroundStyle)
            {
                this.startTime = startTime;
                this.endTime = endTime;
                this.artistName = artistName;
                this.songName = songName;
                this.backgroundID = backgroundID;
                this.backgroundStyle = backgroundStyle;
                this.duration = endTime - startTime;
            }
        }
        public enum BackgroundStyle
        {
            BASE,
            UD,
            DU,
            LR,
            RL,
            SU,
            SD,
            SUR,
            SDR
        }
        public enum TextStyle
        {
            BASE,
            ELASTIC,
            ACCORDION_LEFT,
            ACCORDION_RIGHT,
            SCALE_UP,
            SCALE_DOWN
        }
        public enum TextOrigin
        {
            LEFT,
            CENTRE,
            RIGHT
        }
    }
}
