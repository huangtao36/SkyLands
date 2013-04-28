﻿using System;
using Miyagi.Common.Events;
using Miyagi.UI.Controls;

using Game.GUICreator;
using Mogre;
using Game.World;

namespace Game.States
{
    public class OptionsState : State
    {
        private OptionsGUI mOptionsGUI;
        private bool mIsFullScreen;
        private Vector3 mIslandLoc;
        private MainWorld mWorld = null;

        public OptionsState(StateManager stateMgr) : base(stateMgr, "Option") { }

        protected override void Startup()
        {
            this.mOptionsGUI = new OptionsGUI(this.mStateMgr, "Options GUI");
            this.mOptionsGUI.SetListener(OptionsGUI.ButtonName.FullScreen, this.ClickFullScreenButton);
            this.mOptionsGUI.SetListener(OptionsGUI.ButtonName.HighQuality, this.ClickQualityButton);
            this.mOptionsGUI.SetListener(OptionsGUI.ButtonName.Music, this.ClickMusicButton);
            this.mOptionsGUI.SetListener(OptionsGUI.ButtonName.VSync, this.ClickVSyncButton);
            this.mOptionsGUI.SetListenerBack(this.ClickBackButton);
            this.mOptionsGUI.SetListenerSave(this.ClickSaveButton);
            this.mOptionsGUI.SetListenerLoad(this.ClickLoadButton);
            this.mIsFullScreen = this.mStateMgr.Window.IsFullScreen;
        }

        public override void Hide()
        {
            this.mStateMgr.MiyagiMgr.CursorVisibility = false;
        }

        public override void Show()
        {
            this.mOptionsGUI.Show();
            this.mStateMgr.MiyagiMgr.CursorVisibility = true;
        }

        public void AttachWorld(MainWorld world, Vector3 islandLoc)
        {
            this.mWorld = world;
            this.mIslandLoc = islandLoc;
        }

        public override void Update(float frameTime)
        {
            if (this.mStateMgr.Input.IsKeyDown(MOIS.KeyCode.KC_ESCAPE)) { this.mStateMgr.RequestStatePop(); }
        }

        protected override void Shutdown() { this.mOptionsGUI.Dispose(); }

        private void SwitchText(Button b) 
        {
            if (b.Text == "ON") {b.Text = "OFF";}
            else{b.Text = "ON";}
        }

        private void ClickBackButton(object obj, MouseButtonEventArgs arg)
        {
            this.mStateMgr.RequestStatePop(this.mWorld == null ? 1 : 2);
        }

        private void ClickMusicButton(object obj, MouseButtonEventArgs arg) 
        {
            this.SwitchText(this.mOptionsGUI.Buttons[OptionsGUI.ButtonName.Music]);
        }

        private void ClickQualityButton(object obj, MouseButtonEventArgs arg) 
        {
            this.SwitchText(this.mOptionsGUI.Buttons[OptionsGUI.ButtonName.HighQuality]);
        }

        private void ClickVSyncButton(object obj, MouseButtonEventArgs arg) 
        {
            this.SwitchText(this.mOptionsGUI.Buttons[OptionsGUI.ButtonName.VSync]);
        }

        private void ClickFullScreenButton(object obj, MouseButtonEventArgs arg) 
        {
            this.mIsFullScreen = !this.mIsFullScreen;
            this.mStateMgr.Window.SetFullscreen(this.mIsFullScreen, this.mStateMgr.Window.Width, this.mStateMgr.Window.Height);
            this.SwitchText(this.mOptionsGUI.Buttons[OptionsGUI.ButtonName.FullScreen]);
        }

        private void ClickSaveButton(object obj, MouseButtonEventArgs arg)
        {
            if (this.mWorld != null) { this.mWorld.getIslandAt(this.mIslandLoc).save(); }
        }

        private void ClickLoadButton(object obj, MouseButtonEventArgs arg)
        {
            if (this.mWorld != null) { this.mWorld.getIslandAt(this.mIslandLoc).load(); }
        }
    }
}
