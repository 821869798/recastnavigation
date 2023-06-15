using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

//可动摇杆,适合移动摇杆
public class Joystick : MonoBehaviour
{
    [Tooltip("编辑器按键支持")]
    public bool editorKeySupport;

    /// <summary>
    /// 事件的相机，默认读取pressEventCamera
    /// </summary>
    [HideInInspector]
    public Camera eventCamera;

    [Tooltip("摇杆的transform(null，默认是自己)")]
    public RectTransform jsTransform;

    public RectTransform JoyStickTransform
    {
        get
        {
            if (jsTransform == null)
            {
                jsTransform = this.transform as RectTransform;
            }
            return jsTransform;
        }
    }

    //摇杆圆心（Stick）
    public RectTransform Stick;
    //Stick移动半径（UGUI像素）
    [Tooltip("摇杆的半径")]
    public float StickRadius = 200.0f;

    [Tooltip("摇杆是否是固定的")]
    public bool CenterPosFixed = true;
    //摇杆移动半径（UGUI像素）(固定摇杆时该参数不需要)
    [Tooltip("动态摇杆移动半径(固定摇杆时该参数不需要)")]
    public float MoveRadius = 300.0f;

    //摇杆还原时间
    public float replaceTime = 0.1f;

    //摇杆的事件
    public event Action onTouchDown;
    public event Action<JoystickData> onTouchMove;
    public event Action onTouchUp;

    //按下的原点
    private Vector2 touchOrigin;
    //自己和Stick的默认位置
    private Vector2 selfDefaultPosition;
    private Vector2 stackDefaultLocalPos;
    //摇杆事件的参数
    public JoystickData data = new JoystickData();

    //Canvas 的转换因子
    //private float scaleFactor;

    //是否正在复位
    private bool isReplace = false;
    //复位参数
    private float replaceCount = 0f;
    private Vector2 selfReplaceSpd;
    private Vector2 stickReplaceSpd;

    //是否正在拖拽
    private bool isDragged = false;

    private bool isStarted = false;

    // Use this for initialization
    void Awake()
    {
        //初始化
        InitJoyStick();

        //获取转换系数
        //Canvas canvas = this.GetComponentInParent<Canvas>();
        //scaleFactor = canvas.scaleFactor;
        //绑定操作事件
        EventTriggerListener.Get(this.gameObject).onDrag += OnJoystickDragAction;
        EventTriggerListener.Get(this.gameObject).onUp += OnJoystickUpAction;
        EventTriggerListener.Get(this.gameObject).onDown += OnJoystickDownAction;
        EventTriggerListener.Get(this.gameObject).onEndDrag += OnJoystickEndDragAction;
        EventTriggerListener.Get(this.gameObject).DeselectTriggerEndDrag = false;
    }

    public void InitJoyStick()
    {
        //初始化赋值
        selfDefaultPosition = JoyStickTransform.anchoredPosition;
        stackDefaultLocalPos = Stick.anchoredPosition;
    }

    void OnDisable()
    {
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDragged && onTouchMove != null)
        {
            //派发事件
            onTouchMove(data);
        }
        if (isReplace)
        {
            replaceCount += Time.deltaTime;
            if (replaceCount < replaceTime)
            {
                JoyStickTransform.anchoredPosition += selfReplaceSpd * Time.deltaTime;
                Stick.anchoredPosition += stickReplaceSpd * Time.deltaTime;
            }
            else
            {
                isReplace = false;
                JoyStickTransform.anchoredPosition = selfDefaultPosition;
                Stick.anchoredPosition = stackDefaultLocalPos;
            }
        }

        EditorKeyAdapt();

    }

#if UNITY_EDITOR || UNITY_STANDALONE
    //编辑器开始
    private bool editorKeyStart = false;
#endif

    void EditorKeyAdapt()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!editorKeySupport)
        {
            return;
        }

        if(!Input.anyKey)
        {
            if(editorKeyStart)
            {
                editorKeyStart = false;
                TouchUp();
            }
            return;
        }

        float hor = Input.GetAxisRaw("Horizontal");
        float ver = Input.GetAxisRaw("Vertical");
        if(Mathf.Abs(hor) < 1e-3 && Mathf.Abs(ver) < 1e-3)
        {
            if(editorKeyStart)
            {
                editorKeyStart = false;
                TouchUp();
            }
            return;
        }

        Vector3 touchPos = new Vector3(hor * StickRadius, ver * StickRadius);
        if(!editorKeyStart && !isStarted)
        {
            editorKeyStart = true;
            isStarted = true;
            isReplace = false;
            if (onTouchDown != null)
                onTouchDown();

            TouchMove(touchPos);
        }
        else if(editorKeyStart)
        {
            TouchMove(touchPos);
        }

