using UnityEngine;
using System;

namespace PlasmaModding
{
    public abstract class CustomType<T> : ICustomType
    {
        public string typeName { get; protected set; }
        public string description { get; protected set; }
        public T defaultValue { get; protected set; }
        public Sprite icon { get; protected set; }
        public Sprite sketchIcon { get; protected set; }
        public Type editorType { get; protected set; }
        public GameObject editorObject { get; protected set; }


        public abstract byte[] ToBytes(T value);
        public abstract T FromBytes(byte[] bytes);
        public abstract string ToNiceString(T value);
        public abstract string ToString(T value);

        string ICustomType.typeName => typeName;
        string ICustomType.description => description;
        object ICustomType.defaultValue => defaultValue;
        Sprite ICustomType.icon => sketchIcon;
        Sprite ICustomType.sketchIcon => sketchIcon;
        Type ICustomType.editorType => editorType;
        GameObject ICustomType.editorObject => editorObject;


        byte[] ICustomType.ToBytes(object value) => ToBytes((T)value);
        object ICustomType.FromBytes(byte[] bytes) => FromBytes(bytes);
        string ICustomType.ToNiceString(object value) => ToNiceString((T)value);
        string ICustomType.ToString(object value) => ToString((T)value);
    }

    public interface ICustomType
    {
        string typeName { get; }
        string description { get; }
        object defaultValue { get; }
        Sprite icon { get; }
        Sprite sketchIcon { get; }
        Type editorType { get; }
        GameObject editorObject { get; }

        byte[] ToBytes(object value);
        object FromBytes(byte[] bytes);
        string ToNiceString(object value);
        string ToString(object value);
    }
}
