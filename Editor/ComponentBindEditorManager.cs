using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ComponentBind.Editor
{
    /// <summary>
    /// Handles field binding during editor runtime
    /// </summary>
    [InitializeOnLoad]
    public static class ComponentBindEditorManager
    {
        private static FieldInfo[] _fieldsWithBindAttribute;

        static ComponentBindEditorManager()
        {
            GetFieldsWithAttribute();

            ObjectChangeEvents.changesPublished += (ref ObjectChangeEventStream stream) =>
            {
                if (stream.GetEventType(0) != ObjectChangeKind.ChangeScene)
                    OnChange();
            };

            AssemblyReloadEvents.afterAssemblyReload += OnChange;
        }

        private static void OnChange()
        {
            foreach (var field in _fieldsWithBindAttribute)
            {
                // Get all objects that have the bound filed including derived objects
                var objects = UnityEngine.Object.FindObjectsOfType(field.DeclaringType);

                foreach (var obj in objects)
                {
                    var gameObject = ((Component)obj).gameObject;

                    var attribute = field.GetCustomAttribute<ComponentBindAttribute>();

                    var components = new List<Component>();

                    // Get components of the bind field type from the same game object
                    if (attribute.Source is ComponentSource.Self or ComponentSource.SelfOrChild or ComponentSource.SelfOrParent or ComponentSource.Any)
                        components.AddRange(gameObject.GetComponents(field.FieldType));

                    // Get components of the bind field type from children game objects
                    if (attribute.Source is ComponentSource.Child or ComponentSource.SelfOrChild or ComponentSource.Any)
                        components.AddRange(gameObject.GetComponentsInChildren(field.FieldType).Where(com => com.gameObject != gameObject));

                    // Get components of the bind field type from parent game objects
                    if (attribute.Source is ComponentSource.Parent or ComponentSource.SelfOrParent or ComponentSource.Any)
                        components.AddRange(gameObject.GetComponentsInParent(field.FieldType).Where(com => com.gameObject != gameObject));
                    
                    var context = $"Type {field.FieldType.Name} to assign to {field.Name} Field (Source: {attribute.Source}{(attribute.Strict ? " - Strict" : "")}) in {gameObject.name} Game Object";

                    var couldNotFind = $"Could not find any component of {context}!";
                    var multipleSources = $"There is more than one component of {context}!";

                    const string creatingComponent = "Source is set to Self, creating a new component.";
                    const string selectingFirstSource = "Selecting the first encountered component.";

                    switch (components.Count, attribute.Source, attribute.Strict)
                    {
                        case (0, ComponentSource.Self, _):
                            Debug.Log($"{couldNotFind} {creatingComponent}", gameObject);
                            // Add a new component to the same game object
                            var newComponent = gameObject.AddComponent(field.FieldType);
                            components.Add(newComponent);
                            break;
                        case (> 1, _, false):
                            Debug.LogWarning($"{multipleSources} {selectingFirstSource}", gameObject);
                            break;
                        // Error Cases - Component field will not be set (might still have old value)
                        case (0, _, _):
                            Debug.LogError(couldNotFind, gameObject);
                            continue;
                        case (> 1, _, true):
                            Debug.LogError(multipleSources, gameObject);
                            continue;
                    }

                    field.SetValue(obj, components.First());
                    EditorUtility.SetDirty(gameObject);
                }
            }
        }

        private static void GetFieldsWithAttribute()
        {
            // Get assembly that has bind attribute class
            var thisAssembly = typeof(ComponentBindAttribute).Assembly;

            // Get assemblies that directly reference bind attribute assembly
            var assemblies = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                where assembly.GetReferencedAssemblies().Any(name => name.ToString().Equals(thisAssembly.GetName().ToString()))
                select assembly;

            // Add current assembly to collection
            assemblies = assemblies.Append(thisAssembly);

            // Get all types in the found assemblies
            var types = from assembly in assemblies
                from type in assembly.GetTypes()
                select type;

            // Get all instance fields
            var fields = from type in types
                from method in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                select method;

            // Filter out fields without the bind attribute
            // For public fields we only need one definition from the declaring class
            _fieldsWithBindAttribute = fields.Where(field => field.IsDefined(typeof(ComponentBindAttribute), false) && field.DeclaringType == field.ReflectedType).ToArray();

            // Sort fields based on their attributes settings to ensure that the ones that might create components are evaluated first
            Array.Sort(_fieldsWithBindAttribute, (left, right) =>
            {
                var leftAttribute = left.GetCustomAttribute<ComponentBindAttribute>();
                var rightAttribute = right.GetCustomAttribute<ComponentBindAttribute>();

                return (leftAttribute.Strict, rightAttribute.Strict, leftAttribute.Source, rightAttribute.Source) switch
                {
                    (_, _, ComponentSource.Self, not ComponentSource.Self) => -1,
                    (_, _, not ComponentSource.Self, ComponentSource.Self) => 1,
                    (true, false, _, _) => -1,
                    (false, true, _, _) => 1,
                    _ => 0
                };
            });
        }
    }
}