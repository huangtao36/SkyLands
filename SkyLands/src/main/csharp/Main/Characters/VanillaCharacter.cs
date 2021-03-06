﻿using System;
using System.Collections.Generic;
using System.Linq;
using API.Generic;
using Game.World.Blocks;
using Game.World.Display;
using Mogre;

using API.Ent;
using API.Geo.Cuboid;

using Game.World;
using Game.Animation;
using Game.CharacSystem.AI;
using Game.Shoot;
using Game.RTS;

namespace Game.CharacSystem
{
    public abstract class VanillaCharacter : Character
    {
        private const float WALK_SPEED = 350;
        private const float YAW_SPEED = 7;    // Only for forced movement
        private const float SQUARED_DIST_PRECISION = 100;
        private const float SPRINT_FACTOR = 1.65f;
        protected const float LIMIT_TARGET_DISTANCE = Bullet.DEFAULT_RANGE;

        protected CharacMgr     mCharacMgr;
        protected SceneNode     mNode;
        private   SceneNode[]   mWireBoxes;
        protected MeshAnim      mMesh;
        protected CharacterInfo mCharInfo;
        protected CollisionMgr  mCollisionMgr;
        private Vector3         mPreviousDirection;
        private float           mTimeSinceDead;
        private bool            mIsFriendlySelected, mIsHostilelySelected;

        private PathFinder       mPathFinder;
        protected Stack<Vector3> mForcedDestination;
        private Degree           mYawGoal;
        private VanillaCharacter mFollow;
        private Vector3          mDestination;
        private double           mLastSquaredDist;
        private float            mStaySqrDistToGoal;
        private bool             mWasAllowedToMove, mWillBeAllowedToMove, mIsBlocked;

        public SceneNode     Node            { get { return this.mNode; } }
        public Vector3       Size            { get { return this.mMesh.MeshSize; } }
        public CharacterInfo Info            { get { return this.mCharInfo; } }
        public CollisionMgr  CollisionMgr    { get { return this.mCollisionMgr; } }
        public MovementInfo  MovementInfo    { get; private set; }
        public bool          WaitForRemove   { get; private set; }
        public bool          IsFollowing     { get { return this.mFollow != null; } }
        public Quaternion    InitOrientation { get { return this.mMesh.InitialOrientation; } }
        public Vector3       BlockPosition   { get { return MainWorld.getRelativeFromAbsolute(this.FeetPosition); } }
        public Vector3       FeetPosition
        {
            get           { return this.mNode.Position - new Vector3(0, this.Size.y / 2 + this.mMesh.FeetDiff, 0); }
            protected set { this.mNode.SetPosition(value.x, value.y + this.Size.y / 2 + this.mMesh.FeetDiff, value.z); }
        }

        protected VanillaCharacter(CharacMgr characMgr, CharacterInfo charInfo)
        {
            this.mCharacMgr = characMgr;
            this.mCharInfo = charInfo;
            this.MovementInfo = new MovementInfo(this.OnFall, this.OnJump);
            this.mPreviousDirection = Vector3.ZERO;
            this.mTimeSinceDead = 0;
            this.mLastSquaredDist = -1;
            this.mForcedDestination = new Stack<Vector3>();
        }

        protected void FinishCreation()
        {
            this.FeetPosition = this.mCharInfo.SpawnPoint;
            this.mCollisionMgr = new CollisionMgr(this.mCharacMgr.World, this);

            this.mWireBoxes = new SceneNode[2];
            for (int i = 0; i < 2; i++)
            {
                this.mWireBoxes[i] = this.mNode.CreateChildSceneNode();
                this.mWireBoxes[i].InheritScale = false;
                this.mWireBoxes[i].InheritOrientation = true;
                this.mWireBoxes[i].AttachObject(StaticRectangle.CreateRectangle(this.mCharacMgr.SceneMgr, new Vector3(-Cst.CUBE_SIDE/2, 0, -Cst.CUBE_SIDE/2),
                    new Vector3(1, 2, 1)*Cst.CUBE_SIDE, i == 0 ? ColoredMaterials.BLUE : ColoredMaterials.RED));
                this.mWireBoxes[i].SetVisible(false);
            }
        }

        private void OnFall(bool isFalling)
        {
            if (isFalling)
            {
                this.mMesh.JumpLoop();
                GravitySpeed.Reset();
            }
            else
            {
                if (GravitySpeed.GetTimeSinceFall() >= 600)
                    this.Hit(GravitySpeed.GetSpeed() / GravitySpeed.SPEED_TMAX * VanillaPlayer.DEFAULT_PLAYER_LIFE * 0.8f);
                if (this.mCharInfo.IsPlayer)
                    ((Sinbad)this.mMesh).EndJump();
            }
        }

