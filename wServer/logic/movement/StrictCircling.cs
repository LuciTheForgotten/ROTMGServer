﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.realm;
using wServer.realm.entities;
using wServer.svrPackets;
using Mono.Game;

namespace wServer.logic.movement
{
    class StrictCircling : Behavior
    {
        class CirclingState
        {
            public WeakReference<Entity> target;
            public float angle;
        }

        float radius;
        float angularSpeed;
        float speed;
        short? objType;
        private StrictCircling(float radius, float speed, short? objType)
        {
            this.radius = radius;
            this.angularSpeed = speed / radius;
            this.speed = speed;
            this.objType = objType;
        }
        static readonly Dictionary<Tuple<float, float, short?>, StrictCircling> instances = new Dictionary<Tuple<float, float, short?>, StrictCircling>();
        public static StrictCircling Instance(float radius, float speed, short? objType)
        {
            var key = new Tuple<float, float, short?>(radius, speed, objType);
            StrictCircling ret;
            if (!instances.TryGetValue(key, out ret))
                ret = instances[key] = new StrictCircling(radius, speed, objType);
            return ret;
        }

        Random rand = new Random();
        protected override bool TickCore(RealmTime time)
        {
            if (Host.Self.HasConditionEffect(ConditionEffects.Paralyzed)) return true;
            var speed = this.speed * GetSpeedMultiplier(Host.Self);

            CirclingState state;
            object o;
            if (!Host.StateStorage.TryGetValue(Key, out o))
            {
                float dist = radius + 1;
                Host.StateStorage[Key] = state = new CirclingState()
                {
                    target = WeakReference<Entity>.Create(GetNearestEntity(ref dist, objType)),
                    angle = (float)(2 * Math.PI * rand.NextDouble())
                };
            }
            else
            {
                state = (CirclingState)o;

                state.angle += angularSpeed * (time.thisTickTimes / 1000f);
                if (!state.target.IsAlive)
                {
                    Host.StateStorage.Remove(Key);
                    return false;
                }
                var target = state.target.Target;
                if (target == null || target.Owner == null)
                {
                    Host.StateStorage.Remove(Key);
                    return false;
                }
                double x = target.X + Math.Cos(state.angle) * radius;
                double y = target.Y + Math.Sin(state.angle) * radius;
                ValidateAndMove((float)x, (float)y);
                Host.Self.UpdateCount++;
            }

            if (state.angle >= Math.PI * 2)
                state.angle -= (float)(Math.PI * 2);
            return true;
        }
    }
}
