using System;
using System.Collections.Generic;
using System.Text;

namespace assemblycounter
{
    class ExpressionParser
    {
        private static HashSet<char> validstarts = new HashSet<char>()
        {
            '+',
            '/',
            '*',
            '<'
        };

        private static List<(string, Func<int, int, int>)> operators = new List<(string, Func<int, int, int>)>()
        {
            ("*", (x, y) => x * y),
            ("/", (x, y) => x / y),
            ("+", (x, y) => x + y),
            ("<<", (x, y) => x << y)
        };

        public static int EvaluateCountString(string countstring)
        {
            countstring = countstring.Trim();

            {
                int value;
                if (int.TryParse(countstring, out value))
                    return value;
            }

            StringBuilder scan = new StringBuilder(countstring.Length);
            List<string> symbols = new List<string>();
            List<int> values = new List<int>();
            int parenscount = 0;
            foreach (char c in countstring)
            {
                if (parenscount > 0)
                {
                    if (c == '(')
                    {
                        parenscount++;
                        scan.Append(c);
                    }
                    else if (c == ')')
                    {
                        parenscount--;
                        if (parenscount == 0)
                        {
                            values.Add(EvaluateCountString(scan.ToString()));
                            scan.Clear();
                        }
                        else
                            scan.Append(c);
                    }
                    else
                        scan.Append(c);
                }
                else
                {
                    if (c == '(')
                    {
                        parenscount++;
                        if (scan.Length > 0)
                        {
                            Assert.That(!char.IsDigit(scan[0]));

                            symbols.Add(scan.ToString());
                            scan.Clear();
                        }
                    }
                    else if (c == ')')
                        throw new Exception("unexpected )");
                    else if (char.IsDigit(c))
                    {
                        if (scan.Length != 0 && !char.IsDigit(scan[scan.Length - 1]))
                        {
                            symbols.Add(scan.ToString());
                            scan.Clear();

                            Assert.That(symbols.Count == values.Count);
                        }

                        scan.Append(c);
                    }
                    else if (validstarts.Contains(c))
                    {
                        if (scan.Length != 0 && !validstarts.Contains(scan[scan.Length - 1]))
                        {
                            values.Add(int.Parse(scan.ToString()));
                            scan.Clear();

                            Assert.That(symbols.Count + 1 == values.Count);
                        }

                        scan.Append(c);
                    }
                }
            }

            if (scan.Length > 0)
            {
                Assert.That(char.IsDigit(scan[0]));
                values.Add(int.Parse(scan.ToString()));
            }


            foreach ((string, Func<int, int, int>) @operator in operators)
            {
                for (int i = symbols.Count - 1; i >= 0; i--)
                {
                    if (symbols[i] == @operator.Item1)
                    {
                        values[i] = @operator.Item2(values[i], values[i + 1]);
                        values.RemoveAt(i + 1);
                        symbols.RemoveAt(i);
                    }
                }
            }

            Assert.That(symbols.Count == 0);
            Assert.That(values.Count == 1);


            return values[0];

            throw new NotImplementedException();
        }
    }
}