let typeid = {}
let typeid.nil = 6
let typeid.symb = 7
let typeid.cons = 8
let typeid.queu = 9
let typeid.func = 10
let typeid.spfm = 11
let typeid.erro = 12

let erroid = {}
let erroid.FullMemory = 0
let erroid.OutOfEnvironment = 2
let erroid.Type = 3
let erroid.Symbol = 4
let erroid.Syntax = 5
let erroid.UnCallable = 6
let erroid.ArgsUnmatch = 7
let erroid.UnEvaluatable = 8
let erroid.FileNotFound = 9

function! Nil () abort
	let self = {}
	let self.type = g:typeid.nil
	let self.a = self
	let self.d = self
	return self
endfunction

function! Symb (name) abort
	let self = {}
	let self.type = g:typeid.symb
	let self.name = a:name
	return self
endfunction

let symb_identifiers = {}
function! Intern (name) abort
	if has_key(g:symb_identifiers, a:name)
		return g:symb_identifiers[a:name]
	endif
	let newsym = Symb(a:name)
	let g:symb_identifiers[a:name] = newsym
	return newsym
endfunction

function! Cons (a, d) abort
	let self = {}
	let self.type = g:typeid.cons
	let self.a = a:a
	let self.d = a:d
	return self
endfunction

function! Queu () abort
	let self = {}
	let self.type = g:typeid.queu
	let self.entr = g:nil
	let self.exit = self.entr
	return self
	" TODO
endfunction

function! Func () abort
" TODO
endfunction

function! Spfm () abort
" TODO
endfunction

let nil = Nil()
let t = Intern("T")
let genv = Cons(g:nil, g:nil)

let typetable = {}
let typetable[type(0)] = Intern("<Inum>")
let typetable[type("")] = Intern("<Strn>")
let typetable[type(function("tr"))] = Intern("<Subr>")
let typetable[type([])] = Intern("<Vect>")
let typetable[type({})] = Intern("<vim dict>")
let typetable[type(0.0)] = Intern("<Fnum>")
let typetable[typeid.nil] = Intern("<Nil>")
let typetable[typeid.symb] = Intern("<Symb>")
let typetable[typeid.cons] = Intern("<Cons>")
let typetable[typeid.queu] = Intern("<Queu>")
let typetable[typeid.func] = Intern("<Func>")
let typetable[typeid.spfm] = Intern("<Spfm>")
let typetable[typeid.erro] = Intern("<Erro>")

function! Ltype (o) abort
	let typ = type(a:o)
	if type({}) == typ
		return g:typetable[get(a:o, "type", typ)]
	endif
	return g:typetable[typ]
endfunction


function! Car (c) abort
	return a:c.a
endfunction

function! Cdr (c) abort
	return a:c.d
endfunction

function! Atom (o) abort
	return (Ltype(a:o).name != "<Cons>") ? g:t : g:nil
endfunction

function! Eq (a, b) abort
	return (a:a is a:b) ? g:t : g:nil
endfunction

function! Equal (a, b) abort
	let cond = 0
	let typ = Ltype(a).name
	if a is b
		let cond = 1
	elseif typ == Ltype(b).name
		if "<Symb>" == typ
			let cond = (a.name == b.name)
		elseif "<Cons>" == typ
			let cond = ! (Equal(a.car, b.car) is g:nil || Equal(a.cdr, b.cdr) is g:nil)
		elseif "<Queu>" == typ
			let cond = Equal(a.exit, b.exit)
		elseif "<Func>" == typ
			let cond = ! (Equal(a.args, b.args) is g:nil || Equal(a.body, b.body) is g:nil || Equal(a.env, b.env) is g:nil)
		elseif "<Spfm>" == typ
			let cond = ! (Equal(a.proc, b.proc) is g:nil || Equal(a.name, b.name) is g:nil)
		elseif "<Vect>" == typ
			if len(a) == len(b)
				let cond = 1
				for idx in len(a)
					if Equal(a[idx], b[idx]) is g:nil
						let cond = 0
						break
					endif
				endfor
			endif
		else
			let cond = (a == b)
		endif
	endif
	return cond ? g:t : g:nil
endfunction


function! FindidxEq (val, coll)
	let rest = a:coll
	let idx = 0
	while Atom(rest) is g:nil
		if ! (Eq(a:val, Car(rest)) is g:nil)
			return idx
		endif
		let rest = Cdr(rest)
		let idx += 1
	endwhile
	return g:nil
endfunction

function! Rplaca (c, v) abort
	let a:c.a = a:v
endfunction

function! Rplacd (c, v) abort
	let a:c.d = a:v
endfunction

function! Last (o) abort
	let typ = Ltype(a:o).name
	if "<Cons>" == typ
		let rest = a:o
		while Atom(Cdr(rest)) is g:nil
			let rest = Cdr(rest)
		endwhile
		return rest
	elseif "<Nil>" == typ
		return g:nil
	elseif "<Queu>" == typ
		return a:o.entr
	elseif "<Symb>" == typ
		return Intern(a:o.name[len(a:o.name) - 1])
	elseif "<Strn>" == typ
		return a:o[len(a:o) - 1]
	elseif "<Vect>" == typ
		return [a:o[len(a:o) - 1]]
	endif
	call Lthrow(erroid.type, "cannot apply last to " . Lprint(a:o))
endfunction

function! Nconc (colla, collb) abort
	if ! (Atom(colla) is g:nil)
		return a:collb
	endif
	let las = Last(a:colla)
	call Rplaca(las, a:collb)
	return a:colla
endfunction

function! Nreverse (coll) abort
	let rev = g:nil
	let rest = a:coll
	while Atom(rest) is g:nil
		let tmp = Cdr(rest)
		call Rplacd(rest, rev)
		let rev = rest
		let rest = tmp
	endwhile
	return rev
endfunction


function! List (...) abort
	let lis = g:nil
	for idx in range(a:0)
		let lis = Cons(a:000[a:0 - idx - 1], lis)
	endfor
	return lis
endfunction

function! Mapeval (args, env)
	let eargs = g:nil
	let rest = a:args
	while Atom(rest) is g:nil
		let eargs = Cons(Leval(Car(rest), a:env), eargs)
		let rest = Cdr(rest)
	endwhile
	return Nreverse(eargs)
endfunction




function! Inumable (str)
	return match(a:str, "^[+-]\\?\\d\\+$") != -1
endfunction

function! Fnumable (str)
	return match(a:str, "^[+-]\\?[0-9.]\\+$") != -1
endfunction

function! Growth (tree, buff) abort
	let buf = a:buff[0]
	let rmacs = a:buff[1]
	if buf
		let a:buff[0] = ""
		let a:buff[1] = g:nil
		if "nil" == buf || "NIL" == buf
			return Cons(WrapReadmacros(g:nil, rmacs), a:tree)
		endif
		if Inumable(buf)
			return Cons(WrapReadmacros(str2nr(buf, 10), rmacs), a:tree)
		endif
		if Fnumable(buf)
			return Cons(WrapReadmacros(str2float(buf), rmacs), a:tree)
		endif
		return Cons(WrapReadmacros(Intern(buf), rmacs), a:tree)
	endif
	return a:tree
endfunction

function! FindCoParen (code)
	let sflg = 0
	let layer = 1
	let idx = 0
	while idx < len(a:code)
		let c = code[idx]
		if ! sflg && "(" == c
			let layer += 1
		elseif ! sflg && ")" == c
			let layer -= 1
		elseif "\\" == c
			let idx += 1
		elseif '"' == c
			let sflg = ! sflg
		endif
		if layer < 1
			return idx
		endif
		let idx += 1
	endwhile
	call Lthrow(g:erroid.syntax, "not found close parenthesis.")
endfunction

function! FindCoBracket (code)
	let sflg = 0
	let layer = 1
	let idx = 0
	while idx < len(a:code)
		let c = code[idx]
		if ! sflg && "[" == c
			let layer += 1
		elseif ! sflg && "]" == c
			let layer -= 1
		elseif "\\" == c
			let idx += 1
		elseif '"' == c
			let sflg = ! sflg
		endif
		if layer < 1
			return idx
		endif
		let idx += 1
	endwhile
	call Lthrow(g:erroid.syntax, "not found close brackets.")
endfunction

let escape_char_table = {
\	"a": "\a"
\	, "b": "\b"
\	, "f": "\f"
\	, "n": "\n"
\	, "r": "\r"
\	, "t": "\t"
\	, "v": "\v"
\	, "0": "\0"
\}

function! TakeString (code)
	let strn = ""
	let idx = 0
	while idx < len(a:code)
		let c = a:code[idx]
		if '"' == c
			return [strn, idx + 1]
		endif
		if "\\" == c
			let idx += 1
			let c = a:code[idx]
			if has_key(g:escape_char_table, c)
				let c = g:escape_char_table[c]
			endif
		endif
		let strn += c

		let idx += 1
	endwhile
	call Lthrow(erroid.syntax, "not found close double quote.")
endfunction

function! Cons2Vect (c) abort
	let  arr = []
	let rest = a:c
	while Atom(rest) is g:nil
		let arr = add(arr, Car(rest))
		let rest = Cdr(rest)
	endwhile
	return arr
endfunction



