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
        Vector2d CamAngle = new Vector2d();
        bool Locked = true;

        public override void Init()
        {
            this.ShouldDraw = false; //Don't draw the player entity itself

        }

        public override void Think()
        {

        }

        //Since we're going to be the default source of where to point the camera, we have our own dedicated function for it
        public void CalcView()
        {
            GameWindow window = Utilities.window;

            float multiplier = 8;
            if (window.Keyboard[Key.LShift])
                multiplier = 20;

            Vector3 NewPos = this.Position;

            if (window.Keyboard[Key.F12]) this.Locked = false;

            if (!Locked && Input.LockMouse)
            {
                if (window.Keyboard[Key.W])
                {
                    NewPos.X += (float)Math.Cos(CamAngle.X) * (float)Utilities.ThinkTime * multiplier;
                    NewPos.Y += (float)Math.Sin(CamAngle.Y) * (float)Utilities.ThinkTime * multiplier;
                    NewPos.Z += (float)Math.Sin(CamAngle.X) * (float)Utilities.ThinkTime * multiplier;
                }

                if (window.Keyboard[Key.S])
                {
                    NewPos.X -= (float)Math.Cos(CamAngle.X) * (float)Utilities.ThinkTime * multiplier;
                    NewPos.Y -= (float)Math.Sin(CamAngle.Y) * (float)Utilities.ThinkTime * multiplier;
                    NewPos.Z -= (float)Math.Sin(CamAngle.X) * (float)Utilities.ThinkTime * multiplier;
                }

                if (window.Keyboard[Key.D])
                {
                    NewPos.X += (float)Math.Cos(CamAngle.X + Math.PI / 2) * (float)Utilities.ThinkTime * multiplier;
                    NewPos.Z += (float)Math.Sin(CamAngle.X + Math.PI / 2) * (float)Utilities.ThinkTime * multiplier;
                }

                if (window.Keyboard[Key.A])
                {
                    NewPos.X -= (float)Math.Cos(CamAngle.X + Math.PI / 2) * (float)Utilities.ThinkTime * multiplier;
                    NewPos.Z -= (float)Math.Sin(CamAngle.X + Math.PI / 2) * (float)Utilities.ThinkTime * multiplier;
                }

                CamAngle += new Vector2d(Input.deltaX / 350f, Input.deltaY / -350f);
            }
            CamAngle = new Vector2d((float)CamAngle.X, Utilities.Clamp((float)CamAngle.Y, 1.0f, -1.0f)); //Clamp it because I can't math correctly

            this.SetPos(NewPos, false);
            this.SetAngle(new Vector3((float)CamAngle.X, (float)CamAngle.Y, 0));

            View.SetPos(this.Position);
            View.SetAngles(this.Angle);
        }

        public override void Draw()
        {
            
        }
    }
}
