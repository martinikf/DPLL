using System.Text;

HeuristicsTest();
//ParserTest();
//StructuresTests();

void StructuresTests()
{
    Clause c1 = new();
    c1.Literals.Add(-1);
    c1.Literals.Add(1);

    Clause c2 = new();
    c2.Literals.Add(1);

    Console.WriteLine(c1.ToString());

    Formula f1 = new(new List<Clause>() { c1, c2 });
    Formula f2 = f1.Clone();

    Console.WriteLine(f1.ToString());

    Console.WriteLine("Formula: " + f1 + " IsEmpty()? " + f1.IsEmpty());
    Console.WriteLine("Formula: " + f1 + " HasEmptyClause()? " + f1.HasEmptyClause());

    Console.WriteLine("Set literal 5 to true for formula: " + f1);
    f1.SetLiteral(5);
    Console.WriteLine("Formula after evaluating literal 5 to true: " + f1);
    Console.WriteLine("Formula: " + f1 + " IsEmpty()? " + f1.IsEmpty());
    Console.WriteLine("Formula: " + f1 + " HasEmptyClause()? " + f1.HasEmptyClause());

    Console.WriteLine("Formula: " + f2 + " literals: ");
    f2.Literals.ForEach(l => Console.Write(l + ", "));
    Console.WriteLine();

    SatDpllSolver satDpllSolver = new();
    Console.WriteLine("Formula: " + f2 + " IsSatisfiable backtracking: " + satDpllSolver.IsSatisfiable(f2, Heuristics.DLIS));

}