        private void OnJump(bool isJumping)
        {
            if (isJumping)
            {
                this.mMesh.StartJump();
                JumpSpeed.Jump();
            }
            else { this.MovementInfo.IsFalling = true; }
        }

        public Degree GetYaw()  // Return the yaw between -180 and 180
        {
            Quaternion q = this.mNode.Orientation;
            return (Mogre.Math.Abs(q.w) < Mogre.Math.Abs(q.y) ? (new Degree(180) - q.Yaw) : (Degree)q.Yaw)
                    - this.mMesh.InitialOrientation.Yaw;
        }

        public bool IsOnFloor()
        {
            return !this.MovementInfo.IsFalling && !this.MovementInfo.IsJumping && !this.MovementInfo.IsPushedByArcaneLevitator;
        }

        public void Update(float frameTime)
        {
            Vector3 translation = Vector3.ZERO;

            if (this.mCharInfo.Life > 0)
            {
                if (this.mFollow != null && this.mFollow.IsOnFloor() && this.mForcedDestination.Count == 0 &&
                    (this.mFollow.FeetPosition - this.mDestination).SquaredLength > this.mStaySqrDistToGoal)
                    this.FindPathTo(this.mFollow.FeetPosition);
                
                if (this.mPathFinder != null)
                {
                    this.mForcedDestination = this.mPathFinder.ContinuePathFinding();
                    if (this.mForcedDestination.Count > 0)
                    {
                        this.mPathFinder = null;
                        this.ComputeNextYaw();
                        this.mLastSquaredDist = -1;
                        this.MovementInfo.IsMovementForced = true;
                        this.mWasAllowedToMove = this.MovementInfo.IsAllowedToMove;
                        this.MovementInfo.IsAllowedToMove = false;
                    }
                    else if (this.mPathFinder.HaveGiveUp)
                    {
                        this.mPathFinder = null;
                        this.mDestination = Vector3.ZERO;
                        Console.WriteLine("forget path");
                    }
                }

                Degree distToYawGoal = this.mYawGoal - this.GetYaw();
                float absDiffYaw = System.Math.Abs((((int)distToYawGoal.ValueAngleUnits) + 360) % 360);

                if (this.MovementInfo.IsMovementForced && this.mForcedDestination.Count == 0 && absDiffYaw < 1)
                {
                    this.MovementInfo.IsAllowedToMove = this.mWasAllowedToMove;
                    this.MovementInfo.IsMovementForced = false;
                }

                if (this.MovementInfo.IsMovementForced)
                {
                    this.MovementInfo.YawValue = YAW_SPEED * YawFactor.GetFactor(distToYawGoal);

                    if (this.mForcedDestination.Count > 0)
                    {
                        Vector3 diff = this.mForcedDestination.Peek() - this.FeetPosition;
                        float squaredDistance = diff.x * diff.x + diff.z * diff.z;
                        if ((this.mDestination - this.FeetPosition).SquaredLength < this.mStaySqrDistToGoal)
                        {
                            while(this.mForcedDestination.Count > 0)
                                this.PopForcedDest(false);
                        }
                        else if (this.mLastSquaredDist == -1 || squaredDistance > SQUARED_DIST_PRECISION)
                        {
                            if (absDiffYaw < 20)
                            {
                                float factor = 6 / absDiffYaw;
                                this.MovementInfo.MoveDirection = this.mMesh.MoveForwardDir * (factor <= 1 ? factor : 1);

                                if (this.mForcedDestination.Peek().y > this.FeetPosition.y || this.mIsBlocked)
                                    this.MovementInfo.MoveDirection += Vector3.UNIT_Y;
                            }
                            this.mLastSquaredDist = squaredDistance;
                        }
                        else
                            this.PopForcedDest();
                    }
                }
                if (this.mCharInfo.IsPlayer) { ((VanillaPlayer) this).Update(frameTime); }
                else                         { ((VanillaNonPlayer) this).Update(frameTime); }

                if (this.MovementInfo.IsPushedByArcaneLevitator)
                    translation.y = ArcaneLevitatorSpeed.GetSpeed();
                else if (this.MovementInfo.IsJumping)
                    translation.y = JumpSpeed.GetSpeed();
                else
                    translation.y = GravitySpeed.GetSpeed();

                translation += WALK_SPEED * this.MovementInfo.MoveDirection * new Vector3(1, 0, 1)
                               * (this.MovementInfo.Sprint ? SPRINT_FACTOR : 1); // Ignores the y axis translation here
                this.mNode.Yaw(this.MovementInfo.YawValue * frameTime);

                this.Translate(translation * frameTime);    // Apply the translation

                if (this.MovementInfo.IsAllowedToMove)
                    this.mYawGoal = this.GetYaw();

                if (!this.MovementInfo.IsJumping && !this.MovementInfo.IsFalling && !this.MovementInfo.IsPushedByArcaneLevitator)
                {
                    int zDirSign = this.mMesh.Zdir(this.MovementInfo.MoveDirection);
                    int prevZDirSign = this.mMesh.Zdir(this.mPreviousDirection);

                    if (zDirSign != 0 && zDirSign != prevZDirSign)
                        this.mMesh.Walk(true, zDirSign);
                    else if (zDirSign == 0 && prevZDirSign != 0)
                        this.mMesh.Walk(false);
                }
                this.mPreviousDirection = this.MovementInfo.MoveDirection;
                if (this.mMesh.AnimMgr.CurrentAnims.Count == 0) // By default apply idle anim
                {
                    this.mMesh.Idle();
                    this.mPreviousDirection = Vector3.ZERO;
                }
            }
            else
            {
                this.mTimeSinceDead += frameTime;
                if (this.mTimeSinceDead >= 2) { this.WaitForRemove = true; }
            }

            this.mMesh.Update(frameTime);
            this.MovementInfo.ClearInfo();

            if (this.FeetPosition.y < 0) { this.WaitForRemove = true; }
        }

