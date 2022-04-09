using System.Text;

//HeuristicsTest();
ParserTest();
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
    foreach (var f2Literal in f2.AllLiterals)
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
    Console.WriteLine("Is " + f + " satisfiable: " + solver.IsSatisfiable(f, Heuristics.DLCS));
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
    public Dictionary<int, int> AllLiterals { get; set; }

    public Dictionary<int, HashSet<Clause>> CountClauses { get; set; }

    public int InputLiterals = 0;
    public int InputClauses = 0;
    public bool HasEmptyClause = false;
    public int ClauseCount = 0;

    public Formula()
    {
        AllLiterals = new Dictionary<int, int>();
        CountClauses = new Dictionary<int, HashSet<Clause>>();
    }

    public Formula(IEnumerable<Clause> clauses)
    {
        AllLiterals = new Dictionary<int, int>();
        CountClauses = new Dictionary<int, HashSet<Clause>>();

        foreach (var c in clauses)
        {
            if (CountClauses.ContainsKey(c.Literals.Count))
                CountClauses[c.Literals.Count].Add(c);
            else
            {
                CountClauses[c.Literals.Count] = new() { c };
            }
            ClauseCount++;
        }

        foreach (var c in AllClauses())
        {
            foreach (var l in c.Literals)
            {
                if (AllLiterals.ContainsKey(l))
                    AllLiterals[l]++;
                else
                    AllLiterals[l] = 1;
            }
        }
    }

    public HashSet<int> UniqueLiterals()
    {
        return AllLiterals.Keys.ToHashSet();
    }

    public HashSet<Clause> AllClauses()
    {
        return CountClauses.Values.SelectMany(x => x).ToHashSet();
    }

    public bool IsEmpty()
    {
        return ClauseCount == 0;
    }

    public void SetLiteral(int literal)
    {
        if (AllLiterals[literal] <= 0)
        {
            Console.WriteLine("Warning! Setting literal that doesn't exists");
            return;
        }

        foreach (var c in AllClauses().Where(x => x.Literals.Contains(literal) || x.Literals.Contains(-literal)).ToList())
        {
            if (c.Literals.Contains(literal))
            {
                CountClauses[c.Literals.Count].Remove(c);
                ClauseCount--;
                foreach (var lit in c.Literals)
                {
                    AllLiterals[lit]--;
                }
            }
            else
            {
                CountClauses[c.Literals.Count].Remove(c);

                c.Literals.Remove(-literal);
                AllLiterals[-literal]--;

                if (c.Literals.Count == 0)
                    HasEmptyClause = true;

                if (CountClauses.ContainsKey(c.Literals.Count))
                    CountClauses[c.Literals.Count].Add(c);
                else
                    CountClauses[c.Literals.Count] = new() { c };
            }
        }
    }

    public Formula Clone()
    {
        return new Formula(AllClauses().Select(c => c.Clone()).ToList());
    }

    private int FrequencyK(int literal, int k)
    {
        return CountClauses[k].Count(c => c.Literals.Contains(literal));
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        var empty = true;

        sb.Append('(');

        foreach (var c in AllClauses())
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
                        Clause newClause = new();
                        foreach (var num in line.Split(" "))
                        {
                            var number = int.Parse(num);

                            if (number == 0) continue;

                            if (newClause.Literals.Contains(number)) continue;

                            newClause.Literals.Add(number);

                            if (AllLiterals.ContainsKey(number))
                                AllLiterals[number]++;
                            else
                                AllLiterals[number] = 1;
                        }
                        if (CountClauses.ContainsKey(newClause.Literals.Count))
                            CountClauses[newClause.Literals.Count].Add(newClause);
                        else
                        {
                            CountClauses[newClause.Literals.Count] = new HashSet<Clause>();
                            CountClauses[newClause.Literals.Count].Add(newClause);
                        }

                        ClauseCount++;
                        break;
                    default:
                        Console.WriteLine(line);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            AllLiterals.Clear();
            CountClauses.Clear();
            Console.WriteLine("Error while parsing file");
            Console.WriteLine(ex.ToString());
        }
    }

    public int? GetFirstPureLiteral()
    {
        foreach (var l in AllLiterals.Keys)
        {
            if (AllLiterals[l] > 0 && AllLiterals[-l] == 0)
                return l;
        }

        return null;
    }

    public int? GetFirstSingletonClauseLiteral()
    {
        int? res = 0;
        if (CountClauses.ContainsKey(1))
        {
            res = CountClauses[1].FirstOrDefault()?.Literals.First();
        }

        return res == 0 ? null : res;
    }

    //DLIS
    public int Dlis()
    {
        var maxKey = AllLiterals.Keys.First();

        foreach (var l in AllLiterals.Keys)
        {
            if (AllLiterals[l] > AllLiterals[maxKey])
                maxKey = l;
        }

        Console.WriteLine(maxKey);
        return maxKey; // TODO minus
    }

    //DLCS
    public int Dlcs()
    {
        var maxKey = AllLiterals.Keys.First();

        foreach (var l in AllLiterals.Keys)
        {
            if (AllLiterals[l] + AllLiterals[-l] > AllLiterals[maxKey] + AllLiterals[-maxKey])
                maxKey = l;
        }

        if (AllLiterals[maxKey] >= AllLiterals[-maxKey])
            return -maxKey; // TODO minus
        return maxKey;
    }

    //MOM
    public int MOM()
    {
        var p = AllLiterals.Keys.Count * AllLiterals.Keys.Count + 1;

        var shortestClauses = CountClauses[GetShortestClause()]; //Collection of shortest clauses
        var literals = shortestClauses.SelectMany(x => x.Literals).ToHashSet(); //Unique literals in shortest clauses

        int maxLiteral = literals.First(); //Current best literal
        int maxLiteralValue = 0;

        var l1 = 0;
        var l2 = 0;
        foreach (var l in literals)
        {
            foreach (var c in shortestClauses)
            {
                if (c.Literals.Contains(l))
                    l1++;
                else if (c.Literals.Contains(-l))
                    l2++;
            }

            if ((l1 + l2) * p + l1 * l2 > maxLiteralValue)
                maxLiteral = l;
        }

        return maxLiteral;
    }

    private int GetShortestClause()
    {
        var shortest = CountClauses.Keys.First();

        foreach (var c in CountClauses.Keys)
        {
            if (CountClauses[c].Count > 0 && c < shortest)
                shortest = c;
        }

        return shortest;
    }

    //Bohm
    public int Bohm()
    {
        const int p1 = 1;
        const int p2 = 2;

        int maxLiteral = 0;
        int maxLiteralValue = 0;

        foreach (var l in AllLiterals.Keys)
        {
            var sum = 0;

            //For all clauses len
            for (var i = 2; i <= CountClauses.Keys.Max(); i++)
            {
                if (CountClauses[i].Count <= 0) continue;
                //Spočítám frekvence literálu v kaluzulích délky i
                var l1 = 0;
                var l2 = 0;
                foreach (var c in CountClauses[i])
                {
                    if (c.Literals.Contains(l))
                        l1++;
                    else if (c.Literals.Contains(-l))
                        l2++;
                }

                sum += p1 * Math.Max(l1, l2) + p2 * Math.Min(l1, l2);
            }
            if (sum > maxLiteralValue)
            {
                maxLiteral = l;
                maxLiteralValue = sum;
            }
        }
        if (maxLiteral == 0)
            throw new Exception("Bohm: maxLiteral == 0");
        return maxLiteral;
    }

    //MY find longest clause, foreach longest clause find most frequent literal
    public int My()
    {
        int maxLiteral = 0;
        int maxLiteralValue = 0;

        var longestClauses = CountClauses[GetLongestClause()]; //Collection of longest clauses
        var literals = longestClauses.SelectMany(x => x.Literals).ToHashSet();

        foreach (var l in literals)
        {
            var count = 0;
            foreach (var c in longestClauses)
            {
                if (c.Literals.Contains(l))
                    count++;
            }

            if (count > maxLiteralValue)
            {
                maxLiteral = l;
                maxLiteralValue = count;
            }
        }

        if (maxLiteral == 0)
            throw new Exception("My: maxLiteral == 0");
        return maxLiteral;
    }

    private int GetLongestClause()
    {
        var longest = 0;

        foreach (var c in CountClauses.Keys)
        {
            if (CountClauses[c].Count > 0 && c > longest)
                longest = c;
        }

        return longest;
    }

    public int RandomLiteral()
    {
        return AllLiterals.ElementAt(Random.Shared.Next(AllLiterals.Count)).Key;
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