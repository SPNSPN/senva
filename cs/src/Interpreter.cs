using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Senva
{

class Interpreter
{
	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	internal static extern IntPtr LoadLibrary (string lpFileName);

	[DllImport("kernel32", SetLastError = true)]
	internal static extern bool FreeLibrary (IntPtr hModule);

	[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
	internal static extern IntPtr GetProcAddress (IntPtr hModule, string  lpProcName);

	public T dynamic_cast<T> (object obj, T ct) { return (T)obj; }

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
		private static Dictionary<string, Symb> identifiers;
		public string name;

		static Symb () { Symb.identifiers = new Dictionary<string, Symb>(); }
		public Symb (string name_) { this.name = name_; }

		public static Symb intern (string name)
		{
			Symb sym;
			if (Symb.identifiers.TryGetValue(name, out sym)) { return sym; }
			sym = new Symb(name);
			Symb.identifiers[name] = sym;
			return sym;
		}

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

		public object pop ()
		{
			if (exit is Nil) { return nil; }

			var e = car(exit as Cons);
			if (ReferenceEquals(exit, entr))
			{
				exit = nil;
				entr = nil;
			}
			else { exit = cdr(exit as Cons) as ICons; }
			return e;
		}

		public Queu concat (Queu queu)
		{
			if (entr is Nil)
			{
				entr = queu.entr;
				exit = queu.exit;
			}
			else if (! (queu.exit is Nil))
			{
				rplacd(entr as Cons, queu.exit);
				entr = queu.entr;
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
		public long eid;

		public Erro () : base() {}
		public Erro (long eid_, string estr) : base(estr) { this.eid = eid_; }
		public Erro (ErroId eid_, string estr) : base(estr) { this.eid = (long)eid_; }
		public Erro (long eid_, string estr, System.Exception inner)
			: base(estr, inner) { this.eid = eid_; }
		public Erro (ErroId eid_, string estr, System.Exception inner)
			: base(estr, inner) { this.eid = (long)eid_; }
		protected Erro (System.Runtime.Serialization.SerializationInfo info
				, System.Runtime.Serialization.StreamingContext context) : base(info, context) {}
	}

	public class Subr
	{
		public Func<ICons, object> proc;
		public string name;

		public Subr (Func<ICons, object> proc_, string name_)
		{
			this.proc = proc_;
			this.name = name_;
		}
	}

	public class Spfm
	{
		public Func<ICons, ICons, object> proc;
		public string name;

		public Spfm (Func<ICons, ICons, object> proc_, string name_)
		{
			this.proc = proc_;
			this.name = name_;
		}
	}

	public class LFunc
	{
		public object args;
		public object body;
		public ICons env;

		public LFunc (object args_, object body_, ICons env_)
		{
			this.args = args_;
			this.body = body_;
			this.env = env_;
		}
	}

	static public Nil nil;
	static public Symb t;

	static private Dictionary<char, char> escape_char_table;

	public Cons genv;


	static Interpreter ()
	{
		nil = new Nil();
		t = Symb.intern("T");
	
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

	public delegate object debug_del (object arg);

	public Interpreter ()
	{
		genv = new Cons(nil, nil);
		regist(genv, "nil", nil);
		regist(genv, "t", t);
		regist(genv, "T", t);
		regist_subr2<object, object, Cons>(genv, "cons", cons);
		regist_subr1<ICons, object>(genv, "car", car);
		regist_subr1<ICons, object>(genv, "cdr", cdr);
		regist_subr2<object, object, object>(genv, "eq", eq);
		regist_subr2<object, object, object>(genv, "equal", equal);
		regist_subr1<object, object>(genv, "atom", atom);
		regist(genv, "list", new Subr((ICons args) => { return args; }, "list"));
		regist(genv, "+", new Subr((ICons args) => { return ladd(args); }, "+"));
		regist(genv, "-", new Subr((ICons args)
					=> { return lsub(car(args), cdr(args) as ICons); }, "-"));
		regist(genv, "*", new Subr((ICons args) => { return lmul(args); }, "*"));
		regist(genv, "/", new Subr((ICons args)
					=> { return ldiv(car(args), cdr(args) as ICons); }, "/"));
		regist_subr2<object, object, long>(genv, "%", lmod);
		regist(genv, ">", new Subr((ICons args)
					=> { return lgt(car(args), cdr(args) as ICons); }, ">"));
		regist(genv, "<", new Subr((ICons args)
					=> { return llt(car(args), cdr(args) as ICons); }, "<"));
		regist(genv, ">=", new Subr((ICons args)
					=> { return lge(car(args), cdr(args) as ICons); }, ">="));
		regist(genv, "<=", new Subr((ICons args)
					=> { return lle(car(args), cdr(args) as ICons); }, "<="));
		regist_subr1<object, long>(genv, "int", lint);
		regist_subr1<object, double>(genv, "float", lfloat);

		regist_subr2<Cons, object, Cons>(genv, "rplaca", rplaca);
		regist_subr2<Cons, object, Cons>(genv, "rplacd", rplacd);
		regist_subr1<object, object>(genv, "last", last);
		regist_subr2<ICons, ICons, ICons>(genv, "nconc", nconc);
		regist_subr1<ICons, ICons>(genv, "nreverse", nreverse);
		regist(genv, "vect", new Subr((ICons args)
					=> { return cons2vect(args); }, "vect"));
		regist(genv, "queu", new Subr((ICons args)
					=> { return new Queu(args); }, "queu"));
		regist_subr2<Queu, object, Queu>(genv, "pushqueu"
				, (Queu queu, object o) => { return queu.push(o); });
		regist_subr1<Queu, object>(genv, "popqueu"
				, (Queu queu) => { return queu.pop(); });
		regist_subr2<Queu, Queu, Queu>(genv, "concqueu"
				, (Queu qa, Queu qb) => { return qa.concat(qb); });
		regist_subr1<object, ICons>(genv, "to-list", to_list);
		regist_subr1<object, object[]>(genv, "to-vect", to_vect);
		regist_subr1<object, Queu>(genv, "to-queu", to_queu);
		regist_subr1<object, Symb>(genv, "symbol", symbol);
		regist(genv, "sprint", new Subr(lsprint, "sprint"));
		regist_subr2<object, ICons, object>(genv, "apply", lapply);
		regist_subr2<long, string, object>(genv, "throw", lthrow);
		regist_subr1<object, object>(genv, "empty", lempty);
		regist(genv, "print", new Subr(
					(ICons args) => { return llprint(args); }, "print"));
		regist(genv, "prin", new Subr(
					(ICons args) => { return llprin(args); }, "prin"));
		regist_subr1<object, Symb>(genv, "type", ltype);
		regist_subr1<string, object>(genv, "load", lload);
		regist_subr2<object, long, object>(genv, "getat", lgetat);
		regist_subr3<object, long, object, object>(genv, "setat", lsetat);
		regist(genv, "processor", new Subr(
					(ICons args) => { return Symb.intern("cs"); }, "processor"));
		regist_subr1<object, object>(genv, "tee", tee);
		regist(genv, "exit", new Subr(
					(ICons args) => { Environment.Exit(0); return nil; }, "exit"));
		regist_subr1<string, IntPtr>(genv, "loaddll"
				, (string path) => { return LoadLibrary(path); });
		regist_subr1<IntPtr, object>(genv, "freedll"
				, (IntPtr dll) => { return FreeLibrary(dll); });
		regist_subr1<string, object>(genv, "gettype"
				, (string typename) =>
				{
					Type typ = Type.GetType(typename);
					if (typ == null) { return nil; }
					return typ;
				});
		Func <int, int> testfn = (int a) => { return a + 1; }; // debug
		Console.WriteLine("debug: {0}", testfn.GetType());
		regist_subr3<IntPtr, string, Type, object>(genv, "getproc"
				, (IntPtr dll, string procname, Type proctype) =>
				{
					return dynamic_cast(
							Marshal.GetDelegateForFunctionPointer(
								GetProcAddress(dll, procname), proctype)
							, proctype);
				});
		regist_subr2<object, string, object>(genv, "->", lattr);
		regist_subr1<Type, object>(genv, "new"
				, (Type typ) => { return System.Activator.CreateInstance(typ); });
//		regist_subr0<Assembly[]>(genv, "asms"
//				, () => { return AppDomain.CurrentDomain.GetAssemblies(); });

		regist(genv, "quote", new Spfm((ICons env, ICons args)
					=> { return car(args); }, "quote"));
		regist(genv, "quasiquote", new Spfm(
					(ICons env, ICons args)
					=> { return expand_quasiquote(car(args), env); }
					, "quasiquote"));
		regist(genv, "if", new Spfm(lif, "if"));
		regist(genv, "lambda", new Spfm((ICons env, ICons args)
					=> { return new LFunc(car(args), nth(args, 1), env); }
					, "lambda"));
		regist(genv, "define", new Spfm(ldefine, "define"));
		regist(genv, "setq", new Spfm(lsetq, "setq"));
		regist(genv, "and", new Spfm(land, "and"));
		regist(genv, "or", new Spfm(lor, "or"));
		regist(genv, "!", new Spfm((ICons env, ICons args) =>
					{
						return leval(lapply(leval(car(args), env), cdr(args) as ICons), env);
					}, "!"));
		regist(genv, "do", new Spfm(ldo, "do"));
		regist(genv, "catch", new Spfm(lcatch, "catch"));
		regist(genv, "environment", new Spfm(
					(ICons env, ICons args) => { return env; }, "environment"));

	}

	static private void regist (Cons env, string name, object obj)
	{
		rplaca(env, cons(cons(Symb.intern(name), obj), car(env)));
	}

	static private void regist_subr0<R> (Cons env, string name, Func<R> proc)
	{
		regist(env, name, new Subr((ICons args) => { return proc(); }, name));
	}

	static private void regist_subr1<T, R> (Cons env, string name, Func<T, R> proc)
	{
		regist(env, name, new Subr((ICons args) =>
					{
						try
						{
							return proc((T)(car(args)));
						}
						catch (InvalidCastException)
						{
							throw new Erro(ErroId.Type
									, string.Format("cannot apply {0} to {1}"
										, name, lprint(car(args))));
						}
					}, name));
	}

	static private void regist_subr2<T1, T2, R> (Cons env, string name
			, Func<T1, T2, R> proc)
	{
		regist(env, name, new Subr((ICons args) =>
					{
						try
						{
							return proc((T1)(car(args)), (T2)(nth(args, 1L)));
						}
						catch (InvalidCastException)
						{
							throw new Erro(ErroId.Type
									, string.Format("cannot apply {0} to {1}"
										, name, lprint(car(args))));
						}
					}, name));
	}

	static private void regist_subr3<T1, T2, T3, R> (Cons env, string name
			, Func<T1, T2, T3, R> proc)
	{
		regist(env, name, new Subr((ICons args) =>
					{
						try
						{
							return proc((T1)(car(args))
									, (T2)(nth(args, 1L))
									, (T3)(nth(args, 2L)));
						}
						catch (InvalidCastException)
						{
							throw new Erro(ErroId.Type
									, string.Format("cannot apply {0} to {1}"
										, name, lprint(car(args))));
						}
					}, name));
	}

	static private bool is_inum (object n)
	{
		return (n is int) || (n is uint) || (n is long) || (n is ulong)
			|| (n is byte) || (n is sbyte) || (n is short) || (n is ushort);
   	}

	static private bool is_fnum (object n)
	{
		return (n is float) || (n is double) || (n is decimal);
	}

	static public Cons cons (object a, object d) { return new Cons(a, d); }
	static public object car (ICons c) { return c.car(); }
	static public object cdr (ICons c) { return c.cdr(); }
	static public object atom (object o) { return (o is Cons) ? nil as object : t as object; }
	static public object eq (object a, object b)
	{
		return ReferenceEquals(a, b) ? t as object : nil as object;
//		return (ReferenceEquals(a, b)
//				|| (a is Symb && b is Symb
//					&& ((a as Symb).name == (b as Symb).name)))
//			? t as object : nil as object;
	}

	static public object equal (object a, object b)
	{
		bool cond = false;
		if (ReferenceEquals(a, b)) { cond = true; }
		else if (a is Symb && b is Symb)
		{
			cond = ((a as Symb).name == (b as Symb).name);
		}
		else if (a is Cons && b is Cons)
		{
			cond = (! (equal((a as Cons).a, (b as Cons).a) is Nil
						|| equal((a as Cons).d, (b as Cons).d) is Nil));
		}
		else if (a is Queu && b is Queu)
		{
			cond = (equal((a as Queu).exit, (b as Queu).exit) is Nil) ? false : true;
		}
		else if (a is LFunc && b is LFunc)
		{
			cond = (! (equal((a as LFunc).args, (b as LFunc).args) is Nil
						|| equal((a as LFunc).body, (b as LFunc).body) is Nil
						|| equal((a as LFunc).env, (b as LFunc).env) is Nil));
		}
		else if (a is Spfm && b is Spfm)
		{
			cond = (! (equal((a as Spfm).proc, (b as Spfm).proc) is Nil
					|| equal((a as Spfm).name, (b as Spfm).name) is Nil));
		}
		else if (a.GetType().IsArray && b.GetType().IsArray)
		{
			if ((a as object[]).Length == (b as object[]).Length)
			{
				cond = true;
				for (var idx = 0L; idx < (a as object[]).Length; ++idx)
				{
					if (equal((a as object[])[idx], (b as object[])[idx]) is Nil)
					{
						cond = false;
						break;
					}
				}
			}
		}
		else if ((is_inum(a) || is_fnum(a)) && (is_inum(b) || is_fnum(b)))
		{
			cond = (Convert.ToDouble(a) == Convert.ToDouble(b));
		}
		else if (a is string && b is string)
		{
			cond = a.Equals(b);
		}
		else { cond = (a == b); }
		return (cond) ? t as object : nil as object;
	}

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

	static public object ladd (ICons nums)
	{
		var iacc = 0L;
		var facc = 0.0;
		var isint = true;
		for (object rest = nums; rest is Cons; rest = cdr(rest as Cons))
		{
			var n = car(rest as Cons);
			if (is_inum(n)) { iacc += Convert.ToInt64(n); }
			else if (is_fnum(n)) { facc += Convert.ToDouble(n); isint = false; }
			else { throw new Erro(ErroId.Type
					, string.Format("cannot add {0}", lprint(nums))); }
		}
		if (isint) { return iacc; }
		return iacc + facc;
	}

	static public object lsub (object head, ICons nums)
	{
		if (! (is_inum(head) || is_fnum(head)))
		{
			throw new Erro(ErroId.Type, string.Format("cannot sub {0}"
						, lprint(cons(head, nums))));
		}

		var isint = true;
		var iacc = 0L;
		var facc = 0.0;
		if (is_inum(head)) { iacc = Convert.ToInt64(head); }
		else if (is_fnum(head)) { facc = Convert.ToDouble(head); isint = false; }
		for (object rest = nums; rest is Cons; rest = cdr(rest as Cons))
		{
			var n = car(rest as Cons);
			if (is_inum(n)) { iacc -= Convert.ToInt64(n); }
			else if (is_fnum(n)) { facc -= Convert.ToDouble(n); isint = false; }
			else { throw new Erro(ErroId.Type
					, string.Format("cannot sub {0}", lprint(cons(head, nums)))); }
		}
		if (isint) { return iacc; }
		return iacc + facc;
	}

	static public object lmul (ICons nums)
	{
		var iacc = 1L;
		var facc = 1.0;
		var isint = true;
		for (object rest = nums; rest is Cons; rest = cdr(rest as Cons))
		{
			var n = car(rest as Cons);
			if (is_inum(n)) { iacc *= Convert.ToInt64(n); }
			else if (is_fnum(n)) { facc *= Convert.ToDouble(n); isint = false; }
			else { throw new Erro(ErroId.Type
					, string.Format("cannot mul {0}", lprint(nums))); }
		}
		if (isint) { return iacc; }
		return iacc * facc;
	}

	static public object ldiv (object head, ICons nums)
	{
		if (! (is_inum(head) || is_fnum(head)))
		{
			throw new Erro(ErroId.Type
					, string.Format("cannot div {0}", lprint(cons(head, nums))));
		}

		var isint = true;
		var iacc = 1L;
		var facc = 1.0;
		if (is_inum(head)) { iacc = Convert.ToInt64(head); }
		else if (is_fnum(head)) { facc = Convert.ToDouble(head); isint = false; }

		for (object rest = nums; rest is Cons; rest = cdr(rest as Cons))
		{
			var n = car(rest as Cons);
			if (is_inum(n))
			{
				var ln = Convert.ToInt64(n);
				if ((iacc % ln).Equals(0L)) { iacc /= Convert.ToInt64(ln); }
				else
				{
					facc /= Convert.ToDouble(n);
					isint = false;
				}
			}
			else if (is_fnum(n))
			{
				facc /= Convert.ToDouble(n);
				isint = false;
			}
			else { throw new Erro(ErroId.Type
					, string.Format("cannot div {0}", lprint(cons(head, nums)))); }
		}
		if (isint) { return iacc; }
		return iacc * facc;
	}

	static public long lmod (object a, object b)
	{
		if (! (is_inum(a) && is_inum(b)))
		{
			throw new Erro(ErroId.Type
					, string.Format("cannot mod {0}", lprint(l(a, b))));
		}

		return Convert.ToInt64(a) % Convert.ToInt64(b);
	}

	static public object lgt (object head, ICons nums)
	{
		if (! (is_inum(head) || is_fnum(head)))
		{
			throw new Erro(ErroId.Type
					, string.Format("cannot gt {0}", lprint(cons(head, nums))));
		}

		var a = Convert.ToDouble(head);
		for (object rest = nums; rest is Cons; rest = cdr(rest as Cons))
		{
			if (! (is_inum(car(rest as Cons)) || is_fnum(car(rest as Cons))))
			{
				throw new Erro(ErroId.Type
						, string.Format("cannot gt {0}", lprint(cons(head, nums))));
			}

			var n = Convert.ToDouble(car(rest as Cons));
			if (a > n) { a = n; }
			else { return nil; }
		}
		return t;
	}

	static public object llt (object head, ICons nums)
	{
		if (! (is_inum(head) || is_fnum(head)))
		{
			throw new Erro(ErroId.Type
					, string.Format("cannot lt {0}", lprint(cons(head, nums))));
		}

		var a = Convert.ToDouble(head);
		for (object rest = nums; rest is Cons; rest = cdr(rest as Cons))
		{
			if (! (is_inum(car(rest as Cons)) || is_fnum(car(rest as Cons))))
			{
				throw new Erro(ErroId.Type
						, string.Format("cannot lt {0}", lprint(cons(head, nums))));
			}

			var n = Convert.ToDouble(car(rest as Cons));
			if (a < n) { a = n; }
			else { return nil; }
		}
		return t;
	}
	
	static public object lge (object head, ICons nums)
	{
		if (! (is_inum(head) || is_fnum(head)))
		{
			throw new Erro(ErroId.Type
					, string.Format("cannot ge {0}", lprint(cons(head, nums))));
		}

		var a = Convert.ToDouble(head);
		for (object rest = nums; rest is Cons; rest = cdr(rest as Cons))
		{
			if (! (is_inum(car(rest as Cons)) || is_fnum(car(rest as Cons))))
			{
				throw new Erro(ErroId.Type
						, string.Format("cannot ge {0}", lprint(cons(head, nums))));
			}

			var n = Convert.ToDouble(car(rest as Cons));
			if (a < n) { return nil; }
			a = n;
		}
		return t;
	}

	static public object lle (object head, ICons nums)
	{
		if (! (is_inum(head) || is_fnum(head)))
		{
			throw new Erro(ErroId.Type
					, string.Format("cannot le {0}", lprint(cons(head, nums))));
		}

		var a = Convert.ToDouble(head);
		for (object rest = nums; rest is Cons; rest = cdr(rest as Cons))
		{
			if (! (is_inum(car(rest as Cons)) || is_fnum(car(rest as Cons))))
			{
				throw new Erro(ErroId.Type
						, string.Format("cannot le {0}", lprint(cons(head, nums))));
			}

			var n = Convert.ToDouble(car(rest as Cons));
			if (a > n) { return nil; }
			a = n;
		}
		return t;
	}

	static public long lint (object o)
	{
		if (is_inum(o) || is_fnum(o)) { return Convert.ToInt64(o); }
		throw new Erro(ErroId.Type
				, string.Format("cannot cast {0} to InumT.", lprint(o)));
	}

	static public double lfloat (object o)
	{
		if (is_inum(o) || is_fnum(o)) { return Convert.ToDouble(o); }
		throw new Erro(ErroId.Type
				, string.Format("cannot cast {0} to FnumT.", lprint(o)));
	}

	static public Symb ltype (object o)
	{
		if (o is Cons) { return Symb.intern("<cons>"); }
		if (o is LFunc) { return Symb.intern("<func>"); }
		if (o is Spfm) { return Symb.intern("<spfm>"); }
		if (o is Subr) { return Symb.intern("<subr>"); }
		if (o is Symb) { return Symb.intern("<symb>"); }
		if (o is string) { return Symb.intern("<strn>"); }
		if (is_inum(o)) { return Symb.intern("<inum>"); }
		if (is_fnum(o)) { return Symb.intern("<fnum>"); }
		if (o is Nil) { return Symb.intern("<nil>"); }
		if (o.GetType().IsArray) { return Symb.intern("<vect>"); }
		if (o is Queu) { return Symb.intern("<queu>"); }
		return Symb.intern(string.Format("<cs {0}>", o.GetType()));
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
		if (o is Symb) { return Symb.intern((o as Symb).name[(o as Symb).name.Length - 1].ToString()); }
		if (o is string) { return (o as string)[(o as string).Length - 1].ToString(); }
		if (o.GetType().IsArray) { return new object[]{(o as object[])[(o as object[]).Length - 1]}; }
		throw new Erro(ErroId.Type, string.Format("cannot apply last to {0}", lprint(o)));
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
		for (object rest = coll; rest is Cons; )
		{
			var tmp = cdr(rest as Cons);
			rplacd(rest as Cons, rev);
			rev = rest as Cons;
			rest = tmp;
		}
		return rev;
	}

	public object lload (string path)
	{
		try
		{
			return leval(lreadtop(System.IO.File.ReadAllText(path)), genv);
		}
		catch (System.IO.DirectoryNotFoundException)
		{
			throw new Erro(ErroId.FileNotFound
					, string.Format("not found file: {0}", lprint(path)));
		}
	}

	static public ICons to_list (object obj)
	{
		if (obj.GetType().IsArray) { return vect2cons(obj as object[]); }
		if (obj is Symb)
		{
			return vect2cons((obj as Symb).name.ToCharArray().Select(
						(e) => { return Convert.ToInt64(e) as object; }).ToArray());
		}
		if (obj is string)
		{
			return vect2cons((obj as string).ToCharArray().Select(
						(e) => { return Convert.ToInt64(e) as object; }).ToArray());
		}
		if (obj is Queu) { return (obj as Queu).exit; }
		if (obj is ICons) { return obj as ICons; }
		throw new Erro(ErroId.Type
				, string.Format("cannot cast {0} to ConsT.", lprint(obj)));
	}

	static public object[] to_vect (object obj)
	{
		if (obj is ICons) { return cons2vect(obj as ICons); }
		if (obj is Symb)
		{
			return (obj as Symb).name.ToCharArray().Select(
					(e) => { return Convert.ToInt64(e) as object; }).ToArray();
		}
		if (obj is string)
		{
			return (obj as string).ToCharArray().Select(
					(e) => { return Convert.ToInt64(e) as object; }).ToArray();
		}
		if (obj is Queu) { return cons2vect((obj as Queu).exit); }
		if (obj.GetType().IsArray) { return obj as object[]; }
		throw new Erro(ErroId.Type
				, string.Format("cannot cast {0} to VectT.", lprint(obj)));
	}

	static public Queu to_queu (object obj)
	{
		if (obj is Cons) { return new Queu(obj as Cons); }
		if (obj is Symb)
		{
			return new Queu(vect2cons((obj as Symb).name.ToCharArray().Select((e)
							=> { return Convert.ToInt64(e) as object; }).ToArray()));
		}
		if (obj is string)
		{
			return new Queu(vect2cons((obj as string).ToCharArray().Select((e)
							=> { return Convert.ToInt64(e) as object; }).ToArray()));
		}
		if (obj.GetType().IsArray) { return new Queu(vect2cons(obj as object[])); }
		if (obj is Queu) { return obj as Queu; }
		if (obj is Nil) { return new Queu(); }
		throw new Erro(ErroId.Type
				, string.Format("cannot cast {0} to QueuT.", lprint(obj)));
	}

	static public Symb symbol (object obj)
	{
		if (obj is ICons)
		{
			var strn = "";
			for (object rest = obj; rest is Cons; rest = cdr(rest as Cons))
			{
				strn += Convert.ToChar(car(rest as Cons));
			}
			return Symb.intern(strn);
		}
		if (obj is Queu)
		{
			return Symb.intern(cons2vect((obj as Queu).exit).Aggregate(""
						, (string acc, object e) => { return acc + Convert.ToChar(e).ToString(); } ));
		}
		if (obj.GetType().IsArray)
		{
			return Symb.intern((obj as object[]).Aggregate(""
							,(string acc, object e)
							=> { return acc + Convert.ToChar(e).ToString(); }));
		}
		if (obj is string) { return Symb.intern(obj as string); }
		if (obj is Symb) { return obj as Symb; }
		throw new Erro(ErroId.Type
				, string.Format("cannot cast {0} to SymbT.", lprint(obj)));
	}

	static public string lsprint (ICons args)
	{
		var strn = "";
		for (object rest = args; rest is Cons; rest = cdr(rest as Cons))
		{
			var e = car(rest as Cons);
			strn += (e is string) ? e as string : lprint(e);
		}
		return strn;
	}

	static public object tee (object obj)
	{
		Console.WriteLine(lprint(obj));
		return obj;
	}

	static public object lattr (object obj, string name)
	{
		MemberInfo[] mis = obj.GetType().GetMember(name);
		return mis.Select<MemberInfo, object>((mi) =>
				{
					Console.WriteLine(string.Format("debug: typ: {0}", mi.MemberType));
					if (mi.MemberType == MemberTypes.Event)
				   	{
						return obj.GetType().GetEvent(name);
					}
					if (mi.MemberType == MemberTypes.Field)
					{
						return obj.GetType().GetField(name);
					}
					if (mi.MemberType == MemberTypes.Method)
					{
						// TODO 関数型により同名メソッド呼び分ける
						// return new Subr((sargs) => { return obj.GetType().Getmethod(name); }, name);
						
						return obj.GetType().GetMethod(name);
					}
					if (mi.MemberType == MemberTypes.NestedType)
					{
						return obj.GetType().GetNestedType(name);
					}
					if (mi.MemberType == MemberTypes.Property)
					{
						return obj.GetType().GetProperty(name).GetValue(obj);
					}
					return nil;
				}).ToArray();
	}

	static public object nth (ICons c, long n)
	{
		object rest = c;
		for (var idx = 0L; idx < n; ++idx)
		{
			if (rest is Cons) { rest = cdr(rest as Cons); }
			else
			{
				throw new Erro(ErroId.ArgsUnmatch
						, string.Format("cannot got nth {0} from {1}", n, lprint(c)));
			}
		}
		if (rest is ICons) { return car(rest as ICons); }
		throw new Erro(ErroId.ArgsUnmatch
				, string.Format("cannot got nth {0} from {1}", n, lprint(c)));
	}

	public object lif (ICons env, ICons args)
	{
		return leval(nth(args, (leval(car(args), env) is Nil) ? 2 : 1), env);
	}

	public object ldefine (ICons env, ICons args)
	{
		var sym = car(args);
		if (sym is Symb)
		{
			var asc = assoc(car(genv) as ICons, sym);
			if (asc is Nil)
			{
				rplaca(genv, cons(cons(sym, leval(nth(args, 1), env)), car(genv)));
			}
			else { rplacd(asc as Cons, leval(nth(args, 1), env)); }
			return sym;
		}
		throw new Erro(ErroId.Type
				, string.Format("cannot define {0}, must be SymbT.", lprint(sym)));
	}

	public object lsetq (ICons env, ICons args)
	{
		var sym = car(args);
		if (sym is Symb)
		{
			for (object rest = env; rest is Cons; rest = cdr(rest as Cons))
			{
				var asc = assoc(car(rest as Cons) as ICons, sym);
				if (asc is Cons)
				{
					rplacd(asc as Cons, leval(nth(args, 1), env));
					return cdr(asc as ICons);
				}
			}
			throw new Erro(ErroId.Symbol, string.Format("{0} is not defined."
						, lprint(sym)));
		}
		throw new Erro(ErroId.Type
				, string.Format("cannot setq {0}, must be SymbT.", lprint(sym)));
	}

	public object land (ICons env, ICons args)
	{
		object ret = t;
		for (object rest = args; rest is Cons; rest = cdr(rest as Cons))
		{
			ret = leval(car(rest as Cons),  env);
			if (ret is Nil) { return nil; }
		}
		return ret;
	}

	public object lor (ICons env, ICons args)
	{
		object ret = nil;
		for (object rest = args; rest is Cons; rest = cdr(rest as Cons))
		{
			ret = leval(car(rest as Cons), env);
			if (! (ret is Nil)) { return ret; }
		}
		return nil;
	}


	public object ldo (ICons env, ICons args)
	{
		object rest = args;
		for (; rest is Cons && cdr(rest as Cons) is Cons; rest = cdr(rest as Cons))
		{
			leval(car(rest as Cons), env);
		}
		return leval(car(rest as ICons), env);
	}

	public object expand_quasiquote (object expr, ICons env)
	{
		if (! (expr is Cons)) { return expr; }
		var sym = car(expr as Cons);
		if (sym is Symb && (sym as Symb).name == "unquote")
		{
			return leval(nth(expr as Cons, 1), env);
		}
		ICons eexpr = nil;
		for (object rest = expr; rest is Cons; rest = cdr(rest as Cons))
		{
			var is_splicing = false;
			var e = car(rest as Cons);
			if (e is Cons)
			{
				var esym = car(e as Cons);
				if (esym is Symb && (esym as Symb).name == "splicing")
				{
					is_splicing = true;
					for (var sexpr
							= leval(car(cdr(car(rest as Cons) as ICons) as ICons)
								, env); sexpr is Cons; sexpr = cdr(sexpr as Cons))
					{
						eexpr = cons(car(sexpr as Cons), eexpr);
					}
				}
			}
			if (! is_splicing)
			{
				eexpr = cons(expand_quasiquote(car(rest as Cons), env), eexpr);
			}
		}
		return nreverse(eexpr);
	}

	static public ICons l (params object[] args)
	{
		return args.Reverse().Aggregate(nil
				, (ICons acc, object e) => { return cons(e, acc); });
	}

	private ICons mapeval (ICons args, ICons env)
	{
		ICons eargs = nil;
		for (object rest = args; rest is Cons; rest = cdr(rest as Cons))
		{
			eargs = cons(leval(car(rest as Cons), env), eargs);
		}
		return nreverse(eargs);
	}

	static private object findidx_eq (object o, ICons coll)
	{
		var idx = 0L;
		for (object rest = coll; rest is Cons; rest = cdr(rest as Cons))
		{
			if (ReferenceEquals(o, car(rest as Cons))) { return idx; }
			++idx;
		}
		return nil;
	}


	static private ICons growth (ICons tree, ref string tok, ref ICons rmacs)
	{
		if (tok.Length == 0) { return tree; }

		object o;
		long inum;
		double fnum;

		if ("nil" == tok || "NIL" == tok) { o = nil; }
		else if (Int64.TryParse(tok, out inum)) { o = inum; }
		else if (Double.TryParse(tok, out fnum)) { o = fnum; }
		else { o = Symb.intern(tok); }

		var res = cons(wrap_readmacros(o, rmacs), tree);

		tok = "";
		rmacs = nil;

		return res;
	}

	static private int find_co_paren (string code)
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

	static private int find_co_bracket (string code)
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

	static private dynamic take_string (string code)
	{
		var eflg = false;
		var strn = "";
		for (var idx = 0; idx < code.Length; ++idx)
		{
			var c = code[idx];
			if (eflg)
			{
				char esc;
				if (escape_char_table.TryGetValue(c, out esc)) { c = esc; }
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

	static private object[] cons2vect (ICons c)
	{
		var arr = new List<object>();
		for (object rest = c; rest is Cons; rest = cdr(rest as Cons))
		{
			arr.Add(car(rest as Cons));
		}
		return arr.ToArray();
	}

	static private ICons vect2cons (object[] l)
	{
		return l.Reverse().Aggregate(nil
				, (ICons acc, object e) => { return cons(e, acc); });
	}

	static private ICons bind_tree (object treea, object treeb)
	{
		if (treea is Nil) { return nil; }
		if (! (treea is Cons)) { return l(cons(treea, treeb)); }
		if (! (treeb is ICons))
		{
			throw new Erro(ErroId.Syntax
					, string.Format("cannot bind: {0} and {1}"
						, lprint(treea), lprint(treeb)));
		}
		try
		{
			return nconc(bind_tree(car(treea as Cons), car(treeb as Cons))
					, bind_tree(cdr(treea as Cons), cdr(treeb as Cons)));
		}
		catch (Erro)
		{
			throw new Erro(ErroId.Syntax
					, string.Format("cannot bind: {0} and {1}"
						, lprint(treea), lprint(treeb)));
		}
	}

	static public ICons assoc (ICons alist, object key)
	{
		for (object rest = alist; rest is Cons; rest = cdr(rest as Cons))
		{
			var e = car(rest as Cons);
			if (e is Cons && (! (equal(car(e as Cons), key) is Nil))) { return e as Cons; }
		}
		return nil;
	}
	
	static private object seekenv (ICons env, Symb sym)
	{
		for (object rest = env; rest is Cons; rest = cdr(rest as Cons))
		{
			var res = assoc(car(rest as Cons) as ICons, sym);
			if (res is Cons) { return cdr(res); }
		}
		throw new Erro(ErroId.Symbol
				, string.Format("{0} is not defined.", lprint(sym)));
	}

	static private object wrap_readmacros (object o, ICons rmacs)
	{
		var wraped = o;
		for (object rest = rmacs; rest is Cons; rest = cdr(rest as Cons))
		{
			wraped = l(car(rest as Cons), wraped);
		}
		return wraped;
	}

	static public ICons lread (string code)
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
				tree = cons(wrap_readmacros(lread(code.Substring(idx + 1, co)), rmacs)
						, tree);
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
				ICons invec = lread(code.Substring(idx + 1, co));
				tree = (rmacs is Nil)
					? cons(cons(Symb.intern("vect"), invec), tree)
					: cons(l(Symb.intern("to-vect"), wrap_readmacros(invec, rmacs)), tree);
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
				rmacs = cons(Symb.intern("quote"), rmacs);
			}
			else if ('`' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(Symb.intern("quasiquote"), rmacs);
			}
			else if (',' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(Symb.intern("unquote"), rmacs);
			}
			else if ('@' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(Symb.intern("splicing"), rmacs);
			}
			else if ('^' == c)
			{
				tree = growth(tree, ref tok, ref rmacs);
				rmacs = cons(Symb.intern("tee"), rmacs);
			}
			else if ('.' == c)
			{
				if (tok.Length == 0)
				{
					return nconc(nreverse(cdr(tree) as ICons)
							, cons(car(tree), car(lread(code.Substring(idx + 1)))));
				}
				tok += ".";
			}
			else { tok += c; }
		}
		tree = growth(tree, ref tok, ref rmacs);
		return nreverse(tree);
	}

	static public ICons lreadtop (string code)
	{
		return cons(Symb.intern("do"), lread(code));
	}

	public object leval (object expr_, ICons env_)
	{
		var expr = expr_;
		var env = env_;
		while (true)
		{
			if (expr is Cons)
			{
				var args = cdr(expr as Cons) as ICons;
				var proc = leval(car(expr as Cons), env);
				if (proc is LFunc)
				{
					expr = (proc as LFunc).body;
					env = cons(bind_tree((proc as LFunc).args, mapeval(args, env))
							, (proc as LFunc).env);
				}
				else if (proc is Spfm)
				{
					if ("if" == (proc as Spfm).name)
					{
						expr = nth(args, (leval(car(args), env) is Nil) ? 2L : 1L);
					}
					else if ("do" == (proc as Spfm).name)
					{
						ICons rest = args;
						for (; cdr(rest) is Cons; rest = cdr(rest) as Cons)
						{
							leval(car(rest), env);
						}
						expr = car(rest);
					}
					else if ("!" == (proc as Spfm).name)
					{
						expr = lapply(leval(car(args), env), cdr(args) as ICons);
					}
					else { return (proc as Spfm).proc(env, args); }
				}
				else if (proc is Subr) { return lapply(proc, mapeval(args, env)); }
				else
				{
					throw new Erro(ErroId.UnCallable
							, string.Format("{0} is not callable.", lprint(proc)));
				}
			}
			else if (expr is Symb) { return seekenv(env, expr as Symb); }
			else { return expr; }
		}
	}

	public object lapply (object proc, ICons args)
	{
		if (proc is LFunc)
		{
			return leval((proc as LFunc).body
					, cons(bind_tree((proc as LFunc).args, args)
						, (proc as LFunc).env));
		}
		if (proc is Subr) { return (proc as Subr).proc(args); }
		// TODO c# invoke()
		if (proc is Delegate) { return (proc as Delegate).DynamicInvoke(cons2vect(args)); }
		if (proc is MethodInfo) { return (proc as MethodInfo).Invoke(null, cons2vect(args)); }
		throw new Erro(ErroId.UnCallable
				, string.Format("{0} is not callable.", lprint(proc)));
	}

	static public object lthrow (long eid, string estr)
	{
		throw new Erro(eid, estr);
		return nil;
	}

	public object lcatch (ICons env, ICons args)
	{
		var excep = leval(car(args), env);
		try
		{
			return leval(nth(args, 1), env);
		}
		catch (Erro erro)
		{
			return lapply(excep, l(erro.eid, leval(erro.Message, env)));
		}
	}

	static public object lempty (object coll)
	{
		if (coll is Nil) { return t; }
		if (coll.GetType().IsArray)
		{
			return ((coll as object[]).Length < 1) ? t as object : nil as object;
		}
		if (coll is string)
		{
			return ((coll as string).Length < 1) ? t as object : nil as object;
		}
		if (coll is Queu)
		{
			return ((coll as Queu).exit is Nil) ? t as object : nil as object;
		}
		if (coll is Symb)
		{
			return ((coll as Symb).name.Length < 1) ? t as object : nil as object;
		}
		return nil;
	}

	static public Nil llprin (ICons objs)
	{
		for (object rest = objs; rest is Cons; rest = cdr(rest as Cons))
		{
			var a = car(rest as Cons);
			Console.Write((a is string) ? a : lprint(a));
		}
		return nil;
	}

	static public Nil llprint (ICons objs)
	{
		llprin(objs);
		Console.WriteLine("");
		return nil;
	}

	static public object lgetat (object vect, long idx)
	{
		if (vect.GetType().IsArray) { return (vect as object[])[Convert.ToInt32(idx)]; }
		if (vect is string) { return (vect as string)[Convert.ToInt32(idx)].ToString(); }
		if (vect is Symb) { return Symb.intern((vect as Symb).name[Convert.ToInt32(idx)].ToString()); }
		throw new Erro(ErroId.Type
				, string.Format("cannot apply getat to {0}", lprint(vect)));
	}

	static public object lsetat (object vect, long idx, object valu)
	{
		if (vect.GetType().IsArray)
		{
			(vect as object[])[idx] = valu;
			return vect;
		}
		if (vect is string)
		{
			char c;
			if (is_inum(valu)) { c = Convert.ToChar(valu); }
			else if (valu is string) { c = (valu as string)[0]; }
			else if (valu is Symb) { c = (valu as Symb).name[0]; }
			else { throw new Erro(ErroId.Type
					, string.Format("cannot setat {0} to {1}"
						, lprint(valu), lprint(vect))); }
			return (vect as string).Substring(0, Convert.ToInt32(idx))
				+ c.ToString()
				+ (vect as string).Substring(Convert.ToInt32(idx) + 1);
		}
		if (vect is Symb)
		{
			char c;
			if (is_inum(valu)) { c = Convert.ToChar(valu); }
			else if (valu is string) { c = (valu as string)[0]; }
			else if (valu is Symb) { c = (valu as Symb).name[0]; }
			else { throw new Erro(ErroId.Type
					, string.Format("cannot setat {0} to {1}"
						, lprint(valu), lprint(vect))); }
			return Symb.intern((vect as Symb).name.Substring(0, (Convert.ToInt32(idx)))
				+ c.ToString()
				+ (vect as Symb).name.Substring(Convert.ToInt32(idx) + 1));
		}
		throw new Erro(ErroId.Type, string.Format("cannot apply setat to {0}", lprint(vect)));
	}


	static public string lprint (object expr)
	{
		var dup = seek_dup(expr, nil, nil).dup;
		var s = "";
		var idx = 0L;
		for (var rest = dup; rest is Cons; rest = cdr(rest as Cons))
		{
			s += string.Format("${0} = {1}\n"
					, idx, lprint_rec(car(rest as Cons), dup, false)); 
			++idx;
		}
		s += lprint_rec(expr, dup, true);
		return s;
	}

	static private dynamic seek_dup (object expr, ICons printed, ICons dup)
	{
		if (! (findidx_eq(expr, printed) is Nil))
		{
			if (findidx_eq(expr, dup) is Nil)
			{
				return new {printed = printed, dup = cons(expr, dup)};
			}
			return new {printed = printed, dup = dup};
		}
		if (expr is Cons)
		{
			var res = seek_dup(car(expr as Cons), cons(expr, printed), dup);
			return seek_dup(cdr(expr as Cons), res.printed, res.dup);
		}
		if (expr is Queu) { return seek_dup((expr as Queu).exit, cons(expr, printed), dup); }
		if (expr.GetType().IsArray)
		{
			return (expr as object[])
				.Aggregate(new {printed = cons(expr, printed), dup = dup}
					, (dynamic acc, object elm)
					=> { return seek_dup(elm, acc.printed, acc.dup); });
		}
		if (expr is Erro) { return seek_dup((expr as Erro).Message, cons(expr, printed), dup); }
		return new {printed = printed, dup = dup};
	}

	static private string lprint_rec (object expr, ICons dup, bool rec)
	{
		var idx = findidx_eq(expr, dup);
		if (rec && ! (idx is Nil)) { return string.Format("${0}", idx); }
		if (expr is Nil) { return "NIL"; }
		if (expr is Cons) { return printcons_rec(expr as Cons, dup, true); }
		if (expr is Symb) { return (expr as Symb).name; }
		if (expr is string) { return string.Format("\"{0}\"", expr); }
		if (expr.GetType().IsArray)
		{
			var s = (expr as object[]).Select(
					(object e) => { return lprint_rec(e, dup, true); });
			return string.Format("[{0}]", string.Join(" ", s));
		}
		if (expr is Queu)
		{ return string.Format("/{0}/", lprint_rec((expr as Queu).exit, dup, true)); }
		if (expr is LFunc)
		{
			return string.Format("<Func {0} {1}>",
				   	lprint_rec((expr as LFunc).args, dup, true)
					, lprint_rec((expr as LFunc).body, dup, true));
		}
		if (expr is Spfm) { return string.Format("<Spfm {0}>", (expr as Spfm).name); }
		if (expr is Subr) { return string.Format("<Subr {0}>", (expr as Subr).name); }
		if (is_fnum(expr))
		{
			string sn = expr.ToString();
			if (Regex.IsMatch(sn, @"\.")) { return sn; }
			return string.Format("{0}.0", sn);
		}
		return expr.ToString();
	}

	static private string printcons_rec (Cons coll, ICons dup, bool rec)
	{
		var a = car(coll);
		var d = cdr(coll);
		if (d is Nil) { return string.Format("({0})", lprint_rec(a, dup, rec)); }
		if (! (d is Cons))
		{
			return string.Format("({0} . {1})"
					, lprint_rec(a, dup, rec), lprint_rec(d, dup, rec));
		}
		if (! (findidx_eq(d, dup) is Nil))
		{
			return string.Format("({0} . {1})"
					, lprint_rec(a, dup, rec), lprint_rec(d, dup, rec));
		}
		return string.Format("({0} {1}"
				, lprint_rec(a, dup, rec), lprint_rec(d, dup, rec).Substring(1));
	}
}
}

