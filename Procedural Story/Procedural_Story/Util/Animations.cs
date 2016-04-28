using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

using Procedural_Story.Core.Life;

namespace Procedural_Story.Util {
    static class Animations {
        public static Animation CharacterWalk {
            get {
                Animation anim;

                Vector3[] offsets = new Vector3[] {
                    Vector3.Zero,
                    new Vector3(.5f, .27f, 0),
                    Vector3.Zero,
                    new Vector3(.25f, -.45f, 0),
                    new Vector3(-.25f, -.45f, 0),
                    new Vector3(-.5f, .27f, 0),
                    Vector3.Zero,
                };

                anim = new Animation("CharacterWalk", 0);
                anim.Loop = true;
                float l = 1f;
                AnimationKeyFrame[] frames = new AnimationKeyFrame[] {
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[1]),  // rarm
                    new Pose.Orientation(3, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[3]),  // rleg
                    new Pose.Orientation(4, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[4]),  // lleg
                    new Pose.Orientation(5, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[5]),  // larm
                }), 0),
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, new Vector3(-MathHelper.PiOver4*.75f, 0, 0), Vector3.Zero, Vector3.One, offsets[1]),   // rarm
                    new Pose.Orientation(3, new Vector3( MathHelper.PiOver4, 0, 0), Vector3.Zero, Vector3.One, offsets[3]),        // rleg
                    new Pose.Orientation(4, new Vector3(-MathHelper.PiOver4, 0, 0), Vector3.Zero, Vector3.One, offsets[4]),        // lleg
                    new Pose.Orientation(5, new Vector3( MathHelper.PiOver4*.75f, 0, 0), Vector3.Zero, Vector3.One, offsets[5]),   // larm
                }), l),
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[1]),  // rarm
                    new Pose.Orientation(3, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[3]),  // rarm
                    new Pose.Orientation(4, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[4]),  // rarm
                    new Pose.Orientation(5, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[5]),  // rarm
                }), l*2),
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, new Vector3( MathHelper.PiOver4*.5f, 0, 0), Vector3.Zero, Vector3.One, offsets[1]),    // rarm
                    new Pose.Orientation(3, new Vector3(-MathHelper.PiOver4, 0, 0), Vector3.Zero, Vector3.One, offsets[3]),        // rleg
                    new Pose.Orientation(4, new Vector3( MathHelper.PiOver4, 0, 0), Vector3.Zero, Vector3.One, offsets[4]),        // lleg
                    new Pose.Orientation(5, new Vector3(-MathHelper.PiOver4*.5f, 0, 0), Vector3.Zero, Vector3.One, offsets[5]),    // larm
                }), l*3),
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[1]),  // rarm
                    new Pose.Orientation(3, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[3]),  // rleg
                    new Pose.Orientation(4, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[4]),  // lleg
                    new Pose.Orientation(5, Vector3.Zero, Vector3.Zero, Vector3.One, offsets[5]),  // larm
                }), l*4),
                };

                anim.frames.AddRange(frames);
                return anim;
            }
        }
        public static Animation CharacterSwing {
            get {
                Animation anim;

                Vector3 offset = new Vector3(.5f, .27f, 0);

                anim = new Animation("CharacterSwing", 3);
                AnimationKeyFrame[] frames = new AnimationKeyFrame[] {
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, Vector3.Zero, Vector3.Zero, Vector3.One, offset),  // rarm
                }), 0),
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, new Vector3(MathHelper.Pi * .8f, 0, -MathHelper.Pi * .1f), Vector3.Zero, Vector3.One, offset),  // rarm
                }), .175f),
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, new Vector3(MathHelper.Pi * .3f, MathHelper.Pi * .2f, -MathHelper.Pi * .1f), Vector3.Zero, Vector3.One, offset),   // rarm
                }), .35f),
                new AnimationKeyFrame(new Pose(anim.Weight, 7, new Pose.Orientation[] {
                    new Pose.Orientation(1, new Vector3(MathHelper.Pi * .3f, MathHelper.Pi * .2f, -MathHelper.Pi * .1f), Vector3.Zero, Vector3.One, offset),   // rarm
                }), .5f),
                };

                anim.frames.AddRange(frames);
                return anim;
            }
        }
    }
}
