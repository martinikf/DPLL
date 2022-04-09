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
    Console.WriteLine("Formula: " + f1 + " HasEmptyClause()? " + f1.HasEmptyClause);

    Console.WriteLine("Set literal 5 to true for formula: " + f1);
    f1.SetLiteral(5);
    Console.WriteLine("Formula after evaluating literal 5 to true: " + f1);
    Console.WriteLine("Formula: " + f1 + " IsEmpty()? " + f1.IsEmpty());
    Console.WriteLine("Formula: " + f1 + " HasEmptyClause()? " + f1.HasEmptyClause);

    Console.WriteLine("Formula: " + f2 + " literals: ");
    foreach (var f2Literal in f2.Literals)
    {
        Console.Write(f2Literal + ", ");
    }
    Console.WriteLine();

    SatDpllSolver satDpllSolver = new();
    Console.WriteLine("Formula: " + f2 + " IsSatisfiable backtracking: " + satDpllSolver.IsSatisfiable(f2, Heuristics.DLIS));

}


void ParserTest()
{
    Formula f = new();
    f.ParseFormulaFromFile(Path.Combine(Environment.CurrentDirectory, @"CNF\", "Example3.dimacs"), true);
    SatDpllSolver solver = new();
    Console.WriteLine("Is " + f + " satisfiable: " + solver.IsSatisfiable(f, Heuristics.BOHM));
    Console.WriteLine(solver.RecursiveCalls);
}


void HeuristicsTest()
{
    foreach (Heuristics h in Enum.GetValues(typeof(Heuristics)))
    {
        Formula f = new();
        SatDpllSolver solver = new();

        if (h == Heuristics.RANDOM) continue;

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
        if (f.HasEmptyClause) return false;

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
            Heuristics.RANDOM => f.RandomLiteral(),
            Heuristics.DLIS => f.Dlis(),
            Heuristics.DLCS => f.Dlcs(),
            Heuristics.MOM => f.MOM(),
            Heuristics.BOHM => f.Bohm(),
            Heuristics.My => f.My(),
            _ => throw new Exception("Heuristic not implemented")
        };

        //Evaluate literal
        var f1 = f.Clone();
        f1.SetLiteral(-literal);

        if (IsSatisfiable(f1, h))
        {
            return true;
        }

        //Evaluate literal to false
        f.SetLiteral(literal);

        return IsSatisfiable(f, h);
    }

    public void ResetRecursiveCalls()
    {
        RecursiveCalls = 0;
    }
}

internal class Formula
{
    public HashSet<Clause> Clauses { get; set; }

    public Dictionary<int, int> Frequency { get; set; }

    public HashSet<int> Literals { get; set; }

    public int InputLiterals = 0;
    public int InputClauses = 0;
    public bool HasEmptyClause = false;

    public Formula()
    {
        Clauses = new HashSet<Clause>();
        Literals = new HashSet<int>();
        Frequency = new Dictionary<int, int>();
        Recalculate();
    }

    public Formula(IEnumerable<Clause> clauses)
    {
        Clauses = new HashSet<Clause>();
        Literals = new HashSet<int>();
        Frequency = new Dictionary<int, int>();

        foreach (var c in clauses)
            Clauses.Add(c);
        Recalculate();
    }

    public bool IsEmpty()
    {
        return Clauses.Count == 0;
    }

    public void SetLiteral(int literal)
    {
        if (!Literals.Contains(literal))
            return;

        foreach (var c in Clauses.Where(x => x.Literals.Contains(literal) || x.Literals.Contains(-literal)).ToList())
        {
            if (c.Literals.Contains(literal))
            {
                Clauses.Remove(c);
            }
            else
            {
                c.Literals.Remove(-literal);
                if (c.Literals.Count == 0)
                    HasEmptyClause = true;
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
        Literals.Clear();
        foreach (var c in Clauses)
        {
            foreach (var l in c.Literals)
            {
                Literals.Add(l);
            }
        }

        foreach (var l in Literals)
        {
            Frequency[l] = 0;
            foreach (var c in Clauses)
            {
                if (c.Literals.Contains(l))
                {
                    Frequency[l]++;
                }
            }
        }
    }

    private int FrequencyK(int literal, int k)
    {
        return Clauses.Where(x => x.Literals.Count == k).Count(c => c.Literals.Contains(literal));
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
        int a = Literals.FirstOrDefault(x => !Literals.Contains(-x));
        if (a == 0)
            return null;
        return a;

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
    public int Dlis()
    {
        var maxKey = Literals.First();

        foreach (var l in Literals)
        {
            if (Frequency[l] > Frequency[maxKey])
                maxKey = l;
        }

        return -maxKey; // Opak protože v dppl je to naopak

    }

    //DLCS
    public int Dlcs()
    {
        var maxKey = Literals.First();

        foreach (var l in Literals)
        {
            if (Frequency[l] + Frequency[-l] > Frequency[maxKey] + Frequency[-maxKey])
                maxKey = l;
        }

        if (Frequency[maxKey] >= Frequency[-maxKey])
            return -maxKey; // Opak protože v dppl je to naopak
        return maxKey;
    }

    //MOM
    public int MOM()
    {
        var p = Literals.Count * Literals.Count + 1;
        var k = Clauses.First();

        foreach (var c in Clauses)
        {
            if (c.Literals.Count < k.Literals.Count)
                k = c;
        }

        var literal = k.Literals.First();
        foreach (var l in Literals)
        {
            if ((FrequencyK(l, k.Literals.Count) + FrequencyK(-l, k.Literals.Count)) * p + (FrequencyK(l, k.Literals.Count) * (FrequencyK(-l, k.Literals.Count)))
                >
                (FrequencyK(literal, k.Literals.Count) + FrequencyK(-literal, k.Literals.Count)) * p + (FrequencyK(literal, k.Literals.Count) * (FrequencyK(-literal, k.Literals.Count))))
                literal = l;
        }

        return literal;
    }

    //Bohm
    public int Bohm()
    {
        const int p1 = 1;
        const int p2 = 2;

        //Find longest clause
        var n = Clauses.First().Literals.Count;
        foreach (var c in Clauses)
        {
            if (c.Literals.Count > n)
                n = c.Literals.Count;
        }

        Dictionary<int, int> H = new();

        foreach (var l in Literals)
        {
            var sum = 0;

            for (var i = 2; i <= n; i++)
            {
                //Prochazim klasule nejdelší délky?
                sum += p1 * Math.Max(FrequencyK(l, i), FrequencyK(-l, i)) +
                       p2 * Math.Min(FrequencyK(l, i), FrequencyK(-l, i));
            }
            H[l] = sum;
        }

        return H.OrderByDescending(x => x.Value).First().Key;
    }

    //MY find longest clause, foreach longest clause find most frequent literal
    public int My()
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

    public int RandomLiteral()
    {
        return Literals.ElementAt(Random.Shared.Next(Literals.Count));
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