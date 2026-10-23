/// <summary>
/// Anything that can take damage and die.
/// </summary>
public interface IDamageable
{
    public void TakeDamage(float damage);
    public void Die();
}
