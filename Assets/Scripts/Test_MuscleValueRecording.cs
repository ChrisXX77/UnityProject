using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace MyTest
{
    public class Test_MuscleValueRecording : MonoBehaviour
    {
        [Header("===Config Setting===")]
        public float FPS = 30;
        public string JsonFilePath = "D://temp//test1.json";
        
        [Header("===Source Avatar===")]
        public Animator sourceAnimator;
        private HumanPoseHandler sourcePoseHandler;
        private HumanPose sourcePose;
        public float[] sourceMusclesValue;
        public string[] sourceMusclesName;

        [Header("===Target Avatar===")]
        public Animator targetAnimator;
        private HumanPoseHandler targetPoseHandler;
        private HumanPose targetPose;

        private MotionData motionData;
        private bool onRecording = false;
        private float _fpsDeltaTime = 0f;
        private float _updateTimer = 0f;

        private MuscleValues[] replayData;
        private bool onReplay = false;
        private int replayFrameIndex = 0;

        private bool isPaused = false;

        void OnGUI()
        {
            GUI.skin.button.border = new RectOffset(8, 8, 8, 8);
            GUI.skin.button.margin = new RectOffset(0, 0, 0, 0);
            GUI.skin.button.padding = new RectOffset(0, 0, 0, 0);

            int BtnHeight = 40;
            int BtnWidth = 150;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Existing buttons for recording and replay
            if (onRecording)
            {
                if (GUILayout.Button($"Record[On]:{motionData.motionFrames.Count}", GUILayout.Width(BtnWidth), GUILayout.Height(BtnHeight)))
                {
                    onRecording = false;
                    SaveData();
                }
            }
            else
            {
                if (GUILayout.Button($"Record[Off]", GUILayout.Width(BtnWidth), GUILayout.Height(BtnHeight)))
                {
                    onRecording = true;
                    motionData = new MotionData();
                    _updateTimer = _fpsDeltaTime;
                }
            }

            if (onReplay)
            {
                if (GUILayout.Button($"Replay[On]:{replayFrameIndex}", GUILayout.Width(BtnWidth), GUILayout.Height(BtnHeight)))
                    onReplay = false;
            }
            else
            {
                if (GUILayout.Button($"Replay[Off]", GUILayout.Width(BtnWidth), GUILayout.Height(BtnHeight)))
                {
                    string jsonString = File.ReadAllText(JsonFilePath);
                    motionData = JsonUtility.FromJson<MotionData>(jsonString);
                    replayData = motionData.motionFrames.ToArray();
                    Debug.Log($"load from{JsonFilePath} frame count:{replayData.Length}");
                    replayFrameIndex = 0;
                    onReplay = true;
                }
            }

            // Save data to Json file and replay on new scene
            if (GUILayout.Button("Save", GUILayout.Width(BtnWidth), GUILayout.Height(BtnHeight)))
            {
                SaveData();
                SceneManager.LoadScene("Replace Scene"); // Replace with your next scene name
            }

            // Pause button
            if (isPaused)
            {
                if (GUILayout.Button($"Pause[On]", GUILayout.Width(BtnWidth), GUILayout.Height(BtnHeight)))
                {
                    isPaused = false;
                }
            }
            else
            {
                if (GUILayout.Button($"Pause[Off]", GUILayout.Width(BtnWidth), GUILayout.Height(BtnHeight)))
                {
                    isPaused = true;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void Start()
        {
            _fpsDeltaTime = 1.0f / FPS;

            sourcePose = new HumanPose();
            sourcePoseHandler = new HumanPoseHandler(sourceAnimator.avatar, sourceAnimator.transform);
            sourcePoseHandler.GetHumanPose(ref sourcePose);
            sourceMusclesValue = sourcePose.muscles;
            sourceMusclesName = HumanTrait.MuscleName;

            targetPose = new HumanPose();
            targetPoseHandler = new HumanPoseHandler(targetAnimator.avatar, targetAnimator.transform);
            targetPoseHandler.GetHumanPose(ref targetPose);
        }

        void Update()
        {

            if (isPaused)
            {
                return; // Skip update logic if paused
            }
            // Control source avatar
            for (int i = 0; i < sourceMusclesValue.Length; ++i)
                sourcePose.muscles[i] = sourceMusclesValue[i];

            sourcePoseHandler.GetHumanPose(ref sourcePose);
            targetPoseHandler.SetHumanPose(ref targetPose);

            // Recording source avatar
            if (onRecording)
                DoRecording();

            // Replay to target avatar
            if (onReplay)
                DoReplay();
        }

        private bool DoRecording()
        {
            _updateTimer -= Time.deltaTime;
            if (_updateTimer > 0)
                return true;
            _updateTimer += _fpsDeltaTime;

            MuscleValues tmpValue = new MuscleValues
            {
                muscleValues = new float[sourcePose.muscles.Length],
                position = sourceAnimator.gameObject.transform.localPosition,
                rotation = sourceAnimator.gameObject.transform.localRotation
            };
            for (int i = 0; i < sourcePose.muscles.Length; ++i)
                tmpValue.muscleValues[i] = sourcePose.muscles[i];

            motionData.motionFrames.Add(tmpValue);
            return false;
        }

        private void DoReplay()
        {
            _updateTimer -= Time.deltaTime;
            if (_updateTimer > 0)
                return;
            _updateTimer += _fpsDeltaTime;

            for (int i = 0; i < targetPose.muscles.Length; ++i)
                targetPose.muscles[i] = replayData[replayFrameIndex].muscleValues[i];

            targetAnimator.gameObject.transform.localPosition = replayData[replayFrameIndex].position;
            targetAnimator.gameObject.transform.localRotation = replayData[replayFrameIndex].rotation;

            targetPoseHandler.SetHumanPose(ref targetPose);

            replayFrameIndex += 1;
            if (replayFrameIndex >= motionData.motionFrames.Count)
                replayFrameIndex = 0;
        }

        private void SaveData()
        {
            //Save Data to JsonFile
            if (motionData != null)
            {
                File.WriteAllText(JsonFilePath, JsonUtility.ToJson(motionData));
                Debug.Log("Data saved to: " + JsonFilePath);
            }
        }
    }

    //[Serializable]
    // public class MuscleValues
    // {
    //     public Vector3 position;
    //     public Quaternion rotation;
    //     public float[] muscleValues;
    // }

    // [Serializable]
    // public class MotionData
    // {
    //     public List<MuscleValues> motionFrames = new List<MuscleValues>();
    // }
}




/* 95個Muscles對應
    List<string> tmp=new ();
    for(int i=0;i<HumanTrait.MuscleName.Length;++i)
        tmp.Add($"[{i}]{HumanTrait.MuscleName[i]}");
    Debug.Log(string.Join("\n", tmp));

    [0]Spine Front-Back
    [1]Spine Left-Right
    [2]Spine Twist Left-Right
    [3]Chest Front-Back
    [4]Chest Left-Right
    [5]Chest Twist Left-Right
    [6]UpperChest Front-Back
    [7]UpperChest Left-Right
    [8]UpperChest Twist Left-Right
    [9]Neck Nod Down-Up
    [10]Neck Tilt Left-Right
    [11]Neck Turn Left-Right
    [12]Head Nod Down-Up
    [13]Head Tilt Left-Right
    [14]Head Turn Left-Right
    [15]Left Eye Down-Up
    [16]Left Eye In-Out
    [17]Right Eye Down-Up
    [18]Right Eye In-Out
    [19]Jaw Close
    [20]Jaw Left-Right
    [21]Left Upper Leg Front-Back
    [22]Left Upper Leg In-Out
    [23]Left Upper Leg Twist In-Out
    [24]Left Lower Leg Stretch
    [25]Left Lower Leg Twist In-Out
    [26]Left Foot Up-Down
    [27]Left Foot Twist In-Out
    [28]Left Toes Up-Down
    [29]Right Upper Leg Front-Back
    [30]Right Upper Leg In-Out
    [31]Right Upper Leg Twist In-Out
    [32]Right Lower Leg Stretch
    [33]Right Lower Leg Twist In-Out
    [34]Right Foot Up-Down
    [35]Right Foot Twist In-Out
    [36]Right Toes Up-Down
    [37]Left Shoulder Down-Up
    [38]Left Shoulder Front-Back
    [39]Left Arm Down-Up
    [40]Left Arm Front-Back
    [41]Left Arm Twist In-Out
    [42]Left Forearm Stretch
    [43]Left Forearm Twist In-Out
    [44]Left Hand Down-Up
    [45]Left Hand In-Out
    [46]Right Shoulder Down-Up
    [47]Right Shoulder Front-Back
    [48]Right Arm Down-Up
    [49]Right Arm Front-Back
    [50]Right Arm Twist In-Out
    [51]Right Forearm Stretch
    [52]Right Forearm Twist In-Out
    [53]Right Hand Down-Up
    [54]Right Hand In-Out
    [55]Left Thumb 1 Stretched
    [56]Left Thumb Spread
    [57]Left Thumb 2 Stretched
    [58]Left Thumb 3 Stretched
    [59]Left Index 1 Stretched
    [60]Left Index Spread
    [61]Left Index 2 Stretched
    [62]Left Index 3 Stretched
    [63]Left Middle 1 Stretched
    [64]Left Middle Spread
    [65]Left Middle 2 Stretched
    [66]Left Middle 3 Stretched
    [67]Left Ring 1 Stretched
    [68]Left Ring Spread
    [69]Left Ring 2 Stretched
    [70]Left Ring 3 Stretched
    [71]Left Little 1 Stretched
    [72]Left Little Spread
    [73]Left Little 2 Stretched
    [74]Left Little 3 Stretched
    [75]Right Thumb 1 Stretched
    [76]Right Thumb Spread
    [77]Right Thumb 2 Stretched
    [78]Right Thumb 3 Stretched
    [79]Right Index 1 Stretched
    [80]Right Index Spread
    [81]Right Index 2 Stretched
    [82]Right Index 3 Stretched
    [83]Right Middle 1 Stretched
    [84]Right Middle Spread
    [85]Right Middle 2 Stretched
    [86]Right Middle 3 Stretched
    [87]Right Ring 1 Stretched
    [88]Right Ring Spread
    [89]Right Ring 2 Stretched
    [90]Right Ring 3 Stretched
    [91]Right Little 1 Stretched
    [92]Right Little Spread
    [93]Right Little 2 Stretched
    [94]Right Little 3 Stretched     
    */

    //x-box motion is incorrect. Something wrong