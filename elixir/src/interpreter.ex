defmodule Interpreter do
	@timeout_sec 5

	def proc_cons(a, d) do
		receive do
			{from, :car} -> send from, {self(), a}
							proc_cons a, d
			{from, :cdr} -> send from, {self(), d}
							proc_cons a, d
			{from, :rplaca, ra} -> proc_cons ra, d
			{from, :rplacd, rd} -> proc_cons a, rd
			{from, :typ} -> send from, {self(), :cons}
							proc_cons a, d
		end
	end

	def get_exit_from_queu(q) do
		send q, {self(), :getexit}
		receive do
			{^q, qexi} -> qexi
		after
			@timeout_sec -> raise {:timeout, "cannot receive getexi, from #{q}"}
		end
	end

	def proc_queu(n, n) when is_nil(n) do
		receive do
			{from, :push, e} -> c = cons(e, nil)
								proc_queu(c, c)
			{from, :pop} -> send from, nil
							proc_queu(nil, nil)
			{from, :concat, q} -> proc_queu()
			{from, :getexit} -> nil
								proc_queu(nil, nil)
			{from, :typ} -> :queu
							proc_queu(nil, nil)
		end
	end
	def proc_queu(exi, exi) do
		receive do
			{from, :push, e} -> c = cons(e, nil)
								rplacd(exi, c)
								proc_queu(c, exi)
			{from, :pop} -> a = car(exi)
							send from, a
							proc_queu(nil, nil)
			{from, :concat, q} -> rplacd(exi, getexit_from_queu(q))
								  proc_queu(ent, exi)
			{from, :getexit} -> exi
								proc_queu(exi, exi)
			{from, :typ} -> :queu
							proc_queu(exi, exi)
		end
	end
	def proc_queu(ent, exi) do
		receive do
			{from, :push, e} -> c = cons(e, nil)
								rplacd(ent, c)
								proc_queu(c, exi)
			{from, :pop} -> a = car(exi)
							send from, a
							proc_queu(ent, cdr(exi))
			{from, :concat, q} -> rplacd(ent, getexit_from_queu(q))
								  proc_queu(ent, exi)
			{from, :getexit} -> exi
								proc_queu(ent, exi)
			{from, :typ} -> :queu
							proc_queu(ent, exi)
		end
	end

	def cons(a, d) do
		spawn(Interpreter, :proc_cons, [a, d])
	end

	def car(n) when is_nil(n) do
		n
	end
	def car(pid) when is_pid(pid) do
		send pid, {self(), :car}
		receive do
			{^pid, a} -> a
		after
			@timeout_sec -> raise {:timeout, "cannot receive car from #{pid}"}
		end
	end

	def cdr(n) when is_nil(n) do
		n
	end
	def cdr(pid) when is_pid(pid) do
		send pid, {self(), :cdr}
		receive do
			{^pid, d} -> d
		after
			@timeout_sec -> raise {:timeout, "cannot receive cdr from #{pid}"}
		end
	end

	def latom(pid) do
		if ltype(pid) == :cons do
			nil
		else
			:t
		end
	end

	def ltype(n) when is_nil(n) do
		:nil
	end
	def ltype(symb) when is_atom(symb) do
		:symb
	end
	def ltype(strn) when is_string(strn) do
		:strn
	end
	def ltype(inum) when is_integer(inum) do
		:inum
	end
	def ltype(fnum) when is_float(fnum) do
		:fnum
	end
	def ltype(vect) when is_list(vect) do
		:vect
	end
	def ltype(subr) when is_function(subr) do
		:subr
	end
	def ltype(pid) when is_pid(pid) do
		send pid, {self(), :typ}
		receive do
			{^pid, typ} -> typ
		after
			@timeout_sec -> raise {:timeout, "cannot receive type from #{pid}"}
		end
	end

	def eq(e, e) do
		:t
	end
	def eq(_, _) do
		nil
	end

	def lread(code) do
	# TODO	
	end

	def leval(expr, env) do
	# TODO	
	end

	def lprint(expr) do
	# TODO	
	end
end
