using System;

namespace Senva
{
public interface ICons
{
	object car ();
	object cdr ();
}

class Nil : ICons
{
	public Nil ()
	{
	}

	public object car ()
	{
		return this;
	}

	public object cdr ()
	{
		return this;
	}
}

class Symb
{
	public string name;
	public Symb (string name_)
	{
		this.name = name_;
	}
}

class Cons : ICons
{
	private object a;
	private object d;

	public Cons (object a_, object d_)
	{
		this.a = a_;
		this.d = d_;
	}

	public object car ()
	{
		return this.a;
	}

	public object cdr ()
	{
		return this.d;
	}
}

class Interpreter
{
	public Nil nil;
	public Cons genv;

	public Interpreter ()
	{
		this.nil = new Nil();
		this.genv = new Cons(this.nil, this.nil);
	}

	public ICons read (string code)
	{
		return nil; // TODO
	}

	public ICons eval (ICons expr)
	{
		return expr; // TODO
	}

	public string print (ICons expr)
	{
		return "nil"; // TODO
	}
}
}

