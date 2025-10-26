using System;
using System.Collections.Generic;
using UnityEngine;

public class ElmonAttackDataSO : ScriptableObject
{
    [Serializable]
    public class ElmonAttackData
    {
        [SerializeField] private GameObject arrowPrefab;

        [SerializeReference] private IAttackType attackType;
        [SerializeReference] private IAttackRange attackRange;

        public void Execute(Elmon elmon)
        {
         attackType.ExecuteType(elmon, arrowPrefab, attackRange);
        }
    }

    [SerializeField] private ElmonAttackData[] elmonAttackDataArray;

    public void Execute(Elmon elmon, int index)
    {
        elmonAttackDataArray[index].Execute(elmon);
    }
}