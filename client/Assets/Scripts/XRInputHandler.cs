using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class XRInputHandler : MonoBehaviour
{
    public InputActionAsset inputActions;
    public float verticalMoveSpeed = 0.5f;  // 可在 Inspector 中调整的垂直移动速度
    public GameObject xrRig;                // 拖拽 XR Origin (XR Rig) 物体到 Inspector 中

    private InputAction leftGripAction;
    private InputAction leftTriggerAction;
    private InputAction rightGripAction;
    private InputAction rightTriggerAction;
    private InputAction leftHandPosition;
    private InputAction rightHandPosition;
    private InputAction headPosition;

    private InputAction rightUIScrollAction;

    // 新增：上下移动的 Input Actions (使用用户指定的 Action Name: X, Y, A, B)
    private InputAction Y_button; //  对应 Y 键 (上移)
    private InputAction B_button; //  对应 B 键 (上移 - 可选，如果需要两个键都控制上移)
    private InputAction X_button; // 对应 X 键 (下移)
    private InputAction A_button; // 对应 A 键 (下移 - 可选，如果需要两个键都控制下移)

    // 用来存储抓取的力度
    public float leftGripValue;
    public float leftTriggerValue;
    public float rightGripValue;
    public float rightTriggerValue;

    // 用来存储头部和左右手位置
    public Vector3 headPos;
    public Vector3 leftHandPos;
    public Vector3 rightHandPos;

    // 暴露右手 Joystick 值
    public Vector2 rightJoystickValue;

    // 用来存储是否按下扳机的状态
    public bool leftGripPressed;
    public bool leftTriggerPressed;
    public bool rightGripPressed;
    public bool rightTriggerPressed;

    // 新增：存储上下移动按键状态 (对应用户指定的 Action Name: X, Y, A, B)
    public bool moveUpPressed;
    public bool moveDownPressed;

    public bool A_buttonPressed;
    public bool B_buttonPressed;
    public bool X_buttonPressed;
    public bool Y_buttonPressed;


    private void OnEnable()
    {
        leftGripAction = inputActions.FindActionMap("XRI Left Interaction").FindAction("Select Value");
        rightGripAction = inputActions.FindActionMap("XRI Right Interaction").FindAction("Select Value");
        leftTriggerAction = inputActions.FindActionMap("XRI Left Interaction").FindAction("Activate Value");
        rightTriggerAction = inputActions.FindActionMap("XRI Right Interaction").FindAction("Activate Value");
        leftHandPosition = inputActions.FindActionMap("XRI Left").FindAction("Position");
        rightHandPosition = inputActions.FindActionMap("XRI Right").FindAction("Position");
        headPosition = inputActions.FindActionMap("XRI Head").FindAction("Position");

        rightUIScrollAction = inputActions.FindActionMap("XRI Right Interaction").FindAction("UI Scroll");

        // 新增：查找上下移动的 Input Actions (使用用户指定的 Action Name: X, Y, A, B)
        X_button = inputActions.FindActionMap("XRI Left Interaction").FindAction("X");
        Y_button = inputActions.FindActionMap("XRI Left Interaction").FindAction("Y"); 
        A_button = inputActions.FindActionMap("XRI Right Interaction").FindAction("A"); 
        B_button = inputActions.FindActionMap("XRI Right Interaction").FindAction("B"); 

        // 启用输入动作
        leftGripAction.Enable();
        rightGripAction.Enable();
        leftTriggerAction.Enable();
        rightTriggerAction.Enable();
        leftHandPosition.Enable();
        rightHandPosition.Enable();
        headPosition.Enable();

        rightUIScrollAction.Enable();

        // 启用上下移动的 Input Actions
        Y_button.Enable();
        B_button.Enable();
        X_button.Enable();
        A_button.Enable(); 
    }

    private void OnDisable()
    {
        // 禁用输入动作
        leftGripAction.Disable();
        rightGripAction.Disable();
        leftTriggerAction.Disable();
        rightTriggerAction.Disable();
        leftHandPosition.Disable();
        rightHandPosition.Disable();
        headPosition.Disable();

        rightUIScrollAction.Disable();

        // 禁用上下移动的 Input Actions
        Y_button.Disable();
        B_button.Disable(); // 禁用 B 键 (如果使用)
        X_button.Disable();
        A_button.Disable(); // 禁用 A 键 (如果使用)
    }

    private void Update()
    {
        // 获取头部位置
        headPos = headPosition.ReadValue<Vector3>();

        // 获取左右手的位置
        leftHandPos = leftHandPosition.ReadValue<Vector3>();
        rightHandPos = rightHandPosition.ReadValue<Vector3>();

        // 获取扳机/侧键的按下状态（按下强度）
        leftGripValue = leftGripAction.ReadValue<float>();
        leftTriggerValue = leftTriggerAction.ReadValue<float>();
        rightGripValue = rightGripAction.ReadValue<float>();
        rightTriggerValue = rightTriggerAction.ReadValue<float>();

        rightJoystickValue = rightUIScrollAction.ReadValue<Vector2>();

        // 判断扳机是否按下
        leftGripPressed = leftGripValue > 0.9f;
        leftTriggerPressed = leftTriggerValue > 0.9f;
        rightGripPressed = rightGripValue > 0.9f;
        rightTriggerPressed = rightTriggerValue > 0.9f;

        // 获取上下移动按键状态 (对应用户指定的 Action Name: X, Y, A, B)
        //moveDownPressed = (X_button.ReadValue<float>() > 0.1f) || (A_button.ReadValue<float>() > 0.1f); // X 或 A 按下都下移
        moveDownPressed = A_button.ReadValue<float>() > 0.1f;       // X 或 A 按下都下移
        //moveUpPressed = Y_button.ReadValue<float>() > 0.1f;       // Y 按下上移

        A_buttonPressed = A_button.ReadValue<float>() > 0.8f;
        B_buttonPressed = B_button.ReadValue<float>() > 0.8f;
        X_buttonPressed = X_button.ReadValue<float>() > 0.8f;
        Y_buttonPressed = Y_button.ReadValue<float>() > 0.8f;

    }
}