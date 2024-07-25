using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyTest
{
    public class MotionDataManager : MonoBehaviour
    {
        public string JsonFilePath = "D://temp//test1.json";
        
        public Animator OriginalAnimator;
        private HumanPoseHandler OriginalPoseHandler;
        private HumanPose OriginalPose;

        private List<MuscleValues> replayData;
        private int replayFrameIndex = 0;
        private float FPS = 30;
        private float _fpsDeltaTime;
        private float _updateTimer = 0f;

        void Start()
        {
            _fpsDeltaTime = 1.0f / FPS;

            // Initialize target pose handler
            if (OriginalAnimator != null && OriginalAnimator.avatar != null)
            {
                OriginalPoseHandler = new HumanPoseHandler(OriginalAnimator.avatar, OriginalAnimator.transform);
                OriginalPose = new HumanPose();
                OriginalPoseHandler.GetHumanPose(ref OriginalPose);

                if (OriginalPose.muscles == null)
                {
                    Debug.LogError("targetPose.muscles is null after initialization.");
                }
            }
            else
            {
                Debug.LogError("Target animator or avatar is missing.");
            }

            // Load the JSON file
            LoadData();
        }

        void Update()
        {
            // Replay the motion
            DoReplay();
        }

        private void LoadData()
        {
            if (File.Exists(JsonFilePath))
            {
                string jsonString = File.ReadAllText(JsonFilePath);
                MotionData motionData = JsonUtility.FromJson<MotionData>(jsonString);
                if (motionData != null && motionData.motionFrames != null)
                {
                    replayData = motionData.motionFrames;
                    Debug.Log($"Loaded {replayData.Count} frames from {JsonFilePath}");
                    replayFrameIndex = 0;
                }
                else
                {
                    Debug.LogError("Failed to load motion data. The data is null or malformed.");
                }
            }
            else
            {
                Debug.LogError("No data found at " + JsonFilePath);
            }
        }

        private void DoReplay()
        {
            if (replayData == null || replayData.Count == 0 || OriginalPose.muscles == null)
            {
                return;
            }

            _updateTimer -= Time.deltaTime;
            if (_updateTimer > 0)
                return;
            _updateTimer += _fpsDeltaTime;

            for (int i = 0; i < OriginalPose.muscles.Length; ++i)
            {
                OriginalPose.muscles[i] = replayData[replayFrameIndex].muscleValues[i];
            }

            OriginalAnimator.gameObject.transform.localPosition = replayData[replayFrameIndex].position;
            OriginalAnimator.gameObject.transform.localRotation = replayData[replayFrameIndex].rotation;

            OriginalPoseHandler.SetHumanPose(ref OriginalPose);

            replayFrameIndex += 1;
            if (replayFrameIndex >= replayData.Count)
                replayFrameIndex = 0;
        }
    }

    [System.Serializable]
    public class MuscleValues
    {
        public Vector3 position;
        public Quaternion rotation;
        public float[] muscleValues;
    }

    [System.Serializable]
    public class MotionData
    {
        public List<MuscleValues> motionFrames = new List<MuscleValues>();
    }
}
