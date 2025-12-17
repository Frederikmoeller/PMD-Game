using UnityEngine;
using System.Collections;

public class CombatController : MonoBehaviour
{
    [Header("Combat Settings")]
    public float AttackCooldown = 0.5f;
    public float AttackAnimationDuration = 0.3f;
    public float AttackPushDistance = 0.3f;
    
    private Entity _entity;
    private bool _canAttack = true;
    private bool _isAttacking = false;
    private Coroutine _attackRoutine;
    
    private void Awake()
    {
        _entity = GetComponent<Entity>();
    }
    
    public void Attack(Entity target)
    {
        if (!_entity.CanAct() || !_canAttack || _isAttacking) return;
        
        if (_attackRoutine != null)
            StopCoroutine(_attackRoutine);
            
        _attackRoutine = StartCoroutine(PerformAttack(target));
    }
    
    private IEnumerator PerformAttack(Entity target)
    {
        _isAttacking = true;
        _canAttack = false;
        
        // Animation
        Vector3 startPos = transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 attackPos = Vector3.Lerp(startPos, targetPos, AttackPushDistance);
        
        float halfDuration = AttackAnimationDuration / 2f;
        
        // Lunge forward
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(startPos, attackPos, t / halfDuration);
            yield return null;
        }
        
        // Apply damage
        int damage = CalculateDamage(_entity.Stats, target.Stats);
        target.TakeDamage(damage, _entity);
        
        // Return
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(attackPos, startPos, t / halfDuration);
            yield return null;
        }
        
        transform.position = startPos;
        _isAttacking = false;
        
        // Cooldown
        yield return new WaitForSeconds(AttackCooldown);
        _canAttack = true;
    }
    
    private int CalculateDamage(EntityStats attacker, EntityStats defender)
    {
        int baseDamage = Mathf.Max(1, attacker.Power - defender.Resilience);
        float variance = Random.Range(0.85f, 1.15f);
        
        // Critical hit chance
        if (Random.value < attacker.Fortune * 0.01f)
        {
            variance *= 1.5f;
            Debug.Log("Critical hit!");
        }
        
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * variance));
    }
    
    public bool CanAttack => _canAttack && !_isAttacking && _entity.CanAct();
}