void ParserTest()
{
    Formula f = new();
    f.ParseFormulaFromFile(Path.Combine(Environment.CurrentDirectory, @"CNF\", "Example2.dimacs"), true);
    SatDpllSolver solver = new();
    Console.WriteLine("Is " + f + " satisfiable: " + solver.IsSatisfiable(f, Heuristics.MOM));

}

void HeuristicsTest()
{
    Formula f = new();
    SatDpllSolver solver = new();
    foreach (Heuristics h in Heuristics.GetValues(typeof(Heuristics)))
    {
        Console.WriteLine("----------------------------------------------------");

        f.ParseFormulaFromFile(Path.Combine(Environment.CurrentDirectory, @"CNF\", "Example2.dimacs"), false);
        Console.WriteLine("Heuristic: " + h.ToString());
        Console.WriteLine("Formula: [" + f.InputLiterals + "; " + f.InputClauses + "]");
        Console.WriteLine("Is satisfiable: " + solver.IsSatisfiable(f, h));
        Console.WriteLine("Recursive calls: " + solver.RecursiveCalls);
        solver.ResetRecursiveCalls();
    }
}

public enum Heuristics
{
    RANDOM,
    DLIS,
    DLCS,
    MOM,
    BOHM,
    My
}

internal class SatDpllSolver
{
    public int RecursiveCalls { get; set; }

    public SatDpllSolver()
    {
        RecursiveCalls = 0;
    }

    public bool IsSatisfiable(Formula f, Heuristics h)
    {
        RecursiveCalls++;
        //End conditions
        if (f.IsEmpty()) return true;
        if (f.HasEmptyClause()) return false;

        //Evaluates literals from clauses that contains only one literal
        var x = f.GetFirstSingletonClauseLiteral();
        if (x != null)
        {
            f.SetLiteral((int)x);
            return IsSatisfiable(f, h);
        }

        //Evaluates pure literals
        var y = f.GetFirstPureLiteral();
        if (y != null)
        {
            f.SetLiteral((int)y);
            return IsSatisfiable(f, h);
        }

        //Choose literal to evaluate
        var literal = h switch
        {
            Heuristics.RANDOM => ChooseLiteral(f),
            Heuristics.DLIS => Dlis(f),
            Heuristics.DLCS => Dlcs(f),
            Heuristics.MOM => MOM(f),
            Heuristics.BOHM => Bohm(f),
            Heuristics.My => My(f),
            _ => throw new Exception("Heuristic not implemented")
        };

        //Evaluate literal to true
        var f1 = f.Clone();
        f1.SetLiteral(-literal);

        if (IsSatisfiable(f1, h))
        {
            return true;
        }

        //Evaluate literal to false
        var f2 = f.Clone();
        f2.SetLiteral(literal);

        return IsSatisfiable(f2, h);
    }

    private int ChooseLiteral(Formula f)
    {
        //Random literal
        return f.Literals[Random.Shared.Next(f.Literals.Count)];
    }

    private int Dlis(Formula f)
    {
        return f.MaxFrequentLiteral();
    }

    private int Dlcs(Formula f)
    {
        return f.MaxFrequentLiteralWithNegation();
    }

    private int MOM(Formula f)
    {
        return f.MOM();
    }

    private int Bohm(Formula f)
    {
        return f.Bohm();
    }

    private int My(Formula f)
    {
        return f.MaxFrequentLiteralInShortestClausules();
    }

    public void ResetRecursiveCalls()
    {
        RecursiveCalls = 0;
    }
}

internal class Formula
{
    public List<Clause> Clauses { get; set; }

    public Dictionary<int, int> Frequency { get; set; }

    public List<int> Literals
    {
        get;
        set;
    }

    public int InputLiterals = 0;
    public int InputClauses = 0;

    private int FrequencyK(int literal, int k)
    {
        int count = 0;

        foreach (var c in Clauses.Where(x => x.Literals.Count == k))
        {
            if (c.Literals.Contains(literal))
            {
                count++;
            }
        }

        return count;
    }

    public Formula(IEnumerable<Clause> c)
    {
        Clauses = new List<Clause>();
        Literals = new List<int>();
        Frequency = new Dictionary<int, int>();

        Clauses.AddRange(c);
        Recalculate();
    }

    public Formula()
    {
        Clauses = new List<Clause>();
        Literals = new List<int>();
        Frequency = new Dictionary<int, int>();
        Recalculate();
    }

    public bool IsEmpty()
    {
        return Clauses.Count == 0;
    }

    public bool HasEmptyClause()
    {
        if (IsEmpty()) return false;

        return Clauses.Any(c => c.Literals.Count == 0);
    }

    public void SetLiteral(int literal)
    {
        if (!Literals.Contains(literal))
            return;

        foreach (var c in Clauses.ToList())
        {
            if (c.Literals.Contains(literal))
            {
                Clauses.Remove(c);
            }
            else if (c.Literals.Contains(-literal))
            {
                c.Literals.Remove(-literal);
            }
        }

        Recalculate();
    }

    public Formula Clone()
    {
        return new Formula(Clauses.Select(c => c.Clone()).ToList());
    }

    public void Recalculate()
    {
        var literals = new List<int>();
        foreach (var clause in Clauses)
        {
            literals.AddRange(clause.Literals);
        }

        Literals.Clear();
        Literals.AddRange(literals.Distinct().ToList());

        foreach (var l in Literals)
        {
            Frequency[l] = Clauses.Count(x => x.Literals.Contains(l));
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        var empty = true;

        sb.Append('(');

        foreach (var c in Clauses)
        {
            if (empty)
                empty = false;
            sb.Append(c);
            sb.Append(" ^ ");
        }
        if (!empty)
            sb.Remove(sb.Length - 3, 3);

        sb.Append(')');

        return sb.ToString();
    }

    public void ParseFormulaFromFile(string path, bool print)
    {
        try
        {
            using StreamReader s = new(path);

            string? line;

            while ((line = s.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length <= 0)
                    break;

                switch (line[0])
                {
                    case 'p':
                        var split = line.Split(" ");
                        InputLiterals = int.Parse(split[2]);
                        InputClauses = int.Parse(split[3]);
                        if (print)
                        {
                            Console.WriteLine("Literals count: " + split[2]);
                            Console.WriteLine("Formulas count: " + split[3]);
                        }

                        break;
                    case 'c':
                        if (print)
                            Console.WriteLine("Comment: " + line);
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        Clause c = new();
                        foreach (var num in line.Split(" "))
                        {
                            if (num != "0")
                                c.Literals.Add(int.Parse(num));
                        }

                        Clauses.Add(c);
                        break;
                    default:
                        Console.WriteLine(line);
                        break;
                }
            }

            Recalculate();
        }
        catch (Exception ex)
        {
            Literals.Clear();
            Clauses.Clear();
            Console.WriteLine("Error while parsing file");
            Console.WriteLine(ex.ToString());
        }
    }

    public int? GetFirstPureLiteral()
    {
        foreach (var l in Literals)
        {
            if (!Literals.Contains(-l))
                return l;
        }

        return null;
    }

    public int? GetFirstSingletonClauseLiteral()
    {
        //O(n)
        foreach (var c in Clauses)
        {
            if (c.Literals.Count == 1)
                return c.Literals.Min();
        }

        return null;
    }

    //DLIS
    public int MaxFrequentLiteral()
    {
        int maxKey = Literals[0];
        foreach (var l in Literals)
        {
            if (Frequency[l] > Frequency[maxKey])
                maxKey = l;
        }

        return maxKey;
    }

    //MY find longest clause, foreach longest clause find most frequent literal
    public int MaxFrequentLiteralInShortestClausules()
    {
        //Find shortest clausule
        Clause k = Clauses.First();

        foreach (var c in Clauses)
        {
            if (c.Literals.Count < k.Literals.Count)
                k = c;
        }
        Dictionary<int, int> freq = new();
        foreach (var l in Literals)
        {
            freq[l] = 0;
            foreach (var c in Clauses.Where(x => x.Literals.Count == k.Literals.Count))
            {
                if (c.Literals.Contains(l))
                {
                    freq[l]++;
                }
            }
        }

        return freq.OrderByDescending(x => x.Value).First().Key;
    }

    //DLCS
    public int MaxFrequentLiteralWithNegation()
    {
        var maxKey = Literals[0];

        foreach (var l in Literals)
        {
            if (Frequency[l] + Frequency[-l] > Frequency[maxKey] + Frequency[-maxKey])
                maxKey = l;
        }

        if (Frequency[-maxKey] > Frequency[maxKey])
            return -maxKey;
        return maxKey;
    }

    //MOM
    public int MOM()
    {
        int p = Literals.Count * Literals.Count + 1;
        Clause k = Clauses.First();

        foreach (var c in Clauses)
        {
            if (c.Literals.Count < k.Literals.Count)
                k = c;
        }

        int literal = k.Literals.First();
        foreach (var l in Literals)
        {
            if ((FrequencyK(l, k.Literals.Count) + FrequencyK(-l, k.Literals.Count)) * p + Frequency[l] * Frequency[-l]
                >
                (FrequencyK(literal, k.Literals.Count) + FrequencyK(-literal, k.Literals.Count)) * p + Frequency[literal] * Frequency[-literal])
                literal = l;
        }

        return literal;
    }

    //Bohm
    public int Bohm()
    {
        var p1 = 1;
        var p2 = 2;

        //Find longest clause
        int n = Clauses.First().Literals.Count;
        foreach (var c in Clauses)
        {
            if (c.Literals.Count > n)
                n = c.Literals.Count;
        }

        Dictionary<int, int> H = new();

        foreach (var l in Literals)
        {
            var sum = 0;

            for (int i = 2; i < n; i++)
            {
                sum += p1 * Math.Max(FrequencyK(l, i), FrequencyK(-l, i)) +
                       p2 * Math.Min(FrequencyK(l, i), FrequencyK(-l, i));
            }
            H[l] = sum;
        }

        return H.OrderByDescending(x => x.Value).First().Key;
    }
}

internal class Clause
{
    public HashSet<int> Literals { get; set; }

    public Clause()
    {
        Literals = new HashSet<int>();
    }

    public Clause Clone()
    {
        return new Clause { Literals = new HashSet<int>(Literals) };
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append('(');
        var empty = true;

        foreach (var l in Literals)
        {
            if (empty)
                empty = false;
            sb.Append(l);
            sb.Append(" V ");
        }

        if (!empty)
            sb.Remove(sb.Length - 3, 3);

        sb.Append(')');

        return sb.ToString();
    }
}