        private void Translate(Vector3 translation)
        {
            Vector3 actualTranslation = this.mCollisionMgr.ComputeCollision(translation * this.mNode.LocalAxes.Transpose());
            this.mIsBlocked = (translation.x > 0 && actualTranslation.x == 0) || (translation.z > 0 && actualTranslation.z == 0);

            /* Here translate has been modified to avoid collisions */
            this.MovementInfo.IsFalling = actualTranslation.y < 0;
            this.MovementInfo.IsJumping = actualTranslation.y > 0 && JumpSpeed.IsJumping;

            Vector3 prevBlockPos = this.BlockPosition;
            this.mNode.Translate(actualTranslation);
            Vector3 actBlockPos = this.BlockPosition;

            if(this.mForcedDestination.Count > 0 && this.mCollisionMgr.HasHorizontalCollisionEnded)
                this.ComputeNextYaw();

            if (prevBlockPos != actBlockPos)
            {
                this.mCharacMgr.World.onBlockLeave(prevBlockPos, this);
                this.mCharacMgr.World.onBlockEnter(actBlockPos, this);
            }
        }

        private void PopForcedDest(bool allowTP = true)
        {
            if (allowTP && this.mForcedDestination.Count == 1)
                this.FeetPosition = this.mDestination;
            
            this.mForcedDestination.Pop();
            if (this.mForcedDestination.Count == 0)
            {
                this.MovementInfo.IsMovementForced = false;
                if (this.mWillBeAllowedToMove)
                {
                    this.MovementInfo.IsAllowedToMove = true;
                    this.mWillBeAllowedToMove = false;
                    this.mDestination = Vector3.ZERO;
                }
            }
            else
                this.ComputeNextYaw();
            this.mLastSquaredDist = -1;
        }

        private void ComputeNextYaw()
        {
            Vector3 diff = this.mForcedDestination.Peek() - this.FeetPosition;
            this.mYawGoal = Mogre.Math.ACos(new Vector2(diff.x, diff.z).NormalisedCopy.y) * System.Math.Sign(diff.x);
        }

        public void MoveTo(Vector3 destination, float stayAtDist = 0)
        {
            Vector3 diff = destination - this.FeetPosition;
            if (diff.x * diff.x + diff.z * diff.z < SQUARED_DIST_PRECISION + stayAtDist * stayAtDist || MainWorld.AbsToRelative(destination) == this.BlockPosition)
                return;

            this.mStaySqrDistToGoal = stayAtDist * stayAtDist;

            /* Disable the previous orders */
            this.mFollow = null;
            this.mYawGoal = this.GetYaw();

            this.FindPathTo(destination);
        }

        /*public void SetForcedDest(Vector3 dest) // Temp
        {
            this.mForcedDestination.Clear();
            this.mForcedDestination.Push(dest);
            this.mDestination = dest;
            this.mWasAllowedToMove = this.MovementInfo.IsAllowedToMove;
            this.MovementInfo.IsAllowedToMove = false;
            this.MovementInfo.IsMovementForced = true;
        }*/

