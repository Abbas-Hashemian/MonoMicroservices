using System.Dynamic;

namespace MonoMicroservices.Library.Helpers;
public class IndexerDynamicModel : DynamicObject
{
	private Dictionary<string, object?> _properties = new Dictionary<string, object?>();
	public override bool TryGetMember(GetMemberBinder binder, out object? result) =>
		_properties.TryGetValue(binder.Name, out result);
	public override bool TrySetMember(SetMemberBinder binder, object? value)
	{
		_properties[binder.Name] = value;
		return true;
	}
	public object? this[string name] { get => _properties[name]; set => _properties[name] = value; }
}
