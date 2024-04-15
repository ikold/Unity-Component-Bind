# Unity-Extended-Dicionary
Unity Package that provides automatic binding of component fields in MonoBehaviours.

### Setup
In Unity Package Manager select `Add Package from git URL...` and add the following URL
```sh
https://github.com/ikold/Unity-Component-Bind.git
```

Adding `[ComponentBind]` attribute to a field will bind it to the component that matches the given type.

```C#
using ComponentBind;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
	// Will bind to NavMeshAgent component on the same Game Object
	[ComponentBind]
	private NavMeshAgent navMeshAgent;
	void Update()
	{
		// NavMeshAgent is bound and can be used without additional code setup
	}
}
```

Binding is done on editor updates and the component needs to be serialized to work in the standalone build.
It works the same way you would expose a field in the editor and manually set the reference in the inspector.

Binding can be also done to the components from a parent or child (e.g. `[ComponentBind(ComponentSource.Parent)]`).
By default, the binding will fail and log an error if there are multiple gameObjects with the desired component. It can be reduced to a warning and binding to the first found component by setting `strict` parameter to false (e.g. `[ComponentBind(ComponentSource.Child, strict: false)]`).
