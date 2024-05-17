using UnityEngine;

namespace StatefulVariables // Assuming your ReferenceManager class is in this namespace
{
    [System.Serializable]
    public class Float
    {
        [SerializeField] private float _value;
        [SerializeField] private bool _isChanged;
        [SerializeField] private float _beforeValue;
        [SerializeField] private float _initialValue;

        public float Value { get => _value; set => _value = value; }

        public bool IsChanged => _isChanged;

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
            _beforeValue = _value;
            _value = _initialValue;
            _isChanged = _beforeValue != _value;
        }
    }

    [System.Serializable]
    public class Vector3Stateful
    {
        [SerializeField] private Vector3 _value;
        [SerializeField] private bool _isChanged;
        [SerializeField] private Vector3 _beforeValue;
        [SerializeField] private Vector3 _initialValue;

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
    public class ObjectStateful
    {
        [SerializeField] private Object _value;
        [SerializeField] private bool _isChanged;
        [SerializeField] private Object _beforeValue;
        [SerializeField] private Object _initialValue;

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

    [System.Serializable]
    public class Stateful<T> {
        [SerializeField] private T _value;
        [SerializeField] private bool _isChanged;
        [SerializeField] private T _beforeValue;
        [SerializeField] private T _initialValue;

        public T Value { get => _value; set => _value = value; }
        public System.Type Type => _value.GetType();
        public bool IsChanged => _isChanged;

        public T InitialValue { get => _initialValue; set => _initialValue = value; }

        public Stateful(T v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }

        public void OnUpdate(T v)
        {
            _value = v;
            _isChanged = _beforeValue.Equals(_value);
            _beforeValue = _value;
        }

        public void OnUpdate()
        {
            _isChanged = _beforeValue.Equals(_value);
            _beforeValue = _value;
        }

        public void Reset()
        {
            _isChanged = false;
            _beforeValue = _initialValue;
            _value = _initialValue;
        }

        public void Reset(T v)
        {
            _value = v;
            _beforeValue = v;
            _initialValue = v;
            _isChanged = false;
        }
    }
}
