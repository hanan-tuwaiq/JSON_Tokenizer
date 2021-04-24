using System;
using System.Collections.Generic;
namespace JSONTokenizer
{
    public delegate bool InputCondition(Input input);
    public class Input
    {
        private readonly string input;
        private readonly int length;
        private int position;
        private int lineNumber;
        //Properties
        public int Length
        {
            get
            {
                return this.length;
            }
        }
        public int Position
        {
            get
            {
                return this.position;
            }
        }
        public int NextPosition
        {
            get
            {
                return this.position + 1;
            }
        }
        public int LineNumber
        {
            get
            {
                return this.lineNumber;
            }
        }
        public char Character
        {
            get
            {
                if (this.position > -1) return this.input[this.position];
                else return '\0';
            }
        }
        public Input(string input)
        {
            this.input = input;
            this.length = input.Length;
            this.position = -1;
            this.lineNumber = 1;
        }
        public bool hasMore(int numOfSteps = 1)
        {
            if (numOfSteps <= 0) throw new Exception("Invalid number of steps");
            return (this.position + numOfSteps) < this.length;
        }
        public bool hasLess(int numOfSteps = 1)
        {
            if (numOfSteps <= 0) throw new Exception("Invalid number of steps");
            return (this.position - numOfSteps) > -1;
        }
        //callback -> delegate
        public Input step(int numOfSteps = 1)
        {
            if (this.hasMore(numOfSteps))
                this.position += numOfSteps;
            else
            {
                throw new Exception("There is no more step");
            }
            return this;
        }
        public Input back(int numOfSteps = 1)
        {
            if (this.hasLess(numOfSteps))
                this.position -= numOfSteps;
            else
            {
                throw new Exception("There is no more step");
            }
            return this;
        }
        public Input reset() { return this; }
        public char peek(int numOfSteps = 1)
        {
            if (this.hasMore()) return this.input[this.NextPosition];
            return '\0';
        }
        public string loop(InputCondition condition)
        {
            string buffer = "";
            while (this.hasMore() && condition(this))
                buffer += this.step().Character;
            return buffer;
        }
    }

    public class Token
    {
        public int Position { set; get; }
        public int LineNumber { set; get; }
        public string Type { set; get; }
        public string Value { set; get; }
        public List<Token> Tokens { set; get; }

        public Token(int position, int lineNumber, string type, string value)
        {
            this.Position = position;
            this.LineNumber = lineNumber;
            this.Type = type;
            this.Value = value;
            //this.Tokens = tokens;
        }

        public Token(int position, int lineNumber, string type, string value, List<Token> tokens)
        {
            this.Position = position;
            this.LineNumber = lineNumber;
            this.Type = type;
            this.Value = value;
            this.Tokens = tokens;
        }
    }
    public abstract class Tokenizable
    {
        public abstract bool tokenizable(Tokenizer tokenizer);
        public abstract Token tokenize(Tokenizer tokenizer);
    }
    public class Tokenizer
    {
        public List<Token> tokens;
        public bool enableHistory;
        public Input input;
        public Tokenizable[] handlers;
        public Tokenizer(string source, Tokenizable[] handlers)
        {
            this.input = new Input(source);
            this.handlers = handlers;
        }

        public Tokenizer(Input source, Tokenizable[] handlers)
        {
            this.input = source;
            this.handlers = handlers;
        }

        public Token tokenize()
        {
            foreach (var handler in this.handlers)
                if (handler.tokenizable(this)) return handler.tokenize(this);
            return null;
        }

        public List<Token> all() { return null; }
    }



    public class IdTokenizer : Tokenizable
    {
        private List<string> keywords;
        public IdTokenizer(List<string> keywords)
        {
            this.keywords = keywords;
        }
        public override bool tokenizable(Tokenizer t)
        {
            char currentCharacter = t.input.peek();
            //Console.WriteLine(currentCharacter);
            return Char.IsLetter(currentCharacter) || currentCharacter == '_';
        }
        static bool isId(Input input)
        {
            char currentCharacter = input.peek();
            return Char.IsLetterOrDigit(currentCharacter) || currentCharacter == '_';
        }
        public override Token tokenize(Tokenizer t)
        {
            return new Token(t.input.Position, t.input.LineNumber,
                "identifier", t.input.loop(isId));
        }
    }

    public class StringTokenizer : Tokenizable
    {
        private List<string> keywords;
        /*public StringTokenizer(List<string> keywords)
        {
            this.keywords = keywords;
        }
*/
        public override bool tokenizable(Tokenizer t)
        {
            char currentCharacter = t.input.peek();
            //Console.WriteLine(currentCharacter);
            return currentCharacter == '"';
        }

        public override Token tokenize(Tokenizer t)
        {
            t.input.step();
            Char currentChar = t.input.Character;
            //Console.WriteLine("t.input.Character:   {0}", currentChar);
            string buffer = "";
            buffer += currentChar;
            while (t.input.hasMore() && t.input.peek() != '"')
            {
                t.input.step();
                buffer += t.input.Character;
                //Console.WriteLine("BUFFER    {0}", buffer);
            }
            if (t.input.peek() == '"')
            {
                t.input.step();
                buffer += t.input.Character;
                return new Token(t.input.Position, t.input.LineNumber,
                    "string", buffer);
            }
            else
            {
                Console.WriteLine("{0}  Not a valid string!!", buffer);
                return null;
            }
        }
    }

    public class NumberTokenizer : Tokenizable
    {
        public override bool tokenizable(Tokenizer t)
        {
            return Char.IsDigit(t.input.peek());
        }
        static bool isDigit(Input input)
        {
            return Char.IsDigit(input.peek());
        }
        public override Token tokenize(Tokenizer t)
        {
            return new Token(t.input.Position, t.input.LineNumber,
                "number", t.input.loop(isDigit));
        }
    }
    public class WhiteSpaceTokenizer : Tokenizable
    {
        public override bool tokenizable(Tokenizer t)
        {
            return Char.IsWhiteSpace(t.input.peek());
        }
        static bool isWhiteSpace(Input input)
        {
            return Char.IsWhiteSpace(input.peek());
        }
        public override Token tokenize(Tokenizer t)
        {
            return new Token(t.input.Position, t.input.LineNumber,
                "whitespace", t.input.loop(isWhiteSpace));
        }
    }

    public class ArrayTokenizer : Tokenizable
    {
        // List<Token> arrayToken = new List<Token>();

        public override bool tokenizable(Tokenizer t)
        {
            char currentCharacter = t.input.peek();
            Console.WriteLine("{0} current char", currentCharacter);
            return currentCharacter == '[';
        }

        public override Token tokenize(Tokenizer t)
        {
            List<Token> arrayToken = new List<Token>();
            t.input.step();
            Token subToken;
            while (t.input.peek() != ']')
            {
                if (t.input.peek() == '[')
                {

                    Console.WriteLine("recursion");
                    Token recursionToken = tokenize(t);
                    Console.WriteLine("After recursion {0}", recursionToken.Value);
                    if (recursionToken != null)
                    {
                        arrayToken.Add(recursionToken);
                    }
                    else throw new Exception("Not a valid array");
                }
                if (t.input.peek() != ',')
                {
                    subToken = t.tokenize();
                    if (subToken != null)
                    {
                        arrayToken.Add(subToken);
                    }
                    else throw new Exception("not a valid token in the array");
                }
                else
                {
                    if (t.input.peek(2) == ']') throw new Exception("Invalid ,]");
                    t.input.step(); // pass the ,
                }
            }
            if (t.input.peek() == ']')
            {
                t.input.step();
                /* foreach (var item in arrayToken)
                 {
                     Console.WriteLine("Array item {0}", item.Value);
                 }*/
                return new Token(t.input.Position, t.input.LineNumber,
                "array", "array", arrayToken);
            }
            else
            {
                return null;
            }

        }


    }

    public class NullTokenizer : Tokenizable
    {
        /*  private List<string> keywords;
          public StringTokenizer(List<string> keywords)
          {
              this.keywords = keywords;
          }
        */
        public override bool tokenizable(Tokenizer t)
        {
            if (t.input.peek() == 'n' || t.input.peek() == 'N'
                && t.input.peek(2) == 'u' || t.input.peek(2) == 'U'
                && t.input.peek(3) == 'l' || t.input.peek(3) == 'L'
                && t.input.peek(4) == 'l' || t.input.peek(4) == 'L')
            {
                t.input.step(4);
                return true;
            }
            return false;
        }

        public override Token tokenize(Tokenizer t)
        {
            return new Token(t.input.Position, t.input.LineNumber, "null", "null");
        }
    }

    public class BoolTokenizer : Tokenizable
    {
        /*  private List<string> keywords;
          public StringTokenizer(List<string> keywords)
          {
              this.keywords = keywords;
          }
        */
        string value;
        public override bool tokenizable(Tokenizer t)
        {

            if (t.input.peek() == 't' || t.input.peek() == 'T'
                && t.input.peek(2) == 'r' || t.input.peek(2) == 'R'
                && t.input.peek(3) == 'u' || t.input.peek(3) == 'U'
                && t.input.peek(4) == 'e' || t.input.peek(4) == 'E')
            {
                value = "true";
                t.input.step(4);
                return true;
            }
            else if (t.input.peek() == 'f' || t.input.peek() == 'F'
              && t.input.peek(2) == 'a' || t.input.peek(2) == 'A'
              && t.input.peek(3) == 'l' || t.input.peek(3) == 'L'
              && t.input.peek(4) == 's' || t.input.peek(4) == 'S'
              && t.input.peek(5) == 'e' || t.input.peek(4) == 'E')
            {
                value = "false";
                t.input.step(5);
                return true;
            }
            return false;
        }

        public override Token tokenize(Tokenizer t)
        {
            return new Token(t.input.Position, t.input.LineNumber, "boolean", value);
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Tokenizer t = new Tokenizer(new Input("\"Hello\"null[55,90, false]\"string\"false[100,200]\"anotherstring\"[1,2,\"hi\",[3,4],5]"), new Tokenizable[] {
                new WhiteSpaceTokenizer(),
               /* new IdTokenizer(new List<string>
                {
                    "if","else","for","fun","return"
                }),*/
                new NumberTokenizer(),
                new StringTokenizer(),
                new NullTokenizer(),
                new BoolTokenizer(),
                new ArrayTokenizer()
            });
            Token token = t.tokenize();
            while (token != null)
            {
                Console.WriteLine("In Main() value:" + token.Value + "===" + "type:" + token.Type);

                if (token.Type == "array")
                {
                    foreach (var item in token.Tokens)
                    {
                        Console.WriteLine("array element value:" + item.Value + "            " + "array element type:" + item.Type);
                        if (item.Type == "array")
                        {
                            foreach (var subItem in item.Tokens)
                            {
                                Console.WriteLine("nested element value:" + subItem.Value + "            " + "nested element type:" + subItem.Type);
                            }
                        }
                    }
                }

                token = t.tokenize();

            }
        }

        /*        public static void printTokens(Token token)
                {
                    if (token.Type == "array")
                    {
                        Console.WriteLine("value:" + token.Value + "            " + "type:" + token.Type);
                        foreach (var item in token.Tokens)
                        {
                            *//*if (item.Type == "array") printTokens(item);
                            else return;*//*
                            Console.WriteLine("array element value:" + item.Value + "            " + "array element type:" + item.Type);
                        }
                    } else
                    {
                        Console.WriteLine("value:" + token.Value + "            " + "type:" + token.Type);
                    }
                }*/
    }
}