function! BindTree (treea, treeb) abort
	if a:treea is g:nil
		return g:nil
	endif
	if ! (Atom(treea) is g:nil)
		return List(Cons(treea, treeb))
	endif
	if ! (Atom(treeb) is g:nil || treeb is g:nil)
		Lthrow(erroid.syntax, "cannot bind: " . Lprint(treea) . " and " . Lprint(treeb))
	endif
	try
		return Nconc(BindTree(Car(treea), Car(treeb)), BindTree(Cdr(treea), Cdr(treeb)))
	catch
		Lthrow(erroid.syntax, "cannot bind: " . Lprint(treea) . " and " . Lprint(treeb))
	endtry
endfunction


function! Assoc (alist, key)
	let rest = a:alist
	while Atom(rest) is g:nil
		let e = Car(rest)
		if Equal(Car(e), a:key)
			return e
		endif
		let rest = Cdr(rest)
	endwhile
	return g:nil
endfunction


function! Seekenv (env, sym) abort
	let rest = a:env
	while Atom(rest) is g:nil
		let val = Assoc(Car(rest), a:sym)
		if ! (val is g:nil)
			return Cdr(val)
		endif
		let rest = Cdr(rest)
	endwhile
	Lthrow(erroid.symbol, Lprint(sym) . " is not defined.")
endfunction

function! WrapReadmacros (o, rmacs) abort
	let wraped = a:o
	let rest = a:rmacs
	while Atom(rest) is g:nil
		let warped = List(Car(rest), wraped)
		let rest = Cdr(rest)
	endwhile
	return wraped
endfunction

function! Lread (code) abort
	let tree = g:nil
	let buff = ["", g:nil]
	for idx in range(len(a:code))
		let c = a:code[idx]
		if "(" == c
			let co = FindCoParen(strpart(a:code, idx + 1))
			let tree = Growth(tree, buff)
			let tree = Cons(WrapReadmacros(Lread(strpart(a:code, idx + 1, co)), buff[1]), tree)
			let buff = ["", g:nil]
			let idx += co + 1
		elseif ")" == c
			call Lthrow(g:erroid.Syntax, "found excess close parenthesis.")
		elseif "[" == c
			let co = FindCoBracket(strpart(a:code, idx + 1))
			let tree = Growth(tree, buff)
			let invec = Lread(strpart(a:code, idx + 1, co))
			let tree = Cons(buff[1] ? l(Intern("to-vect"), WrapReadmacros(invec, buff[1])) : Cons(Intern("vect"), invec), tree)
			let buff = ["", g:nil]
			let idx += co + 1
		elseif "]" == c
			call Lthrow(g:erroid.Syntax, "found excess close brackets.")
		elseif " " == c || "\t" == c || "\n" == c
			let tree = Growth(tree, buff)
		elseif ";" == c
			let tree = Growth(tree, buff)
			while idx < len(a:code) && "\n" != a:code[idx]
				let idx += 1
			endwhile
		elseif '"' == c
			let tree = Growth(tree, buff)
			let res = TakeString(strpart(a:code, idx + 1))
			let idx += res[1]
			let tree = Cons(res[0], tree)
			let buff = ["", g:nil]
		elseif "'" == c
			let tree = Growth(tree, buff)
			let buff[1] = Cons(Intern("quote"), buff[1])
		elseif "`" == c
			let tree = growth(tree, buff)
			let buff[1] = Cons(Intern("quasiquote"), buff[1])
		elseif "," == c
			let tree = growth(tree, buff)
			let buff[1] = Cons(Intern("unquote"), buff[1])
		elseif "@" == c
			let tree = growth(tree, buff)
			let buff[1] = Cons(Intern("splicing"), buff[1])
		elseif "^" == c
			let tree = growth(tree, buff)
			let buff[1] = Cons(Intern("tee"), buff[1])
		elseif "." == c
			if buff[0]
				let buff[0] += "."
			else
				return Nconc(Nreverse(Cdr(tree)), Cons(Car(tree), Car(Lread(strpart(a:code, idx + 1)))))
			endif
		else
			let buff[0] .= c
		endif
	endfor
	let tree = Growth(tree, buff)
	return Nreverse(tree)
endfunction

function! Lreadtop (code) abort
	return Cons(Intern("do"), Lread(a:code))
endfunction