#endif
    }

    private void OnJoystickDownAction(GameObject go, PointerEventData eventData)
    {
        TouchDown(eventData);
    }

    private void OnJoystickDragAction(GameObject go, PointerEventData eventData)
    {
        if (!isStarted)
            return;
        var touchPosition = GetTouchLocalPostion(eventData, Stick.parent as RectTransform);
        TouchMove(touchPosition);
        //Debug.Log($"screenPoint:{eventData.position} pressPoint:{eventData.pressPosition} uiPoint:" + touchPosition);
    }


    private void OnJoystickUpAction(GameObject go, PointerEventData eventData)
    {
        if (!isStarted)
            return;
        //Debug.Log("OnJoystickUpAction");
        TouchUp();
    }

    private void OnJoystickEndDragAction(GameObject go, PointerEventData eventData)
    {
        if (!isStarted)
            return;
        //Debug.Log("OnJoystickEndDragAction");
        TouchUp();
    }

    /// <summary>
    /// 获取UI的AnchorPosition
    /// </summary>
    /// <param name="eventData"></param>
    /// <returns></returns>
    private Vector2 GetTouchLocalPostion(PointerEventData eventData, RectTransform rectTransform)
    {
        Camera camera = eventCamera;
        if (camera == null)
        {
            camera = eventData.pressEventCamera;
        }
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
           rectTransform,
           eventData.position,
           camera,
           out var localPos
       ))
        {
            return Vector2.zero;
        }
        return localPos;
    }


    private void TouchDown(PointerEventData eventData)
    {
        if (isStarted)
        {
            return;
        }

        isStarted = true;
        isReplace = false;

        if (onTouchDown != null)
            onTouchDown();

        if (!CenterPosFixed)
        {
            //中心不固定的活动型摇杆
            var joyStickTouchPos = GetTouchLocalPostion(eventData, JoyStickTransform.parent as RectTransform);

            //移动摇杆
            Vector2 direction = joyStickTouchPos - selfDefaultPosition;
            float distance = Vector3.Distance(joyStickTouchPos, selfDefaultPosition);
            if (distance > MoveRadius)
            {
                float radians = Mathf.Atan2(direction.y, direction.x);
                distance = MoveRadius;
                float mx = Mathf.Cos(radians) * distance;
                float my = Mathf.Sin(radians) * distance;
                Vector3 uiPos = selfDefaultPosition;
                uiPos.x += mx;
                uiPos.y += my;

                JoyStickTransform.anchoredPosition = uiPos;

                var touchPosition = GetTouchLocalPostion(eventData, Stick.parent as RectTransform);

                TouchMove(touchPosition);
            }
            else
            {
                JoyStickTransform.anchoredPosition = joyStickTouchPos;

                var touchPosition = GetTouchLocalPostion(eventData, Stick.parent as RectTransform);
                
                TouchMove(touchPosition);
            }
        }
        else
        {
            //固定摇杆
            var touchPosition = GetTouchLocalPostion(eventData, Stick.parent as RectTransform);
            TouchMove(touchPosition);
        }


    }

    private void TouchMove(Vector3 inputPosition)
    {
        Vector3 touchOrigin = stackDefaultLocalPos;
        Vector3 now = inputPosition;
        float distance = Vector3.Distance(now, touchOrigin);
        if (distance < 0.01f)
            return;

        isDragged = true;

        Vector3 direction = now - touchOrigin;
        float radians = Mathf.Atan2(direction.y, direction.x);

        //移动摇杆
        if (Stick != null)
        {
            if (distance > StickRadius)
                distance = StickRadius;

            float mx = Mathf.Cos(radians) * distance;
            float my = Mathf.Sin(radians) * distance;
            Vector3 uiPos = stackDefaultLocalPos;
            uiPos.x += mx;
            uiPos.y += my;
            Stick.anchoredPosition = uiPos;
        }

        //得到新的摇杆参数
        data.power = distance / StickRadius;
        data.radians = radians;
        data.angle = radians * Mathf.Rad2Deg;
        data.angle360 = data.angle < 0 ? 360 + data.angle : data.angle;
    }
    private void TouchUp()
    {
        //isOnArea = false;
        isDragged = false;
        isStarted = false;
        //一种插值还原，一种瞬间还原
        if (replaceTime > 0f)
        {
            isReplace = true;
            replaceCount = 0f;
            selfReplaceSpd = (selfDefaultPosition - JoyStickTransform.anchoredPosition) / replaceTime;
            stickReplaceSpd = (stackDefaultLocalPos - Stick.anchoredPosition) / replaceTime;
        }
        else
        {
            ReplaceImmediate();
        }


        if (onTouchUp != null)
            onTouchUp();
    }

    //立即还原
    public void ReplaceImmediate()
    {
        isReplace = false;
        JoyStickTransform.anchoredPosition = selfDefaultPosition;
        Stick.anchoredPosition = stackDefaultLocalPos;
    }

    public void Reset()
    {
        //isOnArea = false;
        isDragged = false;
        isReplace = false;
        isStarted = false;
        ReplaceImmediate();
    }
}