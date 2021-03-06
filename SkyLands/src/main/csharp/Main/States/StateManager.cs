﻿using System;
using System.Media;
using System.Reflection;
using System.Collections.Generic;
using Mogre;

using Game.BaseApp;
using Game.World;
using Game.Input;
using Game.StoryEditor;

namespace Game.States
{
    public class StateManager : OgreForm
    {
        private readonly Stack<State> mStateStack;
        private readonly Stack<Type>  mNewStates;
        private int                   mPopRequested;
        private static TypeWorld      mNewWorld;
        private static bool           mChangeWorld;

        public Root            Root        { get { return this.mRoot; } }
        public SceneManager    SceneMgr    { get { return this.mSceneMgr; } }
        public RenderWindow    Window      { get { return this.mWindow; } }
        public Controller      Controller  { get { return this.mController; } }
        public Camera          Camera      { get { return this.mCam; } }
        public Viewport        Viewport    { get { return this.mViewport; } }
        public int             NumberState { get { return this.mStateStack.Count; } }
        public GameInfo        GameInfo    { get; set; }
        public StoryEditorInfo StoryInfo   { get; set; }
        public MainState       MainState   { get; private set; }
        public SoundPlayer     SoundPlayer { get; set; }
        public bool    IsOnWorldChangement { get; private set; }

        public StateManager()
        {
            this.mStateStack = new Stack<State>();
            this.mNewStates = new Stack<Type>();
            this.mPopRequested = 0;
            this.GameInfo = new GameInfo();
            this.StoryInfo = new StoryEditorInfo();
            this.RequestStatePush(typeof(MenuState));
            mNewWorld = TypeWorld.Hills;
            this.SoundPlayer = new SoundPlayer();
            this.SoundPlayer.SoundLocationChanged += (o, args) => this.SoundPlayer.PlayLooping();

            this.Disposed += this.Shutdown;
        }

        protected override void Update(float frameTime)
        {
            if (mChangeWorld)
            {
                this.GameInfo.Type = mNewWorld;
                this.IsOnWorldChangement = true;
                mChangeWorld = false;
                this.RequestStatePop();
                this.RequestStatePush(typeof(GameState));
            }
            
            while(this.mPopRequested > 0) { this.PopState(); }

            for (int i = 0; i < this.mNewStates.Count; i++)
            {
                State newState = null;

                // Use reflection to get new state class default constructor
                ConstructorInfo constructor = this.mNewStates.Pop().GetConstructor(new Type[] { typeof(StateManager) });

                // Try to create an object from the requested state class
                if (constructor != null) { newState = (State)constructor.Invoke(new StateManager[] {this}); }
                if (newState != null)    { this.PushState(newState); }
            }

            if (this.mStateStack.Count > 0) { this.mStateStack.Peek().Update(frameTime); }
        }

        /* Add a State to the stack and start it up */
        private void PushState(State newState)
        {
            if (newState == null) {return;}

            LogManager.Singleton.DefaultLog.LogMessage("--- Try to start up state : " + newState);

            if (newState.GetType().IsSubclassOf(typeof(MainState)))
                this.MainState = (MainState)newState;

            if (!newState.StartupState())
            {
                LogManager.Singleton.DefaultLog.LogMessage("ERROR : Failed to start up state : " + newState);
                return;
            }

            if (this.mStateStack.Count > 0) { this.mStateStack.Peek().Hide(); }

            this.mStateStack.Push(newState);
            this.mStateStack.Peek().Show();

            this.IsOnWorldChangement = false;
            this.mController.ClearAll();
        }

        private void PopState()
        {
            if (this.mStateStack.Count > 0)
            {
                string stateName = this.mStateStack.Peek().Name;
                this.mStateStack.Peek().ShutdownState();
                this.mStateStack.Pop();
                this.mController.ClearAll();
                if (this.mStateStack.Count > 0 && this.mNewStates.Count == 0) { this.mStateStack.Peek().Show(); }
                LogManager.Singleton.DefaultLog.LogMessage("--- Popped state : " + stateName);
            }

            this.mPopRequested--;
        }

        public void RequestStatePop(int pop = 1)
        {
            int totalPop = this.mPopRequested + pop;
            if (this.mStateStack.Count > totalPop) { this.mPopRequested = totalPop; } // Will pop the state in Update()
            else                                   { this.mIsShutDownRequested = true; }   // Will ShutDown in Update()
        }

        public void PopToMenu() { this.RequestStatePop(this.NumberState - 1);}

        public void RequestStatePush(params Type[] newStates)
        {
            foreach (Type newState in newStates)
                if (newState != null && newState.IsSubclassOf(typeof(State))) { this.mNewStates.Push(newState); }
        }

        public static void ChangeIsland(TypeWorld newWorld)
        {
            mNewWorld = newWorld;
            StateManager.ChangeIsland();
        }
        public static void ChangeIsland() { mChangeWorld = true; }

        protected override void Shutdown(object sender, EventArgs e)
        {
            LogManager.Singleton.DefaultLog.LogMessage("***********************End of Program\'s Log***********************");
            while (this.mStateStack.Count > 0) { this.PopState(); }
            base.Shutdown(sender, e);
        }
    }
}
