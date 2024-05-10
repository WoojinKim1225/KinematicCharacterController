using System.Collections;
using System.Collections.Generic;
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
            _value = v;
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
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
            _value = v;
            _isChanged = _beforeValue != _value;
            _beforeValue = _value;
        }
    }
}
