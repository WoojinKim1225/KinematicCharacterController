using UnityEngine;

namespace StatefulVariables // Assuming your ReferenceManager class is in this namespace
{
    [System.Serializable]
    public class Bool
    {
        [SerializeField] private bool _value;
        private bool _isChanged;
        private bool _beforeValue;
        private bool _initialValue;

        public bool Value { get => _value; set => _value = value; }

        public bool IsChanged => _isChanged;
        public bool BeforeValue => _beforeValue;

        public bool InitialValue { get => _initialValue; set => _initialValue = value; }

        public Bool(bool v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }

        public void OnUpdate(bool v)
        {
            _beforeValue = _value;
            _value = v;
            _isChanged = _beforeValue != _value;
        }

        public void OnUpdate()
        {
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
        }

        public void Reset()
        {
            _beforeValue = _initialValue;
            _value = _initialValue;
            _isChanged = false;
        }
    }

    [System.Serializable]
    public class Float
    {
        [SerializeField] private float _value;
        private bool _isChanged;
        private float _beforeValue;
        private float _initialValue;

        public float Value { get => _value; set => _value = value; }

        public bool IsChanged => _isChanged;
        public float BeforeValue => _beforeValue;

        public float InitialValue { get => _initialValue; set => _initialValue = value; }

        public Float(float v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }

        public void OnUpdate(float v)
        {
            _beforeValue = _value;
            _value = v;
            _isChanged = _beforeValue != _value;
        }

        public void OnUpdate()
        {
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
        }

        public void Reset()
        {
            _beforeValue = _initialValue;
            _value = _initialValue;
            _isChanged = false;
        }
    }

    [System.Serializable]
    public struct FloatStateful
    {
        [SerializeField] private float _value;
        private bool _isChanged;
        private float _beforeValue;
        [SerializeField] private float _initialValue;

        public float Value { get => _value; set => _value = value; }

        public bool IsChanged => _isChanged;
        public float BeforeValue => _beforeValue;

        public float InitialValue { get => _initialValue; set => _initialValue = value; }

        public FloatStateful(float v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }

        public void OnUpdate(float v)
        {
            _beforeValue = _value;
            _value = v;
            _isChanged = _beforeValue != _value;
        }

        public void OnUpdate()
        {
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
        }

        public void Reset()
        {
            _beforeValue = _initialValue;
            _value = _initialValue;
            _isChanged = false;
        }

        public void Reset(float v)
        {
            _beforeValue = v;
            _value = v;
            _isChanged = false;
        }
    }

    [System.Serializable]
    public class Vector2Stateful
    {
        [SerializeField] private Vector2 _value;
        private bool _isChanged;
        private Vector2 _beforeValue;
        private Vector2 _initialValue;

        public Vector2 Value { get => _value; set => _value = value; }

        public bool IsChanged => _isChanged;

        public Vector2 InitialValue { get => _initialValue; set => _initialValue = value; }

        public Vector2Stateful(Vector2 v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }

        public Vector2Stateful(float x, float y)
        {
            _value = x * Vector2.right + y * Vector2.up;
            _beforeValue = x * Vector2.right + y * Vector2.up;
            _initialValue = x * Vector2.right + y * Vector2.up;
            _isChanged = false;
        }

        public void OnUpdate(Vector2 v)
        {
            _beforeValue = _value;
            _value = v;
            _isChanged = _beforeValue != _value;
        }

        public void OnUpdate(float x, float y)
        {
            _beforeValue = _value;
            _value = x * Vector2.right + y * Vector2.up;
            _isChanged = _beforeValue != _value;
        }

        public void OnUpdate()
        {
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
        }

        public void Reset()
        {
            _beforeValue = _value;
            _value = _initialValue;
            _isChanged = _beforeValue != _value;
        }
    }

    [System.Serializable]
    public class Vector3Stateful
    {
        [SerializeField] private Vector3 _value;
        private bool _isChanged;
        private Vector3 _beforeValue;
        private Vector3 _initialValue;

        public Vector3 Value { get => _value; set => _value = value; }

        public bool IsChanged => _isChanged;

        public Vector3 InitialValue { get => _initialValue; set => _initialValue = value; }

        public Vector3Stateful(Vector3 v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }

        public void OnUpdate(Vector3 v)
        {
            _beforeValue = _value;
            _value = v;
            _isChanged = _beforeValue != _value;
        }

        public void OnUpdate()
        {
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
        }

        public void Reset()
        {
            _beforeValue = _value;
            _value = _initialValue;
            _isChanged = _beforeValue != _value;
        }
    }

    [System.Serializable]
    public class QuaternionStateful
    {
        [SerializeField] private Quaternion _value;
        [SerializeField] private bool _isChanged;
        [SerializeField] private Quaternion _beforeValue;
        [SerializeField] private Quaternion _initialValue;

        public Quaternion Value { get => _value; set => _value = value; }

        public bool IsChanged => _isChanged;

        public Quaternion InitialValue { get => _initialValue; set => _initialValue = value; }

        public QuaternionStateful(Quaternion v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }

        public void OnUpdate(Quaternion v)
        {
            _beforeValue = _value;
            _value = v;
            _isChanged = _beforeValue != _value;
        }

        public void OnUpdate()
        {
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
        }

        public void Reset()
        {
            _beforeValue = _value;
            _value = _initialValue;
            _isChanged = _beforeValue != _value;
        }
    }

    [System.Serializable]
    public class ObjectStateful
    {
        [SerializeField] private Object _value;
        private bool _isChanged;
        private Object _beforeValue;
        private Object _initialValue;

        public Object Value { get => _value; set => _value = value; }
        public System.Type Type => _value.GetType();
        public bool IsChanged => _isChanged;

        public Object InitialValue { get => _initialValue; set => _initialValue = value; }

        public ObjectStateful(Object v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }

        public void OnUpdate(Object v)
        {
            _beforeValue = _value;
            _value = v;
            _isChanged = _beforeValue != _value;
        }

        public void OnUpdate()
        {
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
        }

        public void Reset()
        {
            _beforeValue = _value;
            _value = _initialValue;
            _isChanged = _beforeValue != _value;
        }
    }

}
