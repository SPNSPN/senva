using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

class Program
{
	static Senva.Interpreter senva;

	private static void CHECK (string code, object succ)
	{
		Console.Write(string.Format("CHECK: \"{0}\" -> ", code));
		var s = Senva.Interpreter.lprint(senva.leval(
					Senva.Interpreter.lreadtop(code), senva.genv));
		Console.WriteLine(s);
		if (succ == null) { return; }
		if (s != (succ as string))
		{
			Console.WriteLine("Fail CHECK.");
			Environment.Exit(-1);
		}
	}

	private static void ECHECK (string code, int ceid, object succ)
	{
		Console.Write(string.Format("ECHECK: \"{0}\" -> ", code));
		try
		{
			Console.WriteLine(Senva.Interpreter.lprint(
						senva.leval(Senva.Interpreter.lreadtop(code), senva.genv)));
		}
		catch (Senva.Interpreter.Erro erro)
		{
			Console.WriteLine(string.Format("<Erro \"{0}\">", erro.Message));
			if (ceid != erro.eid)
			{
				throw new Senva.Interpreter.Erro(444, string.Format(
							"except code: {0}, but got code: {1}", ceid, erro.eid));
			}
			if ((succ as string) != erro.Message)
			{
				throw new Senva.Interpreter.Erro(444, string.Format(
							"except code: {0}, but got mess: {1}", succ, erro.Message));
			}
			return;
		}
		throw new Senva.Interpreter.Erro(4444, "not got erro!");
	}

	private static void ECHECK (string code
			, Senva.Interpreter.ErroId ceid, object succ)
	{
		ECHECK(code, (int)ceid, succ);
	}

	[STAThread]
	public static void Main (string[] args)
	{
		senva = new Senva.Interpreter();
		CHECK("", "NIL");
		CHECK("nil", "NIL");
		CHECK("()", "NIL");
		CHECK("(cons 1 2)", "(1 . 2)");
		CHECK("(car (cdr (cons 1 (cons 2 3))))", "2");
		CHECK("(car nil)", "NIL");
		CHECK("(cdr nil)", "NIL");
		CHECK("(atom 1)", "T");
		CHECK("(atom nil)", "T");
		CHECK("(atom (cons 1 2))", "NIL");
		CHECK("(eq 'a 'a)", "T");
		CHECK("(eq 'a 'b)", "NIL");
		CHECK("(eq cons cons)", "T");
		CHECK("(eq cons 'cons)", "NIL");
		CHECK("(eq (cons 1 2) (cons 1 2))", "NIL");
		CHECK("(eq 'a (car (cons 'a 'd)))", "T");
		CHECK("(equal (cons 1 2) (cons 1 2))", "T");
		CHECK("(equal (cons 3 2) (cons 1 2))", "NIL");
		CHECK("(equal 1339 1339)", "T");
		CHECK("(equal 3 1)", "NIL");
		CHECK("(equal 1339 (cons nil 44))", "NIL");
		CHECK("(equal (cons nil 44) nil)", "NIL");
		CHECK("(list 5 4 3 2 1)", "(5 4 3 2 1)");
		CHECK("(rplaca (cons nil 44) 34)", "(34 . 44)");
		CHECK("(rplacd (cons 44 55) (cons 3 nil))", "(44 3)");
		CHECK("(last (list 1 2 3 4))", "(4)");
		CHECK("(last nil)", "NIL");
		CHECK("(last (queu 1 2 3))", "(3)");
		CHECK("(last 'abc)", "c");
		CHECK("(last \"abc\")", "\"c\"");
		CHECK("(last [1 2 3])", "[3]");
		CHECK("(nconc (list 1 2 3) (list 4 5))", "(1 2 3 4 5)");
		CHECK("(nreverse (list 1 2 3 4))", "(4 3 2 1)");
		CHECK("(/ (+ 71 55) (- (* 2 3) 3))", "42");
		CHECK("(/ 3 2)", "1.5");
		CHECK("(/ 3 2.0)", "1.5");
		CHECK("(% 9 2)", "1");
		CHECK("(+ 1 2 (- 10 3 4) 4 (/ 30 2 5) (* 2 2 2))", "21");
		CHECK("(< 1 2 4)", "T");
		CHECK("(< 1 2 1)", "NIL");
		CHECK("(> 3 2 1)", "T");
		CHECK("(> 3 2 3)", "NIL");
		CHECK("(int 2.3)", "2");
		CHECK("(int -555.3)", "-555");
		CHECK("(int 123)", "123");
		CHECK("(float 4)", "4");
		CHECK("(float -1555)", "-1555");
		CHECK("(float -15.356)", "-15.356");
		CHECK("(if nil 40 (if t 42 41))", "42");
		CHECK("(if 0 1 2)", "1");
		CHECK("(if \"\" 1 2)", "1");
		CHECK("(if () 1 2)", "2");
		CHECK("(if [] 1 2)", "1");
		CHECK("(quote sym)", "sym");
		CHECK("(quote (1 a 2 b))", "(1 a 2 b)");
		CHECK("(lambda (n) (+ n 1))", "<Func (n) (+ n 1)>");
		CHECK("((lambda (n) (+ n 1)) 3)", "4");
		CHECK("(! (lambda (a op b) (list op a b)) 1 + 2)", "3");
		CHECK("(define foo 42) foo", "42");
		CHECK("(define foo 42)", "foo");
		CHECK("(define bar 32) (setq bar 333) bar", "333");
		CHECK("(define bar 32) (setq bar 333)", "333");
		CHECK("(((lambda (fib) (do (setq fib (lambda (n) (if (< n 2) 1 (+ (fib (- n 1)) (fib (- n 2)))))) fib)) nil) 10)", "89");
		CHECK("(((lambda (fib) (do (setq fib (lambda (n p1 p2) (if (< n 2) p1 (fib (- n 1) (+ p1 p2) p1)))) fib)) nil) 45 1 1)", "1836311903");
		CHECK("(define hello \")[e\\\\\\\\o\\\" Wor)d;\") hello", "\")[e\\\\o\" Wor)d;\"");
		CHECK("(vect 1 (+ 1 1) 3)", "[1 2 3]");
		CHECK("[]", "[]");
		CHECK("[\"abc\" 42 (+ 1 1) () [1 2] 'non]", "[\"abc\" 42 2 NIL [1 2] non]");
		CHECK("(queu)", "/NIL/");
		CHECK("(queu 1 2 3)", "/(1 2 3)/");
		CHECK("(pushqueu (pushqueu (pushqueu (queu) 5) 1) 0)", "/(5 1 0)/");
		CHECK("(popqueu (pushqueu (pushqueu (pushqueu (queu) 5) 1) 0))", "5");
		CHECK("(concqueu (queu 2 5 8) (queu 3 6 9))", "/(2 5 8 3 6 9)/");
		CHECK("(concqueu (queu 1 4 7) (queu))", "/(1 4 7)/");
		CHECK("(concqueu (queu) (queu 42))", "/(42)/");
		CHECK("(to-list \"hello\")", "(104 101 108 108 111)");
		CHECK("(to-list 'hello)", "(104 101 108 108 111)");
		CHECK("(to-list [1 2 3 4])", "(1 2 3 4)");
		CHECK("(define q (queu)) (pushqueu q 1) (pushqueu q 2) (pushqueu q 3) (pushqueu q 4) (to-list q)", "(1 2 3 4)");
		CHECK("(to-list nil)", "NIL");
		CHECK("(to-vect '(1 2 3 4))", "[1 2 3 4]");
		CHECK("(to-vect \"hello\")", "[104 101 108 108 111]");
		CHECK("(to-vect 'hello)", "[104 101 108 108 111]");
		CHECK("(to-vect (pushqueu (pushqueu (pushqueu (queu) 5) 1) 0))", "[5 1 0]");
		CHECK("(to-vect nil)", "[]");
		CHECK("(to-queu (list 1 2 3 4))", "/(1 2 3 4)/");
		CHECK("(to-queu [1 2 3 4])", "/(1 2 3 4)/");
		CHECK("(to-queu \"hello\")", "/(104 101 108 108 111)/");
		CHECK("(to-queu 'hello)", "/(104 101 108 108 111)/");
		CHECK("(to-queu nil)", "/NIL/");
		CHECK("(symbol '(104 101 108 108 111))", "hello");
		CHECK("(symbol \"abcd\")", "abcd");
		CHECK("(symbol [104 101 108 108 111])", "hello");
		CHECK("(symbol (pushqueu (pushqueu (pushqueu (queu) 97) 98) 99))", "abc");
		CHECK("(symbol nil)", "");
		CHECK("(sprint  \"a\" 1 (cons 1 2))", "\"a1(1 . 2)\"");
		CHECK("`(1 2 ,3 ,(+ 2 2) @(if (> 3 1) '(5 6) nil) @(cons 7 `(8 ,(* 3 3))) 10)"
				, "(1 2 3 4 5 6 7 8 9 10)");
		CHECK("'(1 2 3 . 4)", "(1 2 3 . 4)");
		CHECK("`',(car '(a . d))", "(quote a)");
		CHECK("((lambda (head . rest) rest) 1 2 3 4)", "(2 3 4)");
		CHECK("((lambda all all) 1 2 3 4)", "(1 2 3 4)");
		CHECK("((lambda ((pa (pb pc) pd)) pc) (list 1 (list 2 3) 4))", "3");
		ECHECK("(throw 1 \"an error occured!\")", 1, "an error occured!");
		ECHECK("(do 1 (throw 2 \"an error occured!\") 3)", 2, "an error occured!");
		ECHECK("(if (throw 3 \"an error occured!\") 3 4)", 3, "an error occured!");
		CHECK("(if nil (throw 3 \"an error occured!\") 'safe)", "safe");
		ECHECK("(define sym (throw 4 \"an error occured!\"))", 4, "an error occured!");
		CHECK("(and 1 'a '(4))", "(4)");
		CHECK("(and 1 nil '(4))", "NIL");
		CHECK("(and 1 nil (throw 1 \"E\"))", "NIL");
		CHECK("(and)", "T");
		CHECK("(or nil () '())", "NIL");
		CHECK("(or nil 'a '(4))", "a");
		CHECK("(or nil 1 (throw 1 \"E\"))", "1");
		CHECK("(or)", "NIL");
		ECHECK("ffoo", Senva.Interpreter.ErroId.Symbol, "ffoo is not defined.");
		ECHECK("(", Senva.Interpreter.ErroId.Syntax, "not found close parenthesis.");
		ECHECK(")", Senva.Interpreter.ErroId.Syntax, "found excess close parenthesis.");
		ECHECK("(to-list 3)", Senva.Interpreter.ErroId.Type, "cannot cast 3 to ConsT.");
		ECHECK("(to-vect 33)", Senva.Interpreter.ErroId.Type, "cannot cast 33 to VectT.");
		ECHECK("(symbol 33)", Senva.Interpreter.ErroId.Type, "cannot cast 33 to SymbT.");
		ECHECK("(to-queu 33)", Senva.Interpreter.ErroId.Type, "cannot cast 33 to QueuT.");
		ECHECK("(setq ffoo 33)", Senva.Interpreter.ErroId.Symbol, "ffoo is not defined.");
		ECHECK("\"", Senva.Interpreter.ErroId.Syntax, "not found close double quote.");
		ECHECK("((lambda ((a . b)) a) 1)", Senva.Interpreter.ErroId.Syntax, "cannot bind: ((a . b)) and (1)");
		ECHECK("(3 1)", Senva.Interpreter.ErroId.UnCallable, "3 is not callable.");
		ECHECK("(+ 3 'a)", Senva.Interpreter.ErroId.Type, "cannot add (3 a)");
		ECHECK("(- () 2)", Senva.Interpreter.ErroId.Type, "cannot sub (NIL 2)");
		ECHECK("(* [] 2)", Senva.Interpreter.ErroId.Type, "cannot mul ([] 2)");
		ECHECK("(/ 3 \"a\")", Senva.Interpreter.ErroId.Type, "cannot div (3 \"a\")");
		ECHECK("(% 3 nil)", Senva.Interpreter.ErroId.Type, "cannot mod (3 NIL)");
		ECHECK("(int 'sym)", Senva.Interpreter.ErroId.Type, "cannot cast sym to InumT.");
		ECHECK("(float \"str\")", Senva.Interpreter.ErroId.Type, "cannot cast \"str\" to FnumT.");
		CHECK("(empty ())", "T");
		CHECK("(empty [])", "T");
		CHECK("(empty (queu))", "T");
		CHECK("(empty \"\")", "T");
		CHECK("(empty '(1))", "NIL");
		CHECK("(empty [1])", "NIL");
		CHECK("(empty (pushqueu (queu) 1))", "NIL");
		CHECK("(empty \"1\")", "NIL");
		CHECK("(catch (lambda (id mess) (list 'trap id mess)) (do (print 1) (print 2) (throw 55 \"fail!\") (print 3) 'end))", "(trap 55 \"fail!\")");
		CHECK("(type nil)", "<nil>");
		CHECK("(type 1)", "<inum>");
		CHECK("(type (cons 1 2))", "<cons>");
		CHECK("(type (queu))", "<queu>");
		CHECK("(type [1 2])", "<vect>");
		CHECK("(type 'foo)", "<symb>");
		CHECK("(type \"bar\")", "<strn>");
		CHECK("(type type)", "<subr>");
		CHECK("(type if)", "<spfm>");
		CHECK("(type (lambda (baz) baz))", "<func>");
		CHECK("(type 1.2)", "<fnum>");
		CHECK("(apply + '(1 4 6 9))", "20");
		CHECK("(apply (lambda (a b c d) (list d b a c)) '(1 4 6 9))", "(9 4 1 6)");
		CHECK("(define v [1 2 3]) (getat v 2)", "3");
		CHECK("(define v [1 2 3]) (setat v 2 44) v", "[1 2 44]");
		CHECK("(eq (getat [1 nil 3] 1) nil)", "T");
		CHECK("(if (getat [1 nil 3] 1) 1 2)", "2");
		CHECK("(getat \"abcd\" 2)", "\"c\"");
		CHECK("(getat 'abcd 2)", "c");
		CHECK("(setat [1 2 3] 1 \"a\")", "[1 \"a\" 3]");
		CHECK("(setat [1 2 3] 1 'a)", "[1 a 3]");
		CHECK("(setat [1 2 3] 1 '(1 . 2))", "[1 (1 . 2) 3]");
		CHECK("(setat \"ABC\" 2 99)", "\"ABc\"");
		CHECK("(setat \"ABC\" 2 \"d\")", "\"ABd\"");
		CHECK("(setat \"ABC\" 2 'e)", "\"ABe\"");
		CHECK("(setat 'ABC 0 102)", "fBC");
		CHECK("(setat 'ABC 0 \"g\")", "gBC");
		CHECK("(setat 'ABC 0 'h)", "hBC");
		ECHECK("(getat 5 2)", Senva.Interpreter.ErroId.Type, "cannot apply getat to 5");
		ECHECK("(setat (list 1 2 3) 2 44)", Senva.Interpreter.ErroId.Type, "cannot apply setat to (1 2 3)");
		ECHECK("(setat \"ABC\" 1 '(1 . 2))", Senva.Interpreter.ErroId.Type, "cannot setat (1 . 2) to \"ABC\"");
		CHECK("(to-list \"a\\nb\\tc\\0\")", "(97 10 98 9 99 0)");
		CHECK("`[1 2 ,3 ,(+ 2 2) @(if (> 3 1) '(5 6) nil) @(cons 7 `(8 ,(* 3 3))) 10]"
				, "[1 2 3 4 5 6 7 8 9 10]");
		CHECK("(load \"senva/matrix.snv\") (matrix::determinant '((3 1 1 2 1) (5 1 3 4 1) (2 0 1 0 3) (1 3 2 1 1) (2 1 5 10 1)))", "-292");
		ECHECK("(load \"not/exist/path.ext\")"
				, Senva.Interpreter.ErroId.FileNotFound, "not found file: \"not/exist/path.ext\"");
		ECHECK("(load 33)", Senva.Interpreter.ErroId.Type, "cannot apply load to 33");
		CHECK("((lambda (c) (list (list c c) (cons c c))) (list 1 2))"
				, "$0 = (1 2)\n(($0 $0) ($0 . $0))");
		CHECK("((lambda (c v) [v c [[v c] (list v c)] (list (list c v) [c v])]) (list 1 2) [1 2])"
				, "$0 = (1 2)\n$1 = [1 2]\n[$1 $0 [[$1 $0] ($1 $0)] (($0 $1) [$0 $1])]");
		CHECK("((lambda (rpc) (rplacd rpc rpc)) (list 1 2))", "$0 = (1 . $0)\n$0");
		CHECK("((lambda (rpv) (setat rpv 1 rpv)) [1 2])", "$0 = [1 $0]\n$0");
		CHECK("(processor)", "cs");
		CHECK("(environment)", Senva.Interpreter.lprint(senva.genv));
		CHECK("(load \"senva/test.snv\")", "NIL");
	}
}
