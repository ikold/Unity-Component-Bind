using System;
using UnityEngine;

namespace ComponentBind
{
    /// <summary>
    /// Validates existence of given component in the game object hierarchy and binds it to the field
    /// </summary>
    /// <remarks>
    /// All binding is done as part of the editor application and in stand alone build the attribute does nothing, instead relying on the value being correctly set and serialized.
    /// That requires the field to be public or use <see cref="SerializeField"/> attribute.
    ///
    /// If Source is set to Self (default), the component will be created if it does not exist. Other Source types require to have component created manually on the desired game object
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class ComponentBindAttribute : PropertyAttribute
    {
        public readonly ComponentSource Source;
        public readonly bool Strict;

        /// <param name="source">Where to search for the components to bind to the field</param>
        /// <param name="strict">When true - logs error and does not set the field if there are multiple components that could be bound</param>
        /// <remarks>
        /// When the source is set to value that might correspond to multiple game objects and binding is not strict, order of searching is: base game object first, then children, and parents last.
        /// The order of checking game objects inside children and parents groups is not defined.
        /// </remarks>
        public ComponentBindAttribute(ComponentSource source = ComponentSource.Self, bool strict = true)
        {
            Strict = strict;
            Source = source;
        }
    }
    
    public enum ComponentSource
    {
        Self,
        Child,
        SelfOrChild,
        Parent,
        SelfOrParent,
        Any,
    }
}