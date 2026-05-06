using System.Collections.Generic;
using UnityEngine;

public static class InteractionFocusService
{
    private class Candidate
    {
        public Object Owner;
        public Transform FocusPoint;
        public int Priority;
        public bool InRange;
    }

    private static readonly Dictionary<int, Candidate> _candidates = new Dictionary<int, Candidate>();

    public static void SetCandidate(Object owner, Transform focusPoint, bool inRange, int priority)
    {
        if (owner == null) return;

        int id = owner.GetInstanceID();

        if (!_candidates.TryGetValue(id, out Candidate candidate))
        {
            candidate = new Candidate();
            _candidates[id] = candidate;
        }

        candidate.Owner = owner;
        candidate.FocusPoint = focusPoint;
        candidate.InRange = inRange;
        candidate.Priority = priority;
    }

    public static void RemoveCandidate(Object owner)
    {
        if (owner == null) return;
        _candidates.Remove(owner.GetInstanceID());
    }

    public static bool HasFocus(Object owner, Vector3 playerPosition)
    {
        if (owner == null) return false;

        int ownerId = owner.GetInstanceID();
        int bestId = 0;
        int bestPriority = int.MinValue;
        float bestSqrDistance = float.MaxValue;

        foreach (KeyValuePair<int, Candidate> pair in _candidates)
        {
            Candidate candidate = pair.Value;
            if (candidate == null) continue;
            if (candidate.Owner == null) continue;
            if (!candidate.InRange) continue;
            if (candidate.FocusPoint == null) continue;

            float sqrDistance = (candidate.FocusPoint.position - playerPosition).sqrMagnitude;

            bool isBetter =
                candidate.Priority > bestPriority ||
                (candidate.Priority == bestPriority && sqrDistance < bestSqrDistance);

            if (!isBetter) continue;

            bestPriority = candidate.Priority;
            bestSqrDistance = sqrDistance;
            bestId = pair.Key;
        }

        return bestId == ownerId;
    }
}
