using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarKActions : MonoBehaviour
{
    public int mObjectID = 1;
    RectTransform mObjectRectform;
    float mSpeed = 8.0f;
    // Start is called before the first frame update
    void Start()
    {



        mObjectRectform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ObjectDetect.mDetectObjectIndex != null && ObjectDetect.mObjectDic.ContainsKey(mObjectID))//检测该ID代表的物体是否存在在列表中，mObjectID代表需要获取的物体的ID号
        {
            //DetectObjectDetails DOD = ObjectDetect.mDetectObject[ObjectDetect.mDetectObjectIndex[mObjectID]];//根据ID号，获取该物体在列表中的位置，并获取该物体的所有信息
            DetectObjectDetails DOD = ObjectDetect.mObjectDic[mObjectID];
            Vector3 objectCenterPos = DOD.objectCenterPosition;//获取物体位置信息
            float objectAngle = DOD.objectRotationAngle;//获取物体旋转角度
            switch (DOD.objectstate)//根据不同状态，进行对应的操作
            {
                case ObjectState.Start:
                    {
                        if(Screen.width == 3840)
                            mObjectRectform.localScale = new Vector3(2, 2, 2);
                        else
                            mObjectRectform.localScale = Vector3.one;
                        //开始被识别时的操作2,2,2
                    }
                    break;
                case ObjectState.Move:
                    {

                        Vector2 tempPosition = objectCenterPos;
                        //float deltaTime = Time.deltaTime;
                        Vector2 position = Vector2.Lerp(mObjectRectform.anchoredPosition, tempPosition, Time.deltaTime * 10);
                        mObjectRectform.anchoredPosition = position;// position;

                        Quaternion rotation = Quaternion.Euler(0, 0, -objectAngle);
                        mObjectRectform.rotation = Quaternion.Lerp(mObjectRectform.rotation, rotation, Time.deltaTime * 10);
                        Debug.Log("移动");

                        //移动物体时的操作
                    }
                    break;
 
                case ObjectState.End:
                    {
                        mObjectRectform.localScale = Vector3.zero;
                        //物体离开桌面时的操作
                    }
                    break;
                case ObjectState.Undetect:
                    //没被识别时的操作，一般可以不处理
                    break;
            }
        }
    }
}
