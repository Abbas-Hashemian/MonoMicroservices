using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace MonoMicroservices.Library.DataAccess.Attributes;
/// <summary>
/// Sets the Table name of a model same as another model (e.g. UserIdentity same as main User model)
/// </summary>
public class TableDerivationAttribute : TableAttribute
{
  public TableDerivationAttribute(string name) : base(name) { }
  public TableDerivationAttribute(Type modelType)
    : base(ExtractTableName(modelType))
  {
  }
  private static string ExtractTableName(Type modelType)
  {
    var attr = modelType.GetCustomAttribute(typeof(TableAttribute));
    if (attr == null)
      throw new Exception("No TableAttribute on the target model.");
    var tableName = ((TableAttribute)attr).Name;
    //GetCustomAttribute throws exception for [Table("")] : Message	"The argument 'name' cannot be null, empty or contain only whitespace. (Parameter 'name')"	string
    //so there this verification will never be reached :
    //if (string.IsNullOrEmpty(tableName))
    //	throw new Exception("No Table Name on the target model.");
    return tableName;
  }
}
