namespace Towerpolis.Core.Meta
{
    /// <summary>
    /// District fill-goal predicate (meta-spec §2.4): a district is complete once its population reaches
    /// the goal. Pure and Unity-free; the "fire the complete flow exactly once" bookkeeping lives in the
    /// city/save state (a per-district completed flag), not here.
    /// </summary>
    public static class DistrictGoal
    {
        public static bool IsReached(int population, int fillGoal) => fillGoal > 0 && population >= fillGoal;
    }
}
