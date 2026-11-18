using Verse;
using Verse.AI;

namespace DraftAnything
{
    public class ThinkNode_IsControllable : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn)
        {
            return pawn.IsControllable();
        }
    }
}