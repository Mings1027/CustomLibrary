using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IAttackRange
{
    string Name { get; }
    void ExecuteRange(Elmon elmon, ElmonArrow arrow);
}

public interface IAttackType
{
    string Name { get; }
    void ExecuteType(Elmon elmon, GameObject arrowPrefab, IAttackRange attackRange);
}


[Serializable]
public class MeleeAttack : IAttackType
{
    public string Name { get; } = "근접";

    public void ExecuteType(Elmon elmon, GameObject arrowPrefab, IAttackRange attackRange)
    {
        var arrow = ObjectPoolManager.Instance.Get<ElmonArrow>(arrowPrefab, elmon.AttackPos.position,
            Quaternion.identity);

        attackRange.ExecuteRange(elmon, arrow);
    }
}

[Serializable]
class ProjectileAttack : IAttackType
{
    public string Name { get; } = "투사체";

    [SerializeField] private float speed;

    public void ExecuteType(Elmon elmon, GameObject arrowPrefab, IAttackRange attackRange)
    {
        var arrow = ObjectPoolManager.Instance.Get<ElmonArrow>(arrowPrefab, elmon.AttackPos.position,
            Quaternion.identity);
        var dir = (elmon.Target.position - elmon.AttackPos.position).normalized;
        arrow.EndPos = elmon.Target.position;
        arrow.OnMove += () => { arrow.transform.position += dir * speed * Time.deltaTime; };

        attackRange.ExecuteRange(elmon, arrow);
    }
}

[Serializable]
public class ArcProjectileAttack : IAttackType
{
    public string Name { get; } = "투사체(곡사)";

    [SerializeField] private float speed = 5f;
    [SerializeField] private float height = 3f;

    public void ExecuteType(Elmon elmon, GameObject arrowPrefab, IAttackRange attackRange)
    {
        var arrow = ObjectPoolManager.Instance.Get<ElmonArrow>(arrowPrefab, elmon.AttackPos.position,
            Quaternion.identity);

        Vector3 start = elmon.AttackPos.position;
        var end = elmon.Target.position;
        arrow.EndPos = elmon.Target.position;
        float distance = Vector3.Distance(start, end);
        float duration = distance / speed;
        float t = 0f;

        arrow.OnMove += () =>
        {
            t += Time.deltaTime / duration;
            if (t >= 1f)
            {
                return;
            }

            Vector3 pos = Vector3.Lerp(start, end, t);
            float arc = 4 * height * t * (1 - t);
            pos.y += arc;
            arrowPrefab.transform.position = pos;
        };

        attackRange.ExecuteRange(elmon, arrow);
    }
}

[Serializable]
public class HitscanAttack : IAttackType
{
    public string Name { get; } = "히트스캔";

    public void ExecuteType(Elmon elmon, GameObject arrowPrefab, IAttackRange attackRange)
    {
        var arrow = ObjectPoolManager.Instance.Get<ElmonArrow>(arrowPrefab, elmon.Target.position,
            Quaternion.identity);

        attackRange.ExecuteRange(elmon, arrow);
    }
}


[Serializable]
public class SingleHitAttack : IAttackRange
{
    public string Name { get; } = "단일";

    public void ExecuteRange(Elmon elmon, ElmonArrow arrow)
    {
        arrow.OnHit += () =>
        {
            elmon.Target.GetComponent<Enemy>().TakeDamage();
            arrow.DisableCollider();
        };
    }
}

[Serializable]
public class PierceAttack : IAttackRange
{
    public string Name { get; } = "관통";
    [SerializeField] private int pierceCount;

    public void ExecuteRange(Elmon elmon, ElmonArrow arrow)
    {
        var remainPierce = pierceCount;
        arrow.OnHit += () =>
        {
            elmon.Target.GetComponent<Enemy>().TakeDamage();
            remainPierce--;
            if (remainPierce <= 0)
            {
                arrow.DisableCollider();
            }
        };
    }
}

[Serializable]
public class AreaAttack : IAttackRange
{
    public string Name { get; } = "범위";

    public void ExecuteRange(Elmon elmon, ElmonArrow arrow)
    {
        arrow.OnHit += () =>
        {
            // spawn ElmonAttackRange
            arrow.DisableCollider();
        };
    }
}

[Serializable]
public class MultiShot : IAttackRange
{
    public string Name { get; } = "멀티샷";
    [SerializeField] private int shotCount = 3;
    [SerializeField] private float speed;

    public void ExecuteRange(Elmon elmon, ElmonArrow arrow)
    {
        var monsterCloseList = ObjectPoolManager.Instance.monsterCloseList;

        var dir = (monsterCloseList[0].position - elmon.AttackPos.position).normalized;
        arrow.OnMove = () => { arrow.transform.position += dir * speed * Time.deltaTime; };

        for (int i = 1; i < shotCount; i++)
        {
            var extraArrow =
                ObjectPoolManager.Instance.Get<ElmonArrow>(arrow.gameObject, elmon.AttackPos.position,
                    Quaternion.identity);
            var extraDir = (monsterCloseList[i].position - elmon.AttackPos.position).normalized;
            extraArrow.OnMove += () => { extraArrow.transform.position += extraDir * speed * Time.deltaTime; };
            extraArrow.OnHit += () =>
            {
                elmon.Target.GetComponent<Enemy>().TakeDamage();
                arrow.DisableCollider();
            };
        }
    }
}

[Serializable]
public class RapidAttack : IAttackRange
{
    public string Name { get; } = "연발";

    [SerializeField] private int shotCount = 3;
    [SerializeField] private float delay = 0.1f;
    [SerializeField] private float speed;

    public void ExecuteRange(Elmon elmon, ElmonArrow arrow)
    {
        Execute(elmon, arrow).Forget();
    }

    private async UniTaskVoid Execute(Elmon elmon, ElmonArrow arrow)
    {
        for (int i = 0; i < shotCount; i++)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            
            var dir = (elmon.Target.position - elmon.AttackPos.position).normalized;
            arrow.OnMove = () => { arrow.transform.position += dir * speed * Time.deltaTime; };

            var extraArrow =
                ObjectPoolManager.Instance.Get<ElmonArrow>(arrow.gameObject, elmon.AttackPos.position,
                    Quaternion.identity);
       
            var extraDir = (elmon.Target.position - elmon.AttackPos.position).normalized;
            extraArrow.OnMove += () => { extraArrow.transform.position += extraDir * speed * Time.deltaTime; };

            extraArrow.OnHit += () =>
            {
                elmon.Target.GetComponent<Enemy>().TakeDamage();
                arrow.DisableCollider();
            };
        }
    }
}

[Serializable]
public class ChainAttack : IAttackRange
{
    public string Name { get; } = "체인";
    public void ExecuteRange(Elmon elmon, ElmonArrow arrow) { }
}