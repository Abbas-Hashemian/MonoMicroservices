namespace MonoMicroservices.Library.DataAccess.Bases;
public class RepositoryBase<TDbContext>
{
  protected TDbContext Context;
  public RepositoryBase(TDbContext context)
  {
    Context = context;
  }
}