        private void FindPathTo(Vector3 destination)
        {
            if (!(this.mCharacMgr.World.getIsland().getBlock(MainWorld.AbsToRelative(destination), false) is Air)) { Console.WriteLine("path rejected"); return; }
            this.mDestination = destination;
            Console.WriteLine("Create pathfinder");
            this.mPathFinder = new PathFinder(this.mDestination, this.BlockPosition, this.mCharacMgr.World.getIsland());
        }

        public void YawTo(Degree angle) // A continuous yaw
        {
            this.mYawGoal = angle;
            this.mWasAllowedToMove = this.MovementInfo.IsAllowedToMove;
            this.MovementInfo.IsAllowedToMove = false;
            this.MovementInfo.IsMovementForced = true;
        }

        public void Follow(VanillaCharacter charac, float distMax = Cst.CUBE_SIDE * 3)
        {
            this.mYawGoal = this.GetYaw();
            this.mForcedDestination.Clear();
            this.mFollow = charac;
            this.mStaySqrDistToGoal = distMax * distMax;
        }

        public void StopFollowing()
        {
            this.mFollow = null;
            this.mYawGoal = this.GetYaw();
            this.mForcedDestination.Clear();
        }

        public void Heal(float heal)
        {
            if (heal >= 0) { return; }
            this.mCharInfo.Life += heal;
            float maxLife = this.Info.IsPlayer ? VanillaPlayer.DEFAULT_PLAYER_LIFE : VanillaNonPlayer.DEFAULT_NPC_LIFE;

            if (this.mCharInfo.Life > maxLife) { this.mCharInfo.Life = maxLife; }
        }

        public void Hit(float damage, VanillaCharacter source = null)
        {
            if (this.mCharInfo.Life <= 0 || damage <= 0) { return; }
            this.mCharInfo.Life -= damage;

            if (this.mCharInfo.IsPlayer)
            {
                if (this.mCharacMgr.StateMgr.Controller.GamePadState.IsConnected)
                {
                    float force = (damage + 15)/VanillaPlayer.DEFAULT_PLAYER_LIFE*7;
                    this.mCharacMgr.StateMgr.Controller.Vibrate(force, force, 0.2f);
                }
                ((PlayerRTS)this.mCharInfo.RTS).UpdateLife();
            }

            if (this.mCharInfo.Life <= 0)
            {
                this.MovementInfo.IsJumping = false;
                this.MovementInfo.IsPushedByArcaneLevitator = false;

                // Don't remove the charac right now to play the animation
                if (!this.mCharInfo.IsPlayer) { ((Robot)this.mMesh).Die(); }
                else { this.WaitForRemove = true; }
            }
            else if (!this.Info.IsPlayer && ((VanillaNonPlayer) this).Target == null && !this.MovementInfo.IsMovementForced &&
                this.mFollow == null && source != null)
                ((VanillaNonPlayer) this).SetTargetAndFollow(source);
        }

        public void SetIsAllowedToMove(bool allowToMove, bool waitEndForcedMovement = true)
        {
            if (!waitEndForcedMovement)
            {
                if (allowToMove)
                {
                    this.MovementInfo.IsMovementForced = false;
                    this.mForcedDestination.Clear();
                }
                this.MovementInfo.IsAllowedToMove = allowToMove;
            }
            else
            {
                this.MovementInfo.IsAllowedToMove = !this.MovementInfo.IsMovementForced && allowToMove;
                if (this.MovementInfo.IsAllowedToMove) { this.mWillBeAllowedToMove = allowToMove; }
            }
        }

        public void SetSelected(bool selected, bool friendly = true)
        {
            if (!selected)
            {
                for(int i = 0; i < 2; i++)
                    this.mWireBoxes[i].SetVisible(false);
                this.mIsFriendlySelected = false;
                this.mIsHostilelySelected = false;
            }
            else if (friendly)
            {
                this.mIsFriendlySelected = true;
                this.mIsHostilelySelected = !this.mIsFriendlySelected;
                this.mWireBoxes[0].SetVisible(this.mIsFriendlySelected);
                this.mWireBoxes[1].SetVisible(!this.mIsFriendlySelected);
            }
            else
            {
                this.mIsHostilelySelected = true;
                this.mIsFriendlySelected = !this.mIsHostilelySelected;
                this.mWireBoxes[0].SetVisible(!this.mIsHostilelySelected);
                this.mWireBoxes[1].SetVisible(this.mIsHostilelySelected);
            }
        }

        public bool IsFriendlySelected() { return this.mIsFriendlySelected; }
        public bool IsHostilelySelected() { return this.mIsHostilelySelected; }

        public bool IsFollowingCharac(VanillaCharacter charac) { return this.mFollow == charac; }

