using System;
using UnityEngine;

[DisallowMultipleComponent]
public class GameInput : MonoBehaviour
{
    public enum ActionName
    {
        Left,
        Right,
        Up,
        Down,
        Jump,
        Dash,
        Grab
    }

    [Serializable]
    private struct BindingState
    {
        public KeyCode left;
        public KeyCode right;
        public KeyCode up;
        public KeyCode down;
        public KeyCode jump;
        public KeyCode dash;
        public KeyCode grab;
    }

    private const string BindingsKey = "game_input_bindings";

    private static GameInput instance;

    private BindingState bindings = new BindingState
    {
        left = KeyCode.A,
        right = KeyCode.D,
        up = KeyCode.W,
        down = KeyCode.S,
        jump = KeyCode.Space,
        dash = KeyCode.Return,
        grab = KeyCode.LeftShift
    };

    private bool awaitingRebind;
    private ActionName pendingAction;
    private Action<KeyCode> rebindCompleted;

    public static GameInput Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameInput>();
            }

            return instance;
        }
    }

    public bool AwaitingRebind => awaitingRebind;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadBindings();
    }

    private void Update()
    {
        if (!awaitingRebind)
        {
            return;
        }

        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(keyCode))
            {
                continue;
            }

            SetBinding(pendingAction, keyCode);
            awaitingRebind = false;
            rebindCompleted?.Invoke(keyCode);
            rebindCompleted = null;
            SaveBindings();
            break;
        }
    }

    public float GetHorizontal()
    {
        return GetAxis(GetKey(ActionName.Left), GetKey(ActionName.Right));
    }

    public float GetVertical()
    {
        return GetAxis(GetKey(ActionName.Down), GetKey(ActionName.Up));
    }

    public float GetHorizontalRaw()
    {
        return GetAxis(GetKey(ActionName.Left), GetKey(ActionName.Right));
    }

    public float GetVerticalRaw()
    {
        return GetAxis(GetKey(ActionName.Down), GetKey(ActionName.Up));
    }

    public bool GetJumpPressed()
    {
        return GetKeyDown(ActionName.Jump);
    }

    public bool GetJumpHeld()
    {
        return GetKey(ActionName.Jump);
    }

    public bool GetDashPressed()
    {
        return GetKeyDown(ActionName.Dash);
    }

    public bool GetGrabHeld()
    {
        return GetKey(ActionName.Grab);
    }

    public string GetBindingLabel(ActionName action)
    {
        return GetBinding(action).ToString();
    }

    public void BeginRebind(ActionName action, Action<KeyCode> onComplete)
    {
        awaitingRebind = true;
        pendingAction = action;
        rebindCompleted = onComplete;
    }

    private static float GetAxis(bool negative, bool positive)
    {
        if (negative == positive)
        {
            return 0f;
        }

        return positive ? 1f : -1f;
    }

    private bool GetKey(ActionName action)
    {
        return Input.GetKey(GetBinding(action));
    }

    private bool GetKeyDown(ActionName action)
    {
        return Input.GetKeyDown(GetBinding(action));
    }

    private KeyCode GetBinding(ActionName action)
    {
        switch (action)
        {
            case ActionName.Left:
                return bindings.left;
            case ActionName.Right:
                return bindings.right;
            case ActionName.Up:
                return bindings.up;
            case ActionName.Down:
                return bindings.down;
            case ActionName.Jump:
                return bindings.jump;
            case ActionName.Dash:
                return bindings.dash;
            case ActionName.Grab:
                return bindings.grab;
            default:
                return KeyCode.None;
        }
    }

    private void SetBinding(ActionName action, KeyCode keyCode)
    {
        switch (action)
        {
            case ActionName.Left:
                bindings.left = keyCode;
                break;
            case ActionName.Right:
                bindings.right = keyCode;
                break;
            case ActionName.Up:
                bindings.up = keyCode;
                break;
            case ActionName.Down:
                bindings.down = keyCode;
                break;
            case ActionName.Jump:
                bindings.jump = keyCode;
                break;
            case ActionName.Dash:
                bindings.dash = keyCode;
                break;
            case ActionName.Grab:
                bindings.grab = keyCode;
                break;
        }
    }

    private void LoadBindings()
    {
        if (!PlayerPrefs.HasKey(BindingsKey))
        {
            return;
        }

        BindingState loaded = JsonUtility.FromJson<BindingState>(PlayerPrefs.GetString(BindingsKey));
        if (loaded.left == KeyCode.None || loaded.right == KeyCode.None || loaded.jump == KeyCode.None)
        {
            return;
        }

        bindings = loaded;
    }

    private void SaveBindings()
    {
        PlayerPrefs.SetString(BindingsKey, JsonUtility.ToJson(bindings));
        PlayerPrefs.Save();
    }
}
