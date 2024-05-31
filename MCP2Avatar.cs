/**
  MCP2Avatar - simple receiver of mocopi protocol

  License: MIT License (c) 2024 Mitsumine Suzu(verylowfreq)
 */


using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;

public class MCP2Avatar : MonoBehaviour
{
    public bool autoStart = true;
    public UInt16 Port = 12351;
    public Animator Avatar;

    UdpClient udpClient;


    Queue<byte[]> ReceivedMessages = new Queue<byte[]>();

    Dictionary<HumanBodyBones, Quaternion> TargetRotations = new Dictionary<HumanBodyBones, Quaternion>();

    // Start is called before the first frame update
    void Start()
    {
        if (autoStart) {
            this.StartListen();
        }
    }

    // Update is called once per frame
    void Update()
    {
        byte[] message;
        while (this.ReceivedMessages.TryDequeue(out message)) {
            var bones = ParseSMFMessages(message);

            UpdateBonesTarget(this.Avatar, bones);
            ConvertBonesMirror(this.Avatar);
        }
    }

    public void StartListen() {
        if (this.udpClient != null) {
            this.StopListen();
        }

        IPEndPoint localEP = new IPEndPoint(IPAddress.Any, this.Port);
        this.udpClient = new UdpClient(localEP);
        this.udpClient.BeginReceive(OnUdpReceived, localEP);

        Debug.Log($"MCP2Avatar listning on UDP {Port}");
    }

    public void StopListen() {
        this.udpClient.Close();
        this.udpClient.Dispose();
        this.udpClient = null;
    }

    public void ChangePort(UInt16 newPort) {
        this.Port = newPort;
        this.StartListen();
    }

    void OnUdpReceived(IAsyncResult asyncResult) {
        IPEndPoint remoteEP = (IPEndPoint)asyncResult.AsyncState;
        if (this.udpClient != null) {
            byte[] message = this.udpClient.EndReceive(asyncResult, ref remoteEP);
            this.ReceivedMessages.Enqueue(message);
        }

        if (this.udpClient != null) {
            this.udpClient.BeginReceive(OnUdpReceived, remoteEP);
        }
    }

    public ref struct SMFData {
        public UInt32 Size;
        public string FourCC;
        public ReadOnlySpan<byte> Data;

        public override string ToString()
        {
            return $"<SMFData: {Size}bytes, {FourCC}>";
        }
    }

    public struct SMFBoneTransform {
        public Int16 BoneID;
        public Quaternion Rotation;
        public Vector3 Postion;
    }

    public Dictionary<Int16, HumanBodyBones> SMFBoneTable = new Dictionary<short, HumanBodyBones>() {
        { 0, HumanBodyBones.Hips },
        { 3, HumanBodyBones.Spine },
        { 5, HumanBodyBones.Chest },
        { 8, HumanBodyBones.Neck },
        { 10, HumanBodyBones.Head },

        // { 11, HumanBodyBones.LeftShoulder },
        // { 12, HumanBodyBones.LeftUpperArm } ,
        // { 13, HumanBodyBones.LeftLowerArm },
        // { 14, HumanBodyBones.LeftHand },

        // { 15, HumanBodyBones.RightShoulder },
        // { 16, HumanBodyBones.RightUpperArm },
        // { 17, HumanBodyBones.RightLowerArm },
        // { 18, HumanBodyBones.RightHand},

        // { 19, HumanBodyBones.LeftUpperLeg },
        // { 20, HumanBodyBones.LeftLowerLeg },
        // { 21, HumanBodyBones.LeftFoot },
        // { 22, HumanBodyBones.LeftToes },

        // { 23, HumanBodyBones.RightUpperLeg },
        // { 24, HumanBodyBones.RightLowerLeg },
        // { 25, HumanBodyBones.RightFoot },
        // { 26, HumanBodyBones.RightToes }

        { 11, HumanBodyBones.RightShoulder },
        { 12, HumanBodyBones.RightUpperArm },
        { 13, HumanBodyBones.RightLowerArm },
        { 14, HumanBodyBones.RightHand},

        { 15, HumanBodyBones.LeftShoulder },
        { 16, HumanBodyBones.LeftUpperArm } ,
        { 17, HumanBodyBones.LeftLowerArm },
        { 18, HumanBodyBones.LeftHand },

        { 19, HumanBodyBones.RightUpperLeg },
        { 20, HumanBodyBones.RightLowerLeg },
        { 21, HumanBodyBones.RightFoot },
        { 22, HumanBodyBones.RightToes },

        { 23, HumanBodyBones.LeftUpperLeg },
        { 24, HumanBodyBones.LeftLowerLeg },
        { 25, HumanBodyBones.LeftFoot },
        { 27, HumanBodyBones.LeftToes }

    };