function! Leval (expr, env) abort
	let aexpr = a:expr
	let aenv = a:env
	while 1
		let typ = Ltype(aexpr).name
		if "<Cons>" == typ
			let args = Cdr(aexpr)
			let proc = Leval(Car(aexpr), aenv)
			let ptyp = Ltype(proc).name
			if "<Func>" == ptyp
				let aexpr = proc.body
				let aenv = Cons(BindTree(proc.args, Mapeval(args, aenv)), proc.env)
			elseif "<Spfm>" == ptyp
				if "if" == proc.name
					let aexpr = Leval(Car(args), aenv) is g:nil ? Car(Cdr(Cdr(args))) : Car(Cdr(args))
				elseif "do" == proc.name
					let rest = args
					while Atom(Cdr(rest)) is g:nil
						call Leval(Car(rest), aenv)
						let rest = Cdr(rest)
					endwhile
					let aexpr = Car(rest)
				elseif "!" == proc.name
					let aexpr = Lapply(Leval(Car(args), aenv), Cdr(args))
				else
					return proc.proc(aenv, args)
				endif
			elseif "<Subr>" == ptyp
				return Lapply(proc, Mapeval(args, aenv))
			else
				call Lthrow(erroid.uncallable, Lprint(proc) . " is not callable.")
			endif
		elseif "<Symb>" == typ
			return Seekenv(aenv, aexpr)
		else
			return aexpr
		endif
	endwhile
endfunction

function! Lapply (proc, args) abort
	let typ = Ltype(proc).name
	if "<Func>" == typ
		return Leval(proc.body, Cons(BindTree(proc.args, a:args), proc.env))
	elseif "<Subr>" == typ
		return call(proc, Cons2Vect(args))
	endif
endfunction

function! Lthrow (eid, emess) abort
	throw "," . a:eid . "," . a:emess
endfunction



function! Lprint (expr) abort
	let dup = SeekDup(a:expr, g:nil, g:nil)[1]
	let s = ""
	let idx = 0
	let rest = dup
	while Atom(rest) is g:nil
		let s .= "$" . idx . " = " . LprintRec(Car(rest), dup, 0) . ", "

		let rest = Cdr(rest)
		let idx += 1
	endwhile
	let s .= LprintRec(a:expr, dup, 1)
	return s
endfunction

function! SeekDup (expr, printed, dup) abort
	if ! (FindidxEq(a:expr, a:printed) is g:nil)
		if ! (FindidxEq(a:expr, a:dup) is g:nil)
			return [a:printed, a:dup]
		endif
		return [a:printed, Cons(a:expr, a:dup)]
	endif
	let typ = Ltype(a:expr).name
	if "<Cons>" == typ
		let res = SeekDup(Car(a:expr), Cons(a:expr, a:printed), a:dup)
		return SeekDup(Cdr(a:expr), res[0], res[1])
	elseif "<Queu>" == typ
		return SeekDup(a:expr.exit, Cons(a:expr, a:printed), a:dup)
	elseif "<Vect>" == typ
		let res = [Cons(a:expr, a:printed), a:dup]
		for elm in a:expr
			let res = SeekDup(elm, res[0], res[1])
		endfor
		return res
	elseif "<Erro>" == typ
		return SeekDup(a:expr.estr, Cons(a:expr, a:printed), a:dup)
	endif
	return [a:printed, a:dup]
endfunction

function! LprintRec (expr, dup, rec) abort
	let idx = FindidxEq(a:expr, a:dup)
	if a:rec && ! (idx is g:nil)
		return "$" . idx
	elseif a:expr is g:nil
		return "NIL"
	elseif a:expr is g:t
		return "T"
	endif
	let typ = Ltype(a:expr).name
	if "<Cons>" == typ
		return PrintconsRec(a:expr, a:dup, 1)
	elseif "<Symb>" == typ
		return a:expr.name
	elseif "<Strn>" == typ
		return '"' . a:expr . '"'
	elseif "<Vect>" == typ
		return "[" . join(map(a:expr, 'LprintRec(v:val, a:dup, 1)'), " ") . "]"
	elseif "<Queu>" == typ
		return "/" . LprintRec(a:expr.exit, a:dup, 1) . "/"
	elseif "<Func>" == typ
		return "<Func " . LprintRec(a:expr.args, a:dup, 1) . " " . LprintRec(a:expr.body, a:dup, 1) . ">"
	elseif "<Spfm>" == typ
		return "<Spfm " . a:expr.name . ">"
	elseif "<Subr>" == typ
		return "<Subr " . string(typ) . ">"
	endif
	return string(a:expr)
endfunction

function! PrintconsRec (coll, dup, rec) abort
	let a = Car(a:coll)
	let d = Cdr(a:coll)
	if d is g:nil
		return "(" . LprintRec(a, a:dup, a:rec) . ")"
	elseif ! (Atom(d) is g:nil)
		return "(" . LprintRec(a, a:dup, a:rec) . " . " . LprintRec(d, a:dup, a:rec) . ")"
	elseif FindidxEq(d, a:dup) is g:nil
		return "(" . LprintRec(a, a:dup, a:rec) . " " . strpart(LprintRec(d, a:dup, a:rec), 1)
	endif
	return "(" . LprintRec(a, a:dup, a:rec) . " . " . LprintRec(d, a:dup, a:rec) . ")"
endfunction

function! Regist (name, obj) abort
" TODO
endfunction
