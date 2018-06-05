using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SGGeometry;
using System.Linq;
using System;

public interface IShapeObjectPipeline
{
    Dictionary<string, ShapeObject> GetPipeline();
}

public class ShapeGenerator
{
    Client client;
    Dictionary<string, Meshable> tobeUpdatedExtrusion;
    Dictionary<string, Meshable> tobeUpdatedForm;
    List<string> ID;  
    IShapeObjectPipeline shapePipeline;
    MessageInterpretor interpretor;
    
    public ShapeGenerator(IShapeObjectPipeline pipeline)
    {
        this.shapePipeline = pipeline;

        tobeUpdatedExtrusion = new Dictionary<string, Meshable>();
        tobeUpdatedForm = new Dictionary<string, Meshable>();
        ID = new List<string>();

        client = new Client();
        client.onRecieveCallbacks += OnRecieveData;
        client.ConnectServer();      //连接服务器   

        SetInterpretor(new SketchupInterpretor());       
    }  
    public void OnRecieveData(string data)
    {
        interpretor.Interpret(data);
    }
    private void SetInterpretor(MessageInterpretor interpretor)
    {
        this.interpretor = interpretor;
        if (interpretor.GetType() == typeof(SketchupInterpretor))
            this.SetSketchupInterpretor((SketchupInterpretor)interpretor);
    }
    private void SetSketchupInterpretor(SketchupInterpretor interpretor)
    {
        interpretor.onCreateU += CreatU;     //注册事件
        interpretor.onCreateF += CreatF;
        interpretor.onDelete += Delete;
    }
    public void ConnectClient()
    {
        client.ConnectServer();
    }
    public void CloseClient()
    {
        client.Close();
    }    
    private void UpdateGeometry(string guid, Meshable mb)
    {
        if (!shapePipeline.GetPipeline().ContainsKey(guid))
        {
            Debug.Log("字典添加新元素");
            shapePipeline.GetPipeline().Add(guid, ShapeObject.CreateBasic());
        }
        shapePipeline.GetPipeline()[guid].SetMeshable(mb);
        if (mb.name!= "")
        {
            shapePipeline.GetPipeline()[guid].gameObject.name = mb.name;
        }
        else
            shapePipeline.GetPipeline()[guid].gameObject.name = "gameObject";

    }
    private void UpdateDic(Dictionary<string, Meshable> dic)
    {
        if (dic != null && dic.Values.Count > 0)
        {        
            string[] keys = dic.Keys.ToArray<string>();
            for (int i = 0; i < keys.Length; i++)
            {
                string guid = keys[i];
                if (dic.ContainsKey(guid))
                {
                    Meshable mb = dic[guid];
                    UpdateGeometry(guid, mb);
                }
            }
            dic.Clear();
        }
    }
    private void DeleteObject(string guid)
    {
        Debug.Log("执行删除");
        if (!shapePipeline.GetPipeline().ContainsKey(guid))
        {
            Debug.Log("字典不包含该键:" + guid);
            return;
        }
        ShapeObject so = shapePipeline.GetPipeline()[guid];
        GameObject.Destroy(so.gameObject);
        shapePipeline.GetPipeline().Remove(guid);
    }
  
    //事件关联
    public void CreatU(string id, string name, Vector3[] pts, float h, Vector3 direction)
    {
        Extrusion ext = new Extrusion(pts, h);
        ext.bbox = BoundingBox.CreateFromPoints(ext.vertices, direction);

        tobeUpdatedExtrusion.Add(id, ext);
        tobeUpdatedExtrusion[id].name = name;
    }
    public void CreatF(string id, string name, List<Vector3[]> faces)
    {     
        Form form = new Form();
        foreach (var item in faces)
        {
            Polygon pg = new Polygon(item);
            form.Add(pg);
        }

        tobeUpdatedForm.Add(id, form);
        tobeUpdatedForm[id].name = name;
    }
    public void Delete(string id)
    {
        ID.Add(id);
    }

    //由外界Update调用
    public void Update()
    {
        UpdateDic(tobeUpdatedExtrusion);
        UpdateDic(tobeUpdatedForm);
        if (ID.Count>0)
        {
            foreach (string item in ID)
            {
                DeleteObject(item);
            }
            ID.Clear();
        }       
    }
}