    SMFBoneTransform[] ParseSMFMessages(byte[] message) {
        int cur = 0;
        var bones = new List<SMFBoneTransform>();
        SMFBoneTransform bone = new SMFBoneTransform();
        bone.BoneID = -1;
        bool inBtdt = false;
        int cnt = 4096;
        while (cur < message.Length) {
            if (cnt == 0) {
                Debug.Log("Force exit from loop");
                break;
            } else {
                cnt -= 1;
            }
            SMFData smfdata = ParseSMF2(message, cur, message.Length - cur);

            if (false) {
            // if (smfdata.FourCC == "head") {
            //     // Data format description and version information
            //     cur += 8 + (int)smfdata.Size;

            // } else if (smfdata.FourCC == "sndf") {
            //     // Sender informations
            //     cur += 8 + (int)smfdata.Size;

            // } else if (smfdata.FourCC == "skdf") {
            //     // Bone definitions and pose
            //     cur += 8 + (int)smfdata.Size;
                
            } else if (smfdata.FourCC == "fram") {
                // Bone Data Array box
                // Get 'bndt'
                cur += 8;

            } else if (smfdata.FourCC == "btrs") {
                // Get [ (bnid, pdid, tran)]
                cur += 8;

            } else if (smfdata.FourCC == "btdt") {
                inBtdt = true;
                cur += 8;

            } else if (smfdata.FourCC == "bnid") {
                if (inBtdt) {
                    bone.BoneID = BitConverter.ToInt16(smfdata.Data);
                }
                cur += 8 + (int)smfdata.Size;

            } else if (smfdata.FourCC == "tran") {
                // Debug.Log($"tran for bone id = {bone.BoneID}");
                if (inBtdt) {
                    var floatBytes = new List<byte[]>();
                    for (int i = 0; i < 7; i++) {
                        var floatByte = new byte[4] {
                            smfdata.Data[i * 4 + 0],
                            smfdata.Data[i * 4 + 1],
                            smfdata.Data[i * 4 + 2],
                            smfdata.Data[i * 4 + 3]
                        };
                        floatBytes.Add(floatByte);
                    }
                    
                    float[] floats = new float[7];
                    for (int i = 0; i < 7; i++) {
                        byte[] byteArray;
                        try {
                            byteArray = floatBytes[i];
                        } catch (Exception) {
                            Debug.Log($"i = {i}");
                            throw;
                        }
                        float val = BitConverter.ToSingle(byteArray.AsSpan());
                        floats[i] = val;
                    }
                    bone.Rotation = new Quaternion(floats[0], floats[1], floats[2], floats[3]);
                    bone.Postion = new Vector3(floats[4], floats[5], floats[6]);
                    bones.Add(bone);

                    bone = new SMFBoneTransform();
                }
                cur += 8 + (int)smfdata.Size;

            } else {
                // Debug.Log($"Unknown FourCC: {smfdata.FourCC}");
                cur += 8 + (int)smfdata.Size;
            }
        }

        return bones.ToArray();
    }


    SMFData ParseSMF2(byte[] data, int offset, int length) {
        SMFData d = new SMFData();

        UInt32 size = 0;
        size = (UInt32)((data[offset+3] << 24) + (data[offset+2] << 16) + (data[offset+1] << 8) + (data[offset+0]));
        d.Size = size;

        var asciiEncoding = new System.Text.ASCIIEncoding();
        string fourcc = asciiEncoding.GetString(data.AsSpan<byte>((int)offset + 4, (int)4));
        d.FourCC = fourcc;

        d.Data = data.AsSpan<byte>((int)offset + 8, (int)d.Size);

        return d;
    }


    void UpdateBonesTarget(Animator avatar, SMFBoneTransform[] bones) {
        foreach (var bone in bones) {
            if (!SMFBoneTable.Keys.Contains(bone.BoneID)) {
                continue;
            }
            HumanBodyBones targetBone = SMFBoneTable[bone.BoneID];
            Transform targetTransform = avatar.GetBoneTransform(targetBone);
            var rot = bone.Rotation;

            targetTransform.localRotation = rot;
            
            if (targetBone == HumanBodyBones.Hips) {
                Vector3 pos = new Vector3(bone.Postion.x * -1, bone.Postion.y, bone.Postion.z * 1);
                targetTransform.localPosition = pos;
            }
        }
    }

    Quaternion MirrorQuaternion(Quaternion rot) {
        return new Quaternion(rot.x, rot.y * -1, rot.z* -1, rot.w );
    }

    void MirrorTransform(Transform bone1, Transform bone2) {
        var rot1 = bone1.localRotation;
        bone1.localRotation = MirrorQuaternion(bone2.localRotation);
        bone2.localRotation = MirrorQuaternion(rot1);
    }

    List<(HumanBodyBones, HumanBodyBones)> boneMirroTable = new List<(HumanBodyBones, HumanBodyBones)>() {
        (HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder),
        (HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm),
        (HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm),
        (HumanBodyBones.LeftHand, HumanBodyBones.RightHand),
        (HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg),
        (HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg),
        (HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot),
        (HumanBodyBones.LeftToes, HumanBodyBones.RightToes)
    };

    void ConvertBonesMirror(Animator avatar) {
        foreach (var smfBone in this.SMFBoneTable) {
            var bone = smfBone.Value;
            var mirrorEntry = boneMirroTable.Where(e=>e.Item1 == bone || e.Item2 == bone).ToArray();
            if (mirrorEntry.Count() >= 1) {
                var pair = mirrorEntry.First();
                if (pair.Item1 == bone) {
                    var boneL = avatar.GetBoneTransform(pair.Item1);
                    var boneR = avatar.GetBoneTransform(pair.Item2);
                    MirrorTransform(boneL, boneR);
                }
            } else {
                var boneTransform = avatar.GetBoneTransform(bone);
                boneTransform.localRotation = MirrorQuaternion(boneTransform.localRotation);
            }
        }
    }
}
