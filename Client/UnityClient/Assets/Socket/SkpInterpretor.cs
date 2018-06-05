using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SketchupInterpretor : MessageInterpretor
{
    public delegate void CreateU(string id,string name,Vector3[] pts,float h, Vector3 direction);
    public delegate void CreateF(string id,string name, List<Vector3[]> faces);
    public delegate void Delete(string id);
    public event CreateU onCreateU;
    public event CreateF onCreateF;
    public event Delete onDelete;

    public override void Interpret(string data)
    {     
        Debug.Log(data);
        //if (data == "")
        //{
        //    Debug.Log("开始发送");
        //    client.mSocket.SendMessage("");
        //    return;
        //}
        List<Vector3> vlist = new List<Vector3>();
        List<Vector3[]> vlists = new List<Vector3[]>();

        string id;
        string name;
        float height;

        if (data[0] == 'U')
        {
            string[] newData = data.Split('|');
            id = GetStringFormN(newData[1], 6);
            name = newData[2];
            height = Convert.ToSingle(newData[3]);
            string[] xa = newData[4].Split(',');
            Vector3 vxa = new Vector3(GetNumberInt(xa[0]), GetNumberInt(xa[1]), GetNumberInt(xa[2]));
            for (int i = 5; i < newData.Length; i++)
            {
                string Ve = newData[i];
                string[] Ves = Ve.Split(',');
                Vector3 pos = new Vector3(GetNumberInt(Ves[0]), GetNumberInt(Ves[1]), GetNumberInt(Ves[2]));
                vlist.Add(pos);
            }

             onCreateU(id, name, vlist.ToArray(),height,vxa);
        }
        else if (data[0] == 'D')
        {
            string[] newData = data.Split('|');
            id = GetStringFormN(newData[1], 6);
            onDelete(id);
        }
        else if (data[0] == 'F')
        {
            string[] newData = data.Split('|');
            id = GetStringFormN(newData[1], 6);
            name = newData[2];
            for (int i = 3; i < newData.Length - 1; i++)
            {
                List<Vector3> vector3s = new List<Vector3>();
                string[] Ves = newData[i].Split('_');
                for (int j = 0; j < Ves.Length - 1; j++)
                {
                    string[] num = Ves[j].Split(',');
                    Vector3 pos = new Vector3(GetNumberInt(num[0]), GetNumberInt(num[1]), GetNumberInt(num[2]));
                    vector3s.Add(pos);
                }
                vlists.Add(vector3s.ToArray());
                vector3s.Clear();
            }

            onCreateF(id, name, vlists);
        }
    }

    //提取字符串中的数字(含小数点、负号)
    public static float GetNumberInt(string str)
    {
        float result = 0;
        if (str != null && str != string.Empty)
        {
            str = Regex.Match(str, @"[+-]?\d+[\.]?\d*").Value;
            result = Convert.ToSingle(str);
        }
        return result;
    }
    //提取字符串前n位字符
    public static string GetStringFormN(string str, int n)
    {
        char[] c = str.ToCharArray();
        string re = "";
        for (int i = 0; i < n; i++)
        {
            re += c[i].ToString();
        }
        return re;
    }
}