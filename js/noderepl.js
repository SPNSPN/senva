#! /usr/bin/env node

const snv = require("./interpreter.js");
const readline = require("readline");
const fs = require("fs");
const reader = readline.createInterface({input: process.stdin});

function lload_node (path)
{
	if (! (path  instanceof String) && (typeof path) != "string")
	{
		throw new snv.Erro(snv.ErroId.Type, `cannot apply load to ${snv.lprint(path)}`);
	}

	try
	{
		return snv.leval(snv.lreadtop(fs.readFileSync(path, "utf-8")), snv.genv);
	}
	catch (erro)
	{
		throw erro;
		//throw new snv.Erro(snv.ErroId.FileNotFound, `not found file: ${snv.lprint(path)}`);
	}
}

function llprin_node ()
{
	Array.from(arguments).map(function (a)
			{
				process.stdout.write((a instanceof String || (typeof a) == "string")
						? a : lprint(a));
			});
	return nil;
}

snv.rplacd(snv.assoc(snv.car(snv.genv), new snv.Symb("load")), lload_node);
snv.rplacd(snv.assoc(snv.car(snv.genv), new snv.Symb("prin")), llprin_node);

process.stdout.write("senva> ");
reader.on("line", function (line)
		{
			try
			{
				console.log(snv.lprint(snv.leval(snv.lreadtop(line), snv.genv)));
			}
			catch (erro)
			{
				if (erro instanceof snv.Erro)
				{
					console.log(snv.lprint(erro));
				}
				else
				{
					throw erro;
				}
			}
			process.stdout.write("senva> ");
		});

