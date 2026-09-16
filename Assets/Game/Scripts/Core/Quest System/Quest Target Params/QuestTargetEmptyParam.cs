namespace Game.Core
{
    public abstract class QuestTargetEmptyParam : QuestTargetParam
    {
        public override bool Compare(QuestTargetParam other)
        {
            return other is QuestTargetEmptyParam;
        }
    }
}