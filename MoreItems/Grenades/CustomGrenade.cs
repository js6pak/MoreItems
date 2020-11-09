namespace MoreItems.Grenades
{
    public abstract class CustomGrenade : CustomItem
    {
        public virtual void Explode(GrenadeExplodeEvent ev)
        {
        }
    }
}