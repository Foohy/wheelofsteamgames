using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OlegEngine;
using OlegEngine.Entity;
using OlegEngine.GUI;

using OpenTK;
using OpenTK.Input;

namespace WheelOfSteamGames.Entity
{
    class ent_camera : BaseEntity 
    {
        bool Locked = true;

        public override void Init()
        {
            this.ShouldDraw = false; //Don't draw the player entity itself

            Utilities.window.Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
        }

        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                this.Locked = !this.Locked;
                Input.LockMouse = !this.Locked; 
            }
        }

        public override void Think()
        {

        }

        //Since we're going to be the default source of where to point the camera, we have our own dedicated function for it
        //This is called through reflection in the view manager
        public void CalcView()
        {
            GameWindow window = Utilities.window;

            float multiplier = 8;
            if (window.Keyboard[Key.LShift])
                multiplier = 20;

            Vector3 NewPos = this.Position;

            if (!Locked && Input.LockMouse)
            {
                //Calculate the new angle of the camera
                this.SetAngle(this.Angles + new Angle(Input.deltaY / -15f, Input.deltaX / 15f, 0));

                Vector3 Forward, Right, Up;
                this.Angles.AngleVectors(out Forward, out Up, out Right);

                //Calculate the new position
                if (window.Keyboard[Key.W])
                {
                    NewPos += Forward * (float)Utilities.ThinkTime * multiplier;
                }

                if (window.Keyboard[Key.S])
                {
                    NewPos -= Forward * (float)Utilities.ThinkTime * multiplier;
                }

                if (window.Keyboard[Key.D])
                {
                    NewPos -= Right * (float)Utilities.ThinkTime * multiplier;
                }

                if (window.Keyboard[Key.A])
                {
                    NewPos += Right * (float)Utilities.ThinkTime * multiplier;
                }

                this.SetPos(NewPos, false);
            }
            

            View.SetPos(this.Position);
            View.SetAngles(this.Angles);
        }

        public override void Draw()
        {
            
        }
    }
}
