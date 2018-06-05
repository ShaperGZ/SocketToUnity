using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppExecutor : MonoBehaviour, IShapeObjectPipeline
{
    Dictionary<string, ShapeObject> shapeObjects;
    ShapeGenerator shapeGenerator;
  
    public Dictionary<string, ShapeObject> GetPipeline()
    {
        return shapeObjects;
    }
    void Start()
    {
        shapeObjects = new Dictionary<string, ShapeObject>();     
        shapeGenerator = new ShapeGenerator(this);
    }
    void Update()
    {
        shapeGenerator.Update();
    }
    void OnDisable()
    {
        shapeGenerator.CloseClient();      //关闭服务器
    }  
}