        public virtual void Dispose(bool updateTargets = true)   // Must be called by the CharacMgr only.
        {
            if (updateTargets)
            {
                foreach (Faction faction in Enum.GetValues(typeof(Faction)).Cast<Faction>().Where(faction => faction != this.mCharInfo.Faction))
                {
                    foreach (VanillaNonPlayer ennemy in this.mCharacMgr.GetFactionCharacters(faction)
                        .Where(ennemy => ennemy is VanillaNonPlayer).Cast<VanillaNonPlayer>()
                        .Where(ennemy => ennemy.Target == this)) {
                        ennemy.updateTargets();
                    }
                }
            }

            this.mCharInfo.RTS.AmountUnits--;
            this.mNode.RemoveAndDestroyAllChildren();
            this.mCharacMgr.SceneMgr.DestroySceneNode(this.mNode);
        }

        /* Character */
        public String getName()                 { return this.mCharInfo.Name; }
        public String getDisplayName()          { return this.getName(); }
        public void setDisplayName(String name) { this.mCharInfo.Name = name; }
        public bool save()                      { throw new NotImplementedException(); }
        public Vector3 getSpawnPoint()          { return this.mCharInfo.SpawnPoint; }
        public void setSpawnPoint(Vector3 loc)  { this.mCharInfo.SpawnPoint = loc; }

        /* Entity */
        public int getId()                        { return this.mCharInfo.Id; }
        public CharacterInfo getCharacInfo()      { return this.mCharInfo; }
        public void remove()                      { this.WaitForRemove = true; }
        public bool isRemoved()                   { throw new NotImplementedException(); }
        public bool isSpawned()                   { throw new NotImplementedException(); }
        public void setSavable(bool savable)      { throw new NotImplementedException(); }
        public bool isSavable()                   { throw new NotImplementedException(); }
        public void setViewDistance(int distance) { throw new NotImplementedException(); }
        public int getViewDistance()              { throw new NotImplementedException(); }
        public Chunk getChunk()                   { throw new NotImplementedException(); }
        public SceneNode getNode() { return this.mNode; }

        public Island getIsland() { return this.mCharacMgr.World.getIsland(); }
        public Vector3 getPosition() { return this.BlockPosition; }

        public void setIsPushedByArcaneLevitator(bool isPushedWanted)
        {
            if (isPushedWanted && !this.MovementInfo.IsPushedByArcaneLevitator)
                ArcaneLevitatorSpeed.StartLevitation();

            if (!isPushedWanted)
                GravitySpeed.Reset();

            this.MovementInfo.IsPushedByArcaneLevitator = isPushedWanted;
            this.mMesh.AnimMgr.DeleteAllAnims();
        }

        public virtual void updateTargets()
        {
            foreach (Faction faction in Enum.GetValues(typeof(Faction)).Cast<Faction>().Where(faction => faction != this.mCharInfo.Faction))
            {
                foreach (VanillaNonPlayer ennemy in this.mCharacMgr.GetFactionCharacters(faction).Where(ennemy => ennemy is VanillaNonPlayer))
                {
                    float oldDistance = ennemy.Target != null ? (ennemy.FeetPosition - ennemy.Target.FeetPosition).Length : LIMIT_TARGET_DISTANCE + 1;
                    Vector3 diff = ennemy.FeetPosition - this.FeetPosition;
                    float newDistance = diff.Length;
                    if (newDistance < oldDistance && newDistance < LIMIT_TARGET_DISTANCE && System.Math.Abs(diff.y / Cst.CUBE_SIDE) <= 6) { ennemy.Target = this; }
                    if (ennemy.Target == this && (newDistance > LIMIT_TARGET_DISTANCE || System.Math.Abs(diff.y / Cst.CUBE_SIDE) > 6)) { ennemy.Target = null; }
                }
            }
        }

        public void teleport(Vector3 loc)
        {
            this.MovementInfo.ClearInfo();
            GravitySpeed.Reset();
            this.MovementInfo.IsFalling = false;
            this.MovementInfo.IsJumping = false;
            this.MovementInfo.IsPushedByArcaneLevitator = false;
            this.MovementInfo.IsMovementForced = false;
            this.mPreviousDirection = Vector3.ZERO;
            this.mForcedDestination.Clear();
            this.WaitForRemove = false;

            Vector3 prevBlockPos = this.BlockPosition;
            this.FeetPosition = loc;
            Vector3 actBlockPos = this.BlockPosition;

            if (prevBlockPos != actBlockPos)
            {
                this.mCharacMgr.World.onBlockLeave(prevBlockPos, this);
                this.mCharacMgr.World.onBlockEnter(actBlockPos, this);
            }
        }
    }
}