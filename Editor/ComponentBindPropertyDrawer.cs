using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ComponentBind.Editor
{
    [CustomPropertyDrawer(typeof(ComponentBindAttribute))]
    public class ComponentBindPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new PropertyField(property);
            field.SetEnabled(false);
            return field;
        }
    }
}