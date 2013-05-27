using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OlegEngine;
using OlegEngine.GUI;

using OpenTK;
namespace WheelOfSteamGames.GUI
{
    class PauseMenu : Panel
    {
        Button OKButton;
        Button Quit;
        Panel TitlePanel;
        Label TitleText;

        private float XGoal;
        public bool IsShown = true;
        public event Action OnAcceptPress;
        public string Title = "Untitled";
        public List<Panel> Elements = new List<Panel>();
        public float ElementPadding = 5;

        public override void Init()
        {
            base.Init();
            this.SetColor(20, 24, 33);
            this.ClipChildren = false;

            Button ChangeUsername = GUIManager.Create<Button>(this);
            ChangeUsername.SetHeight(25);
            ChangeUsername.SetWidth(this.Width - 32);
            ChangeUsername.SetPos(16, this.Height - (ChangeUsername.Height + 16));
            ChangeUsername.SetAnchorStyle(Anchors.Left | Anchors.Bottom | Anchors.Right);
            ChangeUsername.SetText("Change Username");
            ChangeUsername.OnButtonPress += new Button.OnButtonPressDel(ChangeUsername_OnButtonPress);

            OKButton = GUIManager.Create<Button>(this);
            OKButton.SetWidth(this.Width - 32);
            OKButton.SetHeight(35);
            OKButton.SetPos(16, ChangeUsername.Position.Y - (8 + 35 ));
            OKButton.SetAnchorStyle(Anchors.Left | Anchors.Bottom | Anchors.Right);
            OKButton.SetText("Accept");
            OKButton.OnButtonPress += new Button.OnButtonPressDel(OKButton_OnButtonPress);


            Quit = GUIManager.Create<Button>();
            Quit.SetWidth(60);
            Quit.SetHeight(35);
            Quit.SetText("Quit");
            Quit.SetPos(Quit.Position.X, Utilities.window.Height - Quit.Height);
            Quit.OnButtonPress += new Button.OnButtonPressDel(Quit_OnButtonPress);

            TitlePanel = GUIManager.Create<Panel>(this);
            //TitlePanel.SetMaterial(Resource.GetTexture("gui/title.png"));
            TitlePanel.SetHeight(25);
            TitlePanel.SetWidth(this.Width);
            TitlePanel.SetPos(new Vector2(0, -TitlePanel.Height));
            TitlePanel.SetColor(135, 36, 31);

            TitleText = GUIManager.Create<Label>();
            TitleText.SetFont("windowtitle");
            TitleText.SetParent(TitlePanel);
            TitleText.SetPos(0, 0);
            TitleText.SetColor(255, 255, 255);
            TitleText.SetText(this.Title);
            TitleText.Dock(DockStyle.LEFT);
            TitleText.SetAlignment(Label.TextAlign.MiddleLeft);
            TitleText.DockPadding(10, 10, 0, 0);

            Utilities.engine.OnSceneResize += new Action(engine_OnSceneResize);
        }

        void ChangeUsername_OnButtonPress(Panel sender)
        {
            Window msgBox = MainRoom.CreateUserSelectDialogue();
            msgBox.SetEnableCloseButton(true);
        }

        void OKButton_OnButtonPress(Panel sender)
        {
            if (OnAcceptPress != null)
                OnAcceptPress();
        }

        void Quit_OnButtonPress(Panel sender)
        {
            Utilities.window.Exit();
        }

        void engine_OnSceneResize()
        {
            Quit.SetPos(Quit.Position.X, Utilities.window.Height - Quit.Height);
        }

        public void HideToLeft()
        {
            XGoal = -this.Width - 10;
            IsShown = false;
        }

        public void ShowToLeft()
        {
            XGoal = 0;
            this.IsShown = true;
        }

        public void SetTitle(string str)
        {
            this.Title = str;
            this.TitleText.SetText(str);
        }

        public CheckBox AddCheckBox( string title, string name )
        {
            CheckBox check = GUIManager.Create<CheckBox>(this);
            check.SetText(title);
            float YPos = 10;
            if (Elements.Count > 0)
                YPos = Elements[Elements.Count - 1].Position.Y + Elements[Elements.Count - 1].Height + ElementPadding;

            check.SetPos(10, YPos);
            check.Name = name;
            Elements.Add(check);

            return check;
        }

        public bool GetCheckboxChecked(string name)
        {
            foreach (Panel p in Elements)
            {
                if (p is CheckBox && p.Name == name)
                {
                    CheckBox check = (CheckBox)p;
                    return check.IsChecked;
                }
            }

            return false;
        }

        public override void Resize(float OldWidth, float OldHeight, float NewWidth, float NewHeight)
        {
            base.Resize(OldWidth, OldHeight, NewWidth, NewHeight);
            TitlePanel.SetWidth(this.Width);

        }

        public override void Draw()
        {
            this.SetPos(Utilities.Lerp(this.Position.X, XGoal, (float)Utilities.Frametime * 10), Utilities.window.Height / 2 - (this.Height + TitlePanel.Height) / 2);
            Quit.SetPos(new Vector2(this.Position.X, Quit.Position.Y));

            base.Draw();
        }

        public override void Remove()
        {
            base.Remove();

            Utilities.engine.OnSceneResize -= new Action(engine_OnSceneResize);
        }
    }
}
