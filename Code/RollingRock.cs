﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.JungleHelper {
    [CustomEntity("JungleHelper/RollingRock")]
    class RollingRock : Actor {
        // configuration constants
        private const float SPEED = 100f;
        private const float FALLING_SPEED = 200f;
        private const float FALLING_ACCELERATION = 400f;

        // state info
        private Sprite sprite;
        private bool rolling = false;
        private bool falling = false;
        private bool shattered = false;
        private float fallingSpeed = 0f;

        public RollingRock(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Add(sprite = JungleHelperModule.SpriteBank.Create("rolling_rock"));
            Collider = new CircleColliderWithRectangles(32);
            Add(new PlayerCollider(onPlayer));

            IgnoreJumpThrus = true;
            AllowPushing = false;
        }

        private void onPlayer(Player player) {
            // kill the player.
            if (player.Position != Position) {
                player.Die(Vector2.Normalize(player.Position - Position));
            } else {
                player.Die(Vector2.Zero);
            }
        }

        protected override void OnSquish(CollisionData data) {
            // just being safe: we don't want the boulder to disappear when it gets squished, because this is just silly
        }

        public override void Update() {
            base.Update();

            if (shattered) {
                // shattered rocks do nothing.
                return;
            }

            if (falling) {
                // move down.
                fallingSpeed = Calc.Approach(fallingSpeed, FALLING_SPEED, FALLING_ACCELERATION * Engine.DeltaTime);
                MoveV(fallingSpeed * Engine.DeltaTime, hitGroundWhileFalling);
            } else if (rolling) {
                // check if we *should* be falling.
                if (!CollideCheck<Solid>(Position + Vector2.UnitY)) {
                    falling = true;
                }
            }

            if (rolling) {
                // move right.
                MoveH(SPEED * Engine.DeltaTime, hitSolidWhileMovingForward);
            }

            if (!falling && !rolling) {
                // we are waiting. fall down when player moved and is on the right.
                Player player = Scene.Tracker.GetEntity<Player>();
                if (player != null && !player.JustRespawned && player.X > Right) {
                    falling = true;
                }
            }
        }

        private void hitGroundWhileFalling(CollisionData collisionData) {
            if (collisionData.Hit is DashBlock dashBlock) {
                // we landed on a dash block: break it!
                dashBlock.Break(Center, Vector2.UnitY, true);
                SceneAs<Level>().Shake(0.2f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

                // we continue falling, but the dash block "stopped" the boulder.
                fallingSpeed = 0f;
            } else {
                // we don't fall anymore.
                falling = false;
                fallingSpeed = 0f;

                // BOOM
                Audio.Play("event:/game/06_reflection/crushblock_impact", BottomCenter);
                SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, BottomCenter, -1.57079637f);

                // if we weren't moving forward, we are now!
                if (!rolling) {
                    rolling = true;
                    sprite.Play("rolling");
                }
            }
        }

        private void hitSolidWhileMovingForward(CollisionData collisionData) {
            if (collisionData.Hit is DashBlock dashBlock) {
                // we want the boulder to break dash blocks.
                dashBlock.Break(Center, Vector2.UnitX, true);
                SceneAs<Level>().Shake(0.2f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            } else {
                // but if it touches anything else, it shatters.
                shatter();
            }
        }

        private void shatter() {
            sprite.Play("shatter");
            shattered = true;
            Collidable = false;

            Audio.Play("event:/game/general/wall_break_stone", Center);

            // for now...
            RemoveSelf();
        }
    }
}