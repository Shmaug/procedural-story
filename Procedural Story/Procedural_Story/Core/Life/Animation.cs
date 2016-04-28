using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Procedural_Story.Core.Life {
    class Pose {
        public struct Orientation {
            public int BoneIndex;
            public Vector3 Rotation;
            public Vector3 Position;
            public Vector3 Scale;
            public Vector3 Origin;

            public Orientation(int bi, Vector3 rot, Vector3 pos, Vector3 sc, Vector3 or) {
                BoneIndex = bi;
                Rotation = rot;
                Position = pos;
                Scale = sc;
                Origin = or;
            }
        }
        public Orientation[] orientations;
        public int BoneCount;
        public int Weight;

        public Pose(int weight, int bc, Orientation[] o) {
            Weight = weight;
            BoneCount = bc;
            orientations = o;
        }

        public Matrix[] getTransforms() {
            Matrix[] mat = new Matrix[BoneCount];
            for (int i = 0; i < mat.Length; i++) mat[i] = Matrix.Identity;
            for (int i = 0; i < orientations.Length; i++) {
                mat[orientations[i].BoneIndex] =
                    Matrix.CreateTranslation(-orientations[i].Origin) *
                    Matrix.CreateRotationX(orientations[i].Rotation.X) * Matrix.CreateRotationY(orientations[i].Rotation.Y) * Matrix.CreateRotationZ(orientations[i].Rotation.Z) *
                    Matrix.CreateScale(orientations[i].Scale) *
                    Matrix.CreateTranslation(orientations[i].Origin) *
                    Matrix.CreateTranslation(orientations[i].Position);
            }
            return mat;
        }

        public static Pose Lerp(Pose p1, Pose p2, float t, AnimationKeyFrame.TransitionType transition) {
            Orientation[] o = new Orientation[p1.orientations.Length];
            for (int i = 0; i < o.Length; i++)
                switch (transition) {
                    case AnimationKeyFrame.TransitionType.Linear:
                        o[i] = new Orientation(p1.orientations[i].BoneIndex,
                            Vector3.Lerp(p1.orientations[i].Rotation, p2.orientations[i].Rotation, t),
                            Vector3.Lerp(p1.orientations[i].Position, p2.orientations[i].Position, t),
                            Vector3.Lerp(p1.orientations[i].Scale, p2.orientations[i].Scale, t),
                            Vector3.Lerp(p1.orientations[i].Origin, p2.orientations[i].Origin, t)
                            );
                        break;
                    case AnimationKeyFrame.TransitionType.Smooth:
                        o[i] = new Orientation(p1.orientations[i].BoneIndex,
                            Vector3.SmoothStep(p1.orientations[i].Rotation, p2.orientations[i].Rotation, t),
                            Vector3.SmoothStep(p1.orientations[i].Position, p2.orientations[i].Position, t),
                            Vector3.SmoothStep(p1.orientations[i].Scale, p2.orientations[i].Scale, t),
                            Vector3.SmoothStep(p1.orientations[i].Origin, p2.orientations[i].Origin, t)
                            );
                        break;
                }
            return new Pose(p1.Weight, p1.BoneCount, o);
        }

        internal bool UsesBone(int b) {
            for (int i = 0; i < orientations.Length; i++)
                if (orientations[i].BoneIndex == b)
                    return true;
            return false;
        }
    }

    class AnimationKeyFrame {
        public enum TransitionType {
            Smooth,
            Linear
        }

        public Pose Pose;
        public float TimeLocation;
        public TransitionType transitionType;

        public AnimationKeyFrame(Pose pose, float time) {
            Pose = pose;
            TimeLocation = time;
            transitionType = TransitionType.Linear;
        }
        public AnimationKeyFrame(Pose pose, float time, TransitionType t) {
            Pose = pose;
            TimeLocation = time;
            transitionType = t;
        }
    }

    delegate void KeyframeEvent(AnimationKeyFrame kf);

    class Animation {
        /// <summary>
        /// The higher, the less important
        /// The most important pose will be displayed
        /// </summary>
        public int Weight;
        public List<AnimationKeyFrame> frames;
        public string AnimationName;
        public float Time = 0;
        public float TimeModifier = 1;
        public bool Loop = false;
        public bool Paused = false;
        public bool Playing = false;
        public KeyframeEvent KeyframeReached;
        public Action AnimationCompleted;

        public Animation(string name, int weight) {
            AnimationName = name;
            Weight = weight;
            frames = new List<AnimationKeyFrame>();
        }

        public void Play() {
            Time = 0;
            Playing = true;
            Paused = false;
        }

        public void Resume() {
            Playing = true;
            Paused = false;
        }
        public void Pause() {
            Playing = true;
            Paused = true;
        }

        public void Stop() {
            Time = 0;
            Playing = false;
            Paused = false;
        }

        public void Update(float delta) {
            if (Paused || !Playing)
                return;

            AnimationKeyFrame curKF = null;
            for (int i = frames.Count - 1; i >= 0; i--)
                if (Time > frames[i].TimeLocation) {
                    curKF = frames[i];
                    break;
                }

            Time += delta * TimeModifier;
            
            if (Time > getLastFrame().TimeLocation) {
                AnimationCompleted?.Invoke();

                if (Loop) {
                    Time = 0;
                    Playing = true;
                } else {
                    Time = getLastFrame().TimeLocation;
                    Playing = false;
                }
            }

            AnimationKeyFrame curKF2 = null;
            for (int i = frames.Count - 1; i >= 0; i--)
                if (Time > frames[i].TimeLocation) {
                    curKF2 = frames[i];
                    break;
                }

            if (curKF != curKF2)
                KeyframeReached?.Invoke(curKF2);
        }

        public Pose getCurrentPose() {
            for (int i = frames.Count - 1; i >= 0; i--) {
                if (Time > frames[i].TimeLocation) {
                    AnimationKeyFrame next = frames[(i + 1) % frames.Count];
                    return Pose.Lerp(frames[i].Pose, next.Pose, MathHelper.Clamp((Time - frames[i].TimeLocation) / (next.TimeLocation - frames[i].TimeLocation), 0, 1), frames[i].transitionType);
                } else if (Time == frames[i].TimeLocation)
                    return frames[i].Pose;
            }
            return null;
        }

        public AnimationKeyFrame getFirstFrame() {
            return frames[0];
        }

        public AnimationKeyFrame getLastFrame() {
            return frames[frames.Count - 1];
        }
    }
}
