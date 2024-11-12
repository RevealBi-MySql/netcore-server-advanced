using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace RevealSdk.Server.Reveal
{
    class ReadOnlySelectVisitor : TSqlFragmentVisitor
    {
        public bool IsReadOnly { get; private set; } = true;

        public override void Visit(TSqlStatement node)
        {
            if (!(node is SelectStatement))
            {
                IsReadOnly = false;
            }
            base.Visit(node);
        }
    }
}
