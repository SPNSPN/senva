using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senva
{

class Interpreter
{
	public interface ICons
	{
		object car ();
		object cdr ();
	}

	public class Nil : ICons
	{
		public Nil () {}

		public object car () { return this; }
		public object cdr () { return this; }

		public static bool operator true (Nil o) { return false; }
		public static bool operator false (Nil o) { return false; }
	}

	public class Symb
	{
		public string name;

		public Symb (string name_) { this.name = name_; }
	}

	public class Cons : ICons
	{
		public object a;
		public object d;

		public Cons (object a_, object d_)
		{
			this.a = a_;
			this.d = d_;
		}

		public object car () { return this.a; }
		public object cdr () { return this.d; }

		public static bool operator true (Cons o) { return true; }
		public static bool operator false (Cons o) { return true; }
	}

	public class Queu
	{
		public ICons entr;
		public ICons exit;

		public Queu ()
		{
			this.entr = nil;
			this.exit = this.entr;
		}

		public Queu (ICons c)
		{
			this.entr = last(c) as ICons;
			this.exit = c;
		}

		public Queu push (object o)
		{
			var c = cons(o, nil);
			if (this.entr is Nil)
			{
				this.entr = c;
				this.exit = c;
			}
			else
			{
				rplacd(this.entr as Cons, c);
				this.entr = c;
			}
			return this;
		}
	}

	public enum ErroId
	{
		FullMemory
		, UnknownOpcode
		, OutOfEnvironment
		, Type
		, Symbol
		, Syntax
		, UnCallable
		, ArgsUnmatch
		, UnEvaluatable
		, FileNotFound
	}

	[Serializable()]
	public class Erro : System.Exception
	{
		public ErroId eid;

		public Erro () : base() {}
		public Erro (ErroId eid_, string estr) : base(estr) { this.eid = eid_; }
		public Erro (ErroId eid_, string estr, System.Exception inner)
			: base(estr, inner) { this.eid = eid_; }
		public Erro (System.Runtime.Serialization.SerializationInfo info
				, System.Runtime.Serialization.StreamingContext context) {}
	}

	static public Nil nil;
	static public Symb t;
	static private Dictionary<char, char> escape_char_table;

	public Cons genv;


	static Interpreter ()
	{
		nil = new Nil();
		t = new Symb("T");
	
		escape_char_table = new Dictionary<char, char>
		{
			{'a', '\a'}
			, {'b', '\b'}
			, {'f', '\f'}
			, {'n', '\n'}
			, {'r', '\r'}
			, {'t', '\t'}
			, {'v', '\v'}
			, {'0', '\0'}
		};
	}

	public Interpreter ()
	{
		genv = new Cons(nil, nil);
	}

	static public Cons cons (object a, object d) { return new Cons(a, d); }
	static public object car (ICons c) { return c.car(); }
	static public object cdr (ICons c) { return c.cdr(); }

	static public Cons rplaca (Cons c, object o)
	{
		c.a = o;
		return c;
	}

	static public Cons rplacd (Cons c, object o)
	{
		c.d = o;
		return c;
	}

	static public object last (object o)
	{
		if (o is Cons)
		{
			ICons rest = o as Cons;
			for (; cdr(rest) is Cons; rest = cdr(rest) as Cons) {}
			return rest;
		}
		if (o is Nil) { return nil; }
		if (o is Queu) { return (o as Queu).entr; }
		if (o is Symb) { return new Symb((o as Symb).name[(o as Symb).name.Length - 1].ToString()); }
		if (o is string) { return (o as string)[(o as string).Length - 1].ToString(); }
		if (o.GetType().IsArray) { return new object[]{(o as object[])[(o as object[]).Length - 1]}; }
		throw new Erro(ErroId.Type, string.Format("cannot apply last to {0}", print(o)));
	}

	static public ICons nconc (ICons colla, ICons collb)
	{
		if (colla is Nil) { return collb; }
		var las = last(colla);
		rplacd(las as Cons, collb);
		return colla;
	}

	static public ICons nreverse (ICons coll)
	{
		ICons rev = nil;
		for (object rest = coll; true; rest = cdr(rest as Cons))
		{
			Console.WriteLine(string.Format("debug: {0}", rest is Cons));
			var tmp = cdr(rest as Cons);
			Console.WriteLine(string.Format("debug: rplacd"));
			rplacd(rest as Cons, rev);
			Console.WriteLine(string.Format("debug: rev"));
			rev = rest as Cons;
			Console.WriteLine(string.Format("debug: rest"));
			rest = tmp;
			Console.WriteLine(string.Format("debug: loop: {0}", rest));
			Console.WriteLine(string.Format("debug: iscons: {0}", rest is Cons));
			if (! (rest is Cons)) { break; }
		}
		return rev;
	}

	static public ICons l (params object[] args)
	{
		return args.Aggregate(nil, (ICons acc, object e) => cons(e, acc));
	}

	private ICons growth (ICons tree, ref string tok, ref ICons rmacs)
	{
		if (tok.Length == 0) { return tree; }

		tok = "";
		rmacs = nil;
		if ("nil" == tok || "NIL" == tok)
		{
			return cons(wrap_readmacros(nil, rmacs), tree);
		}
		long inum;
		if (Int64.TryParse(tok, out inum))
		{
			return cons(wrap_readmacros(inum, rmacs), tree);
		}
		double fnum;
		if (Double.TryParse(tok, out fnum))
		{
			return cons(wrap_readmacros(fnum, rmacs), tree);
		}

		return cons(wrap_readmacros(new Symb(tok), rmacs), tree);
	}

	private int find_co_paren (string code)
	{
		var sflg = false;
		var eflg = false;
		var layer = 1;
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if (eflg) { eflg = false; }
			else if (! sflg && '(' == c) { ++layer; }
			else if (! sflg && ')' == c) { --layer; }
			else if ('\\' == c) { eflg = true; continue; }
			else if ('"' == c) { sflg = ! sflg; }

			if (layer < 1) { return idx; }
		}
		throw new Erro(ErroId.Syntax, "not found close parenthesis.");
	}

	private int find_co_bracket (string code)
	{
		var sflg = false;
		var eflg = false;
		var layer = 1;
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if (eflg) { eflg = false; }
			else if (! sflg && '[' == c) { ++layer; }
			else if (! sflg && ']' == c) { --layer; }
			else if ('\\' == c) { eflg = true; continue; }
			else if ('"' == c) { sflg = ! sflg; }

			if (layer < 1) { return idx; }
		}
		throw new Erro(ErroId.Syntax, "not found close brackets.");
	}

	private dynamic take_string (string code)
	{
		var eflg = false;
		var strn = "";
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if (eflg)
			{
				if (escape_char_table.ContainsKey(c)) { c = escape_char_table[c]; }
				eflg = false;
			}
			else if ('\\' == c)
			{
				eflg = true;
				continue;
			}
			else if ('"' == c) { return new {strn = strn, inc = idx + 1}; }
			strn += c;
		}
		throw new Erro(ErroId.Syntax, "not found close double quote.");
	}

	private object wrap_readmacros (object o, ICons rmacs)
	{
		var wraped = o;
		for (object rest = rmacs; rest is Cons; rest = cdr(rest as Cons))
		{
			wraped = l(car(rest as Cons), wraped);
		}
		return wraped;
	}

	public ICons read (string code)
	{
		ICons tree = nil;
		var tok = "";
		ICons rmacs = nil;
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if ('(' == c)
			{
				var co = find_co_paren(code.Substring(idx + 1));
				tree = growth(tree, ref tok, ref rmacs);
				tree = cons(wrap_readmacros(read(code.Substring(idx + 1, co))
							, rmacs), tree);
				tok = "";
				rmacs = nil;
				idx += co + 1;
			}
			else if (')' == c)
			{
				throw new Erro(ErroId.Syntax, "found excess close parenthesis.");
			}
			else if ('[' == c)
			{
				var co = find_co_bracket(code.Substring(idx + 1));
				tree = growth(tree, ref tok, ref rmacs);
				ICons invec = read(code.Substring(idx + 1, co));
				tree = (rmacs is Nil)
					? cons(cons(new Symb("vect"), invec), tree)
					: cons(l(new Symb("to-vect"), wrap_readmacros(invec, rmacs)), tree);
				tok = "";
				rmacs = nil;
				idx += co + 1;
			}
			else if (']' == c)
			{
				throw new Erro(ErroId.Syntax, "found excess close brackets.");
			}
			else if (' ' == c || '\t' == c || '\n' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
			}
			else if (';' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				for (; idx < code.Length && '\n' != code[idx]; ++idx) {}
			}
			else if ('"' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				var res = take_string(code.Substring(idx + 1));
				idx += res.inc;
				tree = cons(res.strn, tree);
				tok = "";
				rmacs = nil;
			}
			else if ('\'' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("quote"), rmacs);
			}
			else if ('`' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("quasiquote"), rmacs);
			}
			else if (',' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("unquote"), rmacs);
			}
			else if ('@' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("splicing"), rmacs);
			}
			else if ('^' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(new Symb("tee"), rmacs);
			}
			else if ('.' == c)
			{
				if (tok.Length == 0)
				{
					return nconc(nreverse(cdr(tree) as ICons)
							, cons(car(tree), car(read(code.Substring(idx + 1)))));
				}
				tok += ".";
			}
			else { tok += c; }
		}
		tree = growth(tree, ref tok, ref rmacs);
		return nreverse(tree);
	}

	public object eval (object expr)
	{
		return expr; // TODO
	}

	static public string print (object expr)
	{
		return "nil"; // TODO
	}
}
}

