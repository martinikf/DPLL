Console.WriteLine("Hello, World!");

internal class Formula
{
    private List<Clause> clauses = new List<Clause>();


    public void EvaluateLiteral(int i)
    {
        foreach (var clause in clauses)
        {
            if (clause.literals.Contains(i))
            {
                clauses.Remove(clause);
            }
            else
            {
                clause.literals.Remove(i);
            }
        }
    }
}

internal class Clause
{
    public HashSet<int> literals = new HashSet<int>();
}