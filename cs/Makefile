CSC := '/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
LIB := 'C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319,C:\\Windows\\Microsoft.NET\\assembly\\GAC_MSIL'

UCSC := mcs
ULIBD := /usr/lib/mono/4.5
ULIB := $(ULIBD)/mscorlib.dll,$(ULIBD)/System.Windows.Forms.dll,$(ULIBD)/System.Drawing.dll,$(ULIBD)/System.Data.dll

SRC := src
BLD := build

.PHONY: all
all: $(BLD)/repl.exe $(BLD)/test.exe

.PHONY: unixall
unixall: $(BLD)/urepl $(BLD)/utest

$(BLD)/repl.exe: $(SRC)/Repl.cs $(SRC)/Interpreter.cs
	$(CSC) /out:'$(BLD)\\repl.exe' /lib:$(LIB) $(SRC)\\Repl.cs $(SRC)\\Interpreter.cs

$(BLD)/test.exe: $(SRC)/Test.cs $(SRC)/Interpreter.cs
	$(CSC) /out:'$(BLD)\\test.exe' /lib:$(LIB) $(SRC)\\Test.cs $(SRC)\\Interpreter.cs

$(BLD)/urepl: $(SRC)/Repl.cs $(SRC)/Interpreter.cs
	$(UCSC) -out:$(BLD)/urepl -r:$(ULIB) $(SRC)/Repl.cs $(SRC)/Interpreter.cs

$(BLD)/utest: $(SRC)/Test.cs $(SRC)/Interpreter.cs
	$(UCSC) -out:$(BLD)/utest -r:$(ULIB) $(SRC)/Test.cs $(SRC)/Interpreter.cs

.PHONY: repl test clean
repl: $(BLD)/repl.exe
	$(BLD)/repl.exe

test: $(BLD)/test.exe
	$(BLD)/test.exe

.PHONY: urepl utest
urepl: $(BLD)/urepl
	mono $(BLD)/urepl

utest: $(BLD)/utest
	mono $(BLD)/utest


clean:
	rm $(BLD)/